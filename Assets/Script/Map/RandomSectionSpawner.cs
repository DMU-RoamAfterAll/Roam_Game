using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class EventJsonData {
    public string rate;
    public string eventType;
}

public class RandomSectionSpawner : MonoBehaviour
{
    [SerializeField] private List<SectionData> sections = new List<SectionData>();

    [Header("Candidate Scale")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    [Header("Section Count, Distance")]
    public int sectionCount;
    public float initialMinDistance;

    [Header("Random Seed")]
    public int seed;

    [Header("Section Prefab")]
    public GameObject sectionPrefab;

    [Header("Story Section")]
    public Vector2[] mainSections;

    [Header("Data")]
    public string eventFolderPath;
    public string[] eventFiles;

    void Start() {
        if (sectionPrefab == null) {
            Debug.LogError("Point Prefab이 설정되지 않았습니다!");
            return;
        }

        eventFolderPath = "Assets/Resources/EventData/" + $"{this.gameObject.name}Events";

        if(Directory.Exists(eventFolderPath)) {
            eventFiles = Directory.GetFiles(eventFolderPath, "*.json").OrderBy(f => f).ToArray();
            sectionCount = eventFiles.Length;
        }
        else {
            Debug.Log("파일 위치 오류!");
        }

        List<Vector2> points = GenerateGuaranteedPoints(sectionCount, initialMinDistance, seed);

        List<int> eventPool = Enumerable.Range(0, sectionCount).OrderBy(x => Random.value).ToList();

        for(int i = 0; i < sectionCount; i++) {
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

            sections.Add(section);
        }
    }

    List<Vector2> GenerateGuaranteedPoints(int count, float minDist, int seed) {
        List<Vector2> result = new List<Vector2>();
        System.Random rng = new System.Random(seed);

        int maxAttemptsPerPoint = 500;
        float minDistStep = minDist * 0.1f; // 줄일 때 10%씩 감소

        foreach (var main in mainSections) {
            result.Add(main);
        }

        while (result.Count < count) {
            int attempts = 0;
            bool pointPlaced = false;

            while (attempts < maxAttemptsPerPoint) {
                float x = (float)(rng.NextDouble() * (maxX - minX) + minX);
                float y = (float)(rng.NextDouble() * (maxY - minY) + minY);
                Vector2 candidate = new Vector2(x, y);

                bool isValid = true;
                foreach (var point in result) {
                    if (Vector2.Distance(point, candidate) < minDist) {
                        isValid = false;
                        break;
                    }
                }

                if (isValid) {
                    result.Add(candidate);
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
}