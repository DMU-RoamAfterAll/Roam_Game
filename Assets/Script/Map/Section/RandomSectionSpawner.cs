using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Security.Cryptography;

[System.Serializable]
public class EventJsonData {
    public string rate;
    public string eventType;
}

[System.Serializable]
public class MainEventJsonData {
    public string rate;
    public string eventType;
    public float x;
    public float y;
}

public class RandomSectionSpawner : MonoBehaviour {
    [SerializeField] private List<SectionData> sections = new List<SectionData>();

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

    [Header("Script")]
    public AreaData areaData;

    void Start() {
        areaData = Resources.Load<AreaData>($"AreaData/{this.gameObject.name}Data");

        initialMinDistance = GameDataManager.Data.initialMinDistance;
        initialMaxDistance = GameDataManager.Data.initialMaxDistance;
        seed = GameDataManager.Data.seed;

        maxRadius = GameDataManager.Data.maxRadius;

        eventFolderPath = areaData.sectionDataFolderPath;
        mainEventFolderPath = areaData.mainSectionDataFolderPath;

        sectionPrefab = GameDataManager.Data.sectionPrefab;
        mainSectionPrefab = GameDataManager.Data.mainSectionPrefab;

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

        CreateMainSection();

        List<Vector2> points = GenerateGuaranteedPoints(sectionCount, initialMinDistance, initialMaxDistance, maxRadius);
        CreateSection(points);
    }

    List<Vector2> GenerateGuaranteedPoints(int count, float minDist, float maxDist, float maxRadius) {
        List<Vector2> generatedPoints = new List<Vector2>();
        System.Random rng = new System.Random(UniqueSeed());
        List<Vector2> allPoints = new List<Vector2>(mainSections);

        // 시작점이 없으면 중심에서 시작
        if (allPoints.Count == 0)
            allPoints.Add(Vector2.zero);

        while (generatedPoints.Count < count)
        {
            Vector2 basePoint = allPoints[rng.Next(allPoints.Count)];
            float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
            float distance = minDist + (float)rng.NextDouble() * (maxDist - minDist);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            Vector2 candidate = basePoint + offset;

            if (candidate.magnitude > maxRadius)
                continue;

            bool tooClose = allPoints.Any(p => Vector2.Distance(candidate, p) < minDist);
            if (tooClose)
                continue;

            bool canConnect = allPoints.Any(p => Vector2.Distance(candidate, p) <= maxDist);
            if (!canConnect)
                continue;

            generatedPoints.Add(candidate);
            allPoints.Add(candidate);
        }

        return generatedPoints;
    }

    void CreateSection(List<Vector2> points) {
        List<int> eventPool = Enumerable.Range(0, sectionCount).OrderBy(x => Random.value).ToList();

        for (int i = 0; i < sectionCount; i++) {
            int fileIndex = eventPool[i];
            string filePath = eventFiles[fileIndex];
            string json = File.ReadAllText(filePath);
            EventJsonData data = JsonUtility.FromJson<EventJsonData>(json);

            Vector2 pos = points[i];
            GameObject go = Instantiate(sectionPrefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);

            go.name = $"Section_{i}";
            go.transform.SetParent(this.transform);

            SectionData section = go.GetComponent<SectionData>();
            section.id = fileIndex;
            section.rate = data.rate[0];
            section.eventType = data.eventType;
            section.isVisited = false;
            section.isCleared = false;
            section.sectionPosition = pos;

            sections.Add(section);
        }
    }

    void CreateMainSection() {
        for (int i = 0; i < mainSectionCount; i++) {
            string filePath = mainEventFiles[i];
            string json = File.ReadAllText(filePath);

            MainEventJsonData data = JsonUtility.FromJson<MainEventJsonData>(json);
            Vector2 position = new Vector2(data.x, data.y);
            GameObject go = Instantiate(mainSectionPrefab, position, Quaternion.identity);

            go.name = $"MainSection_{i}";
            go.transform.SetParent(this.transform);

            SectionData section = go.GetComponent<SectionData>();
            section.id = i;
            section.rate = data.rate[0];
            section.eventType = data.eventType;
            section.isVisited = false;
            section.isCleared = false;
            section.sectionPosition = position;

            sections.Add(section);
        }

        mainSections = sections
            .Where(s => s.eventType == "Main")
            .Select(s => s.sectionPosition)
            .ToArray();
    }

    int UniqueSeed() {
        string combinedSeed = seed.ToString() + "_" + gameObject.name;
        using (SHA256 sha = SHA256.Create()) {
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combinedSeed));
            return System.BitConverter.ToInt32(hash, 0);
        }
    }
}