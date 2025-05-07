using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class EventJsonData {
    public string rate;
    public string eventType;
}

public class CreateSection : MonoBehaviour {
    [SerializeField] private List<SectionData> sections = new List<SectionData>();

    public GameObject sectionPrefab;

    public string eventFolderPath;
    public string[] eventFiles;
    public int sectionCount;

    void Start() { 
        Random.InitState(4321);

        eventFolderPath = "Assets/Resources/EventData/" + $"{this.gameObject.name}Events";

        if(Directory.Exists(eventFolderPath)) {
            eventFiles = Directory.GetFiles(eventFolderPath, "*.json").OrderBy(f => f).ToArray();
            sectionCount = eventFiles.Length;
        }
        else {
            Debug.Log("파일 위치 오류!");
        }

        createSection();
    }

    void createSection() {
        List<int> eventPool = Enumerable.Range(0, sectionCount).OrderBy(x => Random.value).ToList();

        for(int i = 0; i < sectionCount; i++) {
            int fileIndex = eventPool[i];
            string filePath = eventFiles[fileIndex];

            string json = File.ReadAllText(filePath);
            EventJsonData data = JsonUtility.FromJson<EventJsonData>(json);

            GameObject go = Instantiate(sectionPrefab);
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
}