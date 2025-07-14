//전체적인 "수정필요"
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Security.Cryptography;

[System.Serializable]
public class EventJsonData {
    public string id;
    public string rate;
    public string eventType;
}

[System.Serializable]
public class MainEventJsonData {
    public string id;
    public string rate;
    public string eventType;
    public float x;
    public float y;
}

public class RandomSectionSpawner : MonoBehaviour {
    [SerializeField] private List<SectionData> sections = new List<SectionData>();
    public List<SectionData> Sections => sections;

    [Header("Section Count, Distance")]
    public int sectionCount;
    public int mainSectionCount;
    public float initialMinDistance;
    public float initialMaxDistance;
    public float maxRadius;

    [Header("Random Seed")]
    public int seed;

    [Header("Section Prefab")]
    public GameObject sectionPrefab;
    public GameObject mainSectionPrefab;

    [Header("Story Section")]
    public Vector2[] mainSections;

    [Header("Data")]
    public string eventFolderPath;
    public string mainEventFolderPath;
    public string[] eventFiles;
    public string[] mainEventFiles;

    [Header("Coordinate")]
    public Vector2 center;
    public float centerX;
    public float centerY;
    public float maxX;
    public float minX;
    public float maxY;
    public float minY;

    [Header("Script")]
    public AreaAsset areaAsset;
    
    void Awake() {
        StartCoroutine(StartCoroutine());
    }

    IEnumerator StartCoroutine() {
        areaAsset = Resources.Load<AreaAsset>($"AreaAssetData/{this.gameObject.name}Data");

        initialMinDistance = GameDataManager.Data.initialMinDistance;
        initialMaxDistance = GameDataManager.Data.initialMaxDistance;
        seed = GameDataManager.Data.seed;

        maxRadius = GameDataManager.Data.maxRadius;

        eventFolderPath = areaAsset.sectionDataFolderPath;
        mainEventFolderPath = areaAsset.mainSectionDataFolderPath;

        sectionPrefab = GameDataManager.Data.sectionPrefab;
        mainSectionPrefab = GameDataManager.Data.mainSectionPrefab;

        maxX = float.MinValue;
        minX = float.MaxValue;
        maxY = float.MinValue;
        minY = float.MaxValue;

        if (Directory.Exists(eventFolderPath)) {
            eventFiles = Directory.GetFiles(eventFolderPath, "*.json").OrderBy(f => f).ToArray();
            sectionCount = eventFiles.Length;
        } else {
            Debug.LogError("이벤트 파일 위치 오류!");
        }

        if (Directory.Exists(mainEventFolderPath)) {
            mainEventFiles = Directory.GetFiles(mainEventFolderPath, "*.json").OrderBy(f => f).ToArray();
            mainSectionCount = mainEventFiles.Length;
        } else {
            Debug.LogError("메인 이벤트 파일 위치 오류!");
        }

        #region Function

        yield return StartCoroutine(CreateMainSection());

        List<Vector2> points = GenerateGuaranteedPoints(sectionCount, initialMinDistance, initialMaxDistance, maxRadius);
        yield return StartCoroutine(CreateSection(points));

        AdjustMainSection();

        GetBound();

        AreaLocateControl.createdAreaCount++;

        #endregion
    }

    List<Vector2> GenerateGuaranteedPoints(int count, float minDist, float maxDist, float maxRadius) {
        List<Vector2> generatedPoints = new List<Vector2>();
        System.Random rng = new System.Random(UniqueSeed());

        List<Vector2> allPoints = new List<Vector2>{ Vector2.zero };
        generatedPoints.Add(Vector2.zero);

        int attempts = 0;

        while (generatedPoints.Count < count)
        {
            attempts++;
            Vector2 randomPoint = allPoints[rng.Next(allPoints.Count)];
            float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
            float distance = minDist + (float)rng.NextDouble() * (maxDist - minDist);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            Vector2 candidate = randomPoint + offset;

            if (Vector2.Distance(candidate, randomPoint) > maxRadius)
                continue;

            bool tooClose = allPoints.Any(p => Vector2.Distance(candidate, p) < minDist);
            if (tooClose)
                continue;

            bool canConnect = allPoints.Any(p => Vector2.Distance(candidate, p) <= maxDist);
            if (!canConnect)
                continue;

            generatedPoints.Add(candidate);
            allPoints.Add(candidate);

            if (attempts % 100 == 0) {
                Debug.LogError($"[GeneratePoints] Failed to generate enough points after {attempts} attempts. Terminating early.");
                break;
            }
        }

        return generatedPoints;
    }

    IEnumerator CreateSection(List<Vector2> points) {
        List<int> eventPool = Enumerable.Range(0, sectionCount).OrderBy(x => Random.value).ToList();

        for (int i = 0; i < sectionCount; i++) {
            int fileIndex = eventPool[i];
            string filePath = eventFiles[fileIndex];
            string json = File.ReadAllText(filePath);
            EventJsonData data = JsonUtility.FromJson<EventJsonData>(json);

            Vector2 pos = points[i];
            GameObject go = Instantiate(sectionPrefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
            GameDataManager.Instance.sections.Add(go);

            go.name = $"Section_{i}";
            go.transform.SetParent(this.transform);

            SectionData section = go.GetComponent<SectionData>();
            section.id = this.gameObject.name + "/" + data.id;
            section.rate = data.rate[0];
            section.eventType = data.eventType;
            section.isVisited = false;
            section.isCleared = false;
            section.isPlayerOn = false;
            section.isCanMove = false;
            section.sectionPosition = pos;

            sections.Add(section);

            float waitTime = Random.Range(0.5f, 1.2f);
            yield return new WaitForSeconds(waitTime);
        }
    }

    IEnumerator CreateMainSection() {
        for (int i = 0; i < mainSectionCount; i++) {
            string filePath = mainEventFiles[i];
            string json = File.ReadAllText(filePath);

            MainEventJsonData data = JsonUtility.FromJson<MainEventJsonData>(json);
            Vector2 position = new Vector2(data.x, data.y);

            GameObject go = Instantiate(mainSectionPrefab, position, Quaternion.identity);
            GameDataManager.Instance.mainSections.Add(go);

            go.name = $"MainSection_{i}";
            go.transform.SetParent(this.transform);

            SectionData section = go.GetComponent<SectionData>();
            section.id = this.gameObject.name + "/" + data.id.ToString();
            section.rate = data.rate[0];
            section.eventType = data.eventType;
            section.isVisited = false;
            section.isCleared = false;
            section.isPlayerOn = false;
            section.isCanMove = false;
            section.sectionPosition = position;

            sections.Add(section);

            float waitTime = Random.Range(0.5f, 1.2f);
            yield return new WaitForSeconds(waitTime);
        }

        mainSections = sections
            .Where(s => s.eventType == "Main")
            .Select(s => s.sectionPosition)
            .ToArray();
    }

    void AdjustMainSection() {
        var mainObjs = GetComponentsInChildren<SectionData>()
            .Where(s => s.eventType == "Main" && s.transform.parent == this.transform)
            .ToList();

        var originalPositions = mainObjs.ToDictionary(m => m, m => m.sectionPosition);

        foreach (var main in mainObjs) {
            Vector2 mainOriginalPos = originalPositions[main];
            Vector2 closest = Vector2.zero;
            float closestDist = float.MaxValue;

            foreach (var other in mainObjs) {
                if (other == main) continue;
                float dist = Vector2.Distance(mainOriginalPos, originalPositions[other]);
                if (dist < closestDist) {
                    closestDist = dist;
                    closest = originalPositions[other];
                }
            }

            foreach (var section in sections) {
                if (section.eventType == "Main" || section == main || section.transform.parent != this.transform) continue;
                float dist = Vector2.Distance(mainOriginalPos, section.sectionPosition);
                if (dist < closestDist) {
                    closestDist = dist;
                    closest = section.sectionPosition;
                }
            }

            if (closestDist > initialMaxDistance) {
                Vector2 direction = (closest - mainOriginalPos).normalized;
                Vector2 newPos = closest - direction * initialMaxDistance;
                main.sectionPosition = newPos;
                main.transform.position = new Vector3(newPos.x, newPos.y, 0);
            }
        }
    }

    int UniqueSeed() {
        string combinedSeed = seed.ToString() + "_" + gameObject.name;
        using (SHA256 sha = SHA256.Create()) {
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combinedSeed));
            return System.BitConverter.ToInt32(hash, 0);
        }
    }

    void GetBound() {
        foreach(var section in sections) {
            float x = section.sectionPosition.x;
            float y = section.sectionPosition.y;

            if(x > maxX) {
                maxX = x;
            }

            if(x < minX) {
                minX = x;
            }

            if(y > maxY) {
                maxY = y;
            }

            if(y < minY) {
                minY = y;
            }

            centerX = (minX + maxX) / 2f;
            centerY = (minY + maxY) / 2f;

            center = new Vector2(centerX, centerY);
        }

        foreach(var section in sections) {
            if(this.gameObject.name != "Tutorial") section.sectionPosition -= center;
            section.transform.position = new Vector2(section.sectionPosition.x, section.sectionPosition.y);
        }
    }
}