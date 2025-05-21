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

    [Header("Candidate Scale")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    [Header("Section Count, Distance")]
    public int sectionCount;
    public int mainSectionCount;
    public float initialMinDistance;

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

        minX = areaData.minX;
        maxX = areaData.maxX;
        minY = areaData.minY;
        maxY = areaData.maxY;
        
        initialMinDistance = GameDataManager.Data.initialMinDistance;

        seed = GameDataManager.Data.seed;

        eventFolderPath = areaData.sectionDataFolderPath;
        mainEventFolderPath = areaData.mainSectionDataFolderPath;

        sectionPrefab = GameDataManager.Data.sectionPrefab;
        mainSectionPrefab = GameDataManager.Data.mainSectionPrefab;
        
        if(Directory.Exists(eventFolderPath)) {
            eventFiles = Directory.GetFiles(eventFolderPath, "*.json").OrderBy(f => f).ToArray();
            sectionCount = eventFiles.Length;
        }
        else {
            Debug.Log("이벤트 파일 위치 오류!");
        }

        if(Directory.Exists(mainEventFolderPath)) {
            mainEventFiles = Directory.GetFiles(mainEventFolderPath, "*.json").OrderBy(f => f).ToArray();
            mainSectionCount = mainEventFiles.Length;
        }
        else {
            Debug.Log("메인 이벤트 파일 위치 오류!");
        }

        CreateMainSection();

        List<Vector2> points = GenerateGuaranteedPoints(sectionCount, initialMinDistance, seed);
        CreateSection(points);
    }

    List<Vector2> GenerateGuaranteedPoints(int count, float minDist, int seed) {
        List<Vector2> result = new List<Vector2>();
        System.Random rng = new System.Random(UniqueSeed());

        int maxAttemptsPerPoint = 500;
        float minDistStep = minDist * 0.1f; // 줄일 때 10%씩 감소

        List<Vector2> allPoints = new List<Vector2>(mainSections);

        while (result.Count < count) {
            int attempts = 0;
            bool pointPlaced = false;

            while (attempts < maxAttemptsPerPoint) {
                float x = (float)(rng.NextDouble() * (maxX - minX) + minX);
                float y = (float)(rng.NextDouble() * (maxY - minY) + minY);
                Vector2 candidate = new Vector2(x, y);

                bool isValid = true;
                foreach (var point in allPoints) {
                    if (Vector2.Distance(point, candidate) < minDist) {
                        isValid = false;
                        break;
                    }
                }

                if (isValid) {
                    result.Add(candidate);
                    allPoints.Add(candidate);
                    pointPlaced = true;
                    break;
                }

                attempts++;
            }

            if (!pointPlaced) {
                minDist = Mathf.Max(minDist - minDistStep, 0f);
                Debug.LogWarning($"거리 조건 완화됨: 현재 최소 거리 {minDist:F2}");
            }
        }

        return result;
    }

    void CreateSection(List<Vector2> points) {
        List<int> eventPool = Enumerable.Range(0, sectionCount).OrderBy(x => Random.value).ToList();

        for(int i = 0; i < sectionCount; i++) {
            foreach(var mainSectionPoint in mainSections) {
                if(points[i] == mainSectionPoint) return;
            }

            int fileIndex = eventPool[i];
            string filePath = eventFiles[fileIndex];

            string json = File.ReadAllText(filePath);
            EventJsonData data = JsonUtility.FromJson<EventJsonData>(json);

            GameObject go = Instantiate(sectionPrefab, new Vector3(points[i].x, points[i].y, 0f), Quaternion.identity);

            go.name = $"Section_{i}";
            go.transform.SetParent(this.gameObject.transform);
            SectionData section = go.GetComponent<SectionData>();

            section.id = fileIndex;
            section.rate = data.rate[0];
            section.eventType = data.eventType;
            section.isVisited = false;
            section.isCleared = false;
            section.sectionPosition = new Vector2(points[i].x, points[i].y);

            sections.Add(section);
        }
    }

    void CreateMainSection() {
        for(int i = 0; i < mainSectionCount; i++) {
            string filePath = mainEventFiles[i];
            string json = File.ReadAllText(filePath);

            MainEventJsonData data = JsonUtility.FromJson<MainEventJsonData>(json);

            Vector2 position = new Vector2(data.x, data.y);
            GameObject go = Instantiate(mainSectionPrefab, position, Quaternion.identity);

            go.name = $"MainSection_{i}";
            go.transform.SetParent(this.gameObject.transform);
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
        string combinedSeed = seed.ToString() + "_" + this.gameObject.name;

        using (SHA256 sha = SHA256.Create()) {
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combinedSeed));
            return System.BitConverter.ToInt32(hash, 0);
        }
    }
}