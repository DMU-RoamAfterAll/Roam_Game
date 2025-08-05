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
    public GameObject Player;
    public AreaLocateControl areaLocate;
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
        StartCoroutine(InitializeSections());
    }

    IEnumerator InitializeSections() {
        Player = MapSceneDataManager.Instance.Player;

        // 1) Load the AreaAsset (pre-created or downloaded at runtime)
        areaAsset = Resources.Load<AreaAsset>($"AreaAssetData/{this.gameObject.name}Data");

        // 2) Get configuration from GameDataManager
        seed               = GameDataManager.Data.seed;
        initialMinDistance = MapSceneDataManager.mapData.initialMinDistance;
        initialMaxDistance = MapSceneDataManager.mapData.initialMaxDistance;
        maxRadius          = MapSceneDataManager.mapData.maxRadius;

        // 3) Folder paths stored in the asset (editor-only values, unused at runtime if using Resources)
        eventFolderPath      = areaAsset.sectionDataFolderPath;
        mainEventFolderPath  = areaAsset.mainSectionDataFolderPath;

        // 4) Prefabs from GameDataManager
        sectionPrefab    = MapSceneDataManager.mapData.sectionPrefab;
        mainSectionPrefab = MapSceneDataManager.mapData.mainSectionPrefab;

        // 5) Initialize bounds
        maxX = float.MinValue;
        minX = float.MaxValue;
        maxY = float.MinValue;
        minY = float.MaxValue;

        // 6) Load all JSON TextAssets from Resources/EventData and MainEventData
        TextAsset[] eventJsons = Resources.LoadAll<TextAsset>(
            $"EventData/{areaAsset.areaName}Events"
        );
        sectionCount = eventJsons.Length;

        TextAsset[] mainJsons = Resources.LoadAll<TextAsset>(
            $"MainEventData/Main{areaAsset.areaName}Events"
        );
        mainSectionCount = mainJsons.Length;

        #region Function

        // 7) Create main sections first
        for (int i = 0; i < mainSectionCount; i++) {
            MainEventJsonData data = JsonUtility.FromJson<MainEventJsonData>(mainJsons[i].text);
            Vector2 pos;
            if(data.x == 0 && data.y == 0) { pos = RandomMainSection(); }
            else { pos = new Vector2(data.x, data.y); }

            GameObject go = Instantiate(mainSectionPrefab, pos, Quaternion.identity);
            go.transform.SetParent(this.transform);
            go.name = $"MainSection_{i}";
            MapSceneDataManager.Instance.mainSections.Add(go);

            SectionData section = go.GetComponent<SectionData>();
            section.id              = $"{gameObject.name}/{data.id}";
            section.rate            = data.rate[0];
            section.eventType       = data.eventType;
            section.isVisited       = false;
            section.isCleared       = false;
            section.isPlayerOn      = false;
            section.isCanMove       = false;
            section.sectionPosition = pos;

            sections.Add(section);

            yield return new WaitForSeconds(Random.Range(0.5f, 1.2f));
        }

        // 8) Generate random points for normal sections
        List<Vector2> sectionPoints = GenerateGuaranteedPoints(
            sectionCount,
            initialMinDistance,
            initialMaxDistance,
            maxRadius
        );

        // 9) Create normal sections
        for (int i = 0; i < sectionCount; i++) {
            EventJsonData data = JsonUtility.FromJson<EventJsonData>(eventJsons[i].text);
            Vector2 pos = sectionPoints[i];

            GameObject go = Instantiate(sectionPrefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
            go.transform.SetParent(this.transform);
            go.name = $"Section_{i}";
            MapSceneDataManager.Instance.sections.Add(go);

            SectionData section = go.GetComponent<SectionData>();
            section.id              = $"{gameObject.name}/{data.id}";
            section.rate            = data.rate[0];
            section.eventType       = data.eventType;
            section.isVisited       = false;
            section.isCleared       = false;
            section.isPlayerOn      = false;
            section.isCanMove       = false;
            section.sectionPosition = pos;

            sections.Add(section);

            yield return new WaitForSeconds(Random.Range(0.5f, 1.2f));
        }

        // 10) Adjust main sections relative to nearest neighbor
        AdjustMainSection();

        // 11) Calculate bounds and recenter
        GetBound();

        // 12) Notify AreaLocateControl that this area is set up
        areaLocate = this.transform.parent.GetComponent<AreaLocateControl>();
        areaLocate.createdAreaCount++;
        areaLocate.FindAreaPoint();

        #endregion
    }

    List<Vector2> GenerateGuaranteedPoints(int count, float minDist, float maxDist, float maxRadius, Vector2? current = null) {
        Vector2 currentPoint = current ?? Vector2.zero;
        List<Vector2> generatedPoints = new List<Vector2> { currentPoint };
        System.Random rng = new System.Random(UniqueSeed());
        List<Vector2> allPoints = new List<Vector2>(generatedPoints);

        int attempts = 0;
        while (generatedPoints.Count < count) {
            attempts++;
            Vector2 origin = allPoints[rng.Next(allPoints.Count)];
            float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
            float distance = minDist + (float)rng.NextDouble() * (maxDist - minDist);
            Vector2 candidate = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            if (Vector2.Distance(candidate, origin) > maxRadius) continue;
            if (allPoints.Any(p => Vector2.Distance(candidate, p) < minDist)) continue;
            if (!allPoints.Any(p => Vector2.Distance(candidate, p) <= maxDist)) continue;

            generatedPoints.Add(candidate);
            allPoints.Add(candidate);

            if (attempts % 100 == 0) {
                Debug.LogError($"[GeneratePoints] Failed after {attempts} attempts.");
                break;
            }
        }

        return generatedPoints;
    }

    Vector2 RandomMainSection() {
        System.Random rng = new System.Random(UniqueSeed());
        return new Vector2(Random.Range(maxRadius * -1, maxRadius), Random.Range(maxRadius * -1, maxRadius));
    }

    void AdjustMainSection() {
        var mainObjs = GetComponentsInChildren<SectionData>()
            .Where(s => s.eventType == "Main" && s.transform.parent == this.transform)
            .ToList();

        var original = mainObjs.ToDictionary(m => m, m => m.sectionPosition);

        foreach (var main in mainObjs) {
            Vector2 mPos = original[main];
            Vector2 closest = Vector2.zero;
            float distMin = float.MaxValue;

            foreach (var other in mainObjs) {
                if (other == main) continue;
                float d = Vector2.Distance(mPos, original[other]);
                if (d < distMin) { distMin = d; closest = original[other]; }
            }

            foreach (var sec in sections) {
                if (sec.eventType == "Main" || sec == main || sec.transform.parent != transform) continue;
                float d = Vector2.Distance(mPos, sec.sectionPosition);
                if (d < distMin) { distMin = d; closest = sec.sectionPosition; }
            }

            if (distMin > initialMaxDistance) {
                Vector2 dir = (closest - mPos).normalized;
                Vector2 newPos = closest - dir * initialMaxDistance;
                main.sectionPosition = newPos;
                main.transform.position = new Vector3(newPos.x, newPos.y, 0);
            }
        }
    }

    int UniqueSeed() {
        string combined = seed.ToString() + "_" + gameObject.name;
        using (SHA256 sha = SHA256.Create()) {
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return System.BitConverter.ToInt32(hash, 0);
        }
    }

    void GetBound() {
        foreach (var sec in sections) {
            Vector2 p = sec.sectionPosition;
            maxX = Mathf.Max(maxX, p.x);
            minX = Mathf.Min(minX, p.x);
            maxY = Mathf.Max(maxY, p.y);
            minY = Mathf.Min(minY, p.y);
            centerX = (minX + maxX) * 0.5f;
            centerY = (minY + maxY) * 0.5f;
            center = new Vector2(centerX, centerY);
        }

        foreach (var sec in sections) {
            if (gameObject.name != "Tutorial")
                sec.sectionPosition -= center;
            sec.transform.position = sec.sectionPosition;
        }
    }

    [ContextMenu("Spawn Event Section")]
    private void CreateEventSection() {
        List<Vector2> eventSectionPoints = GenerateGuaranteedPoints(
            2,
            initialMinDistance,
            initialMaxDistance,
            initialMaxDistance,
            Player.transform.position
        );
        
        foreach(var point in eventSectionPoints) {
            Debug.Log("EventSection Vector : " + point);
        }
    }
}