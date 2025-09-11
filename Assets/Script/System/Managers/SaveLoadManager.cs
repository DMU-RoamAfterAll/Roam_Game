using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class SaveData {
    public string playerName;
    public int originSeed;
    public Vector3 playerPos;
    public string currentSectionId;
    public string preSectionId;
    public List<string> visitedSectionIds = new List<string>();
}

public class SaveLoadManager : MonoBehaviour {
    public static SaveLoadManager Instance { get; private set; }

    [Header("File Path Check")]
    [SerializeField] private string path;
    [SerializeField] private string folder;

    [Header("Option")]
    [SerializeField] private bool autoSaveOnPause = true;
    [SerializeField] private bool prettyPrint = true;
    public bool HasSave() {
        // path가 아직 초기화 전이어도 안전하게 기본 경로로 검사
        string p = !string.IsNullOrEmpty(path)
            ? path
            : System.IO.Path.Combine(Application.persistentDataPath, "playerDataTest.json");
        return File.Exists(p);
    }

    public void DeleteSave() { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); } 

    void Awake() {
        if(Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void Start() {
        folder = Application.persistentDataPath;
        path = Path.Combine(folder, "playerDataTest.json");
        Debug.Log($"[SaveLoad] Save Path = {path}");
    }

    //현재 MapScene에서 상태 스냅샷
    public void SaveNow() {
        try {
            var data = TakeSnapshot();
            var json = JsonUtility.ToJson(data, prettyPrint);
            File.WriteAllText(path, json);
            Debug.Log($"[SaveLoad] Saved to {path}\n{json}");
        } catch (Exception e) {
            Debug.LogError($"[SaveLoad] Save failed: {e.Message}");
        }
    }

    public bool TryLoad(out SaveData data) {
        data = null;
        try {
            if (!File.Exists(path)) return false;
            var json = File.ReadAllText(path);
            data = JsonUtility.FromJson<SaveData>(json);
            if(data == null) return false;
            Debug.Log($"SaveLoad Loaded from {path}");
            return true;
        } catch (Exception e) {
            Debug.LogError($"[SaveLoad] Load failed: {e.Message}");
            return false;
        }
    }

    public IEnumerator ApplyLoadedData(SaveData data) {
        if(data == null) yield break;

        var spawner = FindFirstObjectByType<RandomSectionSpawner>();
        if(spawner != null) {
            spawner.seed = data.originSeed;
            spawner.SendMessage("Generate", SendMessageOptions.DontRequireReceiver);
        }

        yield return null;

        var player = GameObject.FindWithTag("Player");
        if(player != null) {
            player.transform.position = data.playerPos;
        }

        #if UNITY_2023_1_OR_NEWER
        var sections = UnityEngine.Object.FindObjectsByType<SectionData>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        #else
        var sections = UnityEngine.Object.FindObjectsOfType<SectionData>();
        #endif

        var map = new Dictionary<string, SectionData>();
        foreach (var s in sections) {
            if (!string.IsNullOrEmpty(s.id) && !map.ContainsKey(s.id)) {
                map.Add(s.id, s);
            }
        }

        foreach(var id in data.visitedSectionIds) {
            if(map.TryGetValue(id, out var sec)) {
                sec.isVisited = true;
            }
        }

        var pc = FindFirstObjectByType<PlayerControl>();
        if (pc != null) {
            if(!string.IsNullOrEmpty(data.currentSectionId) && map.TryGetValue(data.currentSectionId, out var cur)) {
                pc.sectionData = cur;
                player.transform.SetParent(cur.transform, true);
                pc.sectionData.isPlayerOn = true;
            }
            if(!string.IsNullOrEmpty(data.preSectionId) && map.TryGetValue(data.preSectionId, out var pre)) {
                pc.preSection = pre.gameObject;
            }
        }
    }

    private SaveData TakeSnapshot() {
        var save = new SaveData();

        save.playerName = GameDataManager.Data.playerName;
        save.originSeed = GameDataManager.Data.seed;

        var player = GameObject.FindWithTag("Player");
        if (player != null) save.playerPos = player.transform.position;

        #if UNITY_2023_1_OR_NEWER
        var sections = UnityEngine.Object.FindObjectsByType<SectionData>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        #else
        var sections = UnityEngine.Object.FindObjectsOfType<SectionData>();
        #endif

        foreach (var s in sections) {
            if (s.isVisited) save.visitedSectionIds.Add(s.id);
        }

        var pc = FindFirstObjectByType<PlayerControl>();
        if(pc != null) {
            save.currentSectionId = pc.sectionData != null ? pc.sectionData.id : null;
            save.preSectionId = pc.preSection != null ? pc.preSection.GetComponent<SectionData>()?.id : null;
        }

        return save;
    }

    void OnApplicationPause(bool pause) {
        if(autoSaveOnPause && pause) SaveNow();
    }

    void OnApplicationQuit() {
        SaveNow();
    } 

    public void SaveData() => SaveNow();

    public void LoadData() {
        if(TryLoad(out var data)) {
            Debug.Log($"[SaveLoad] user = {data.playerName}, seed = {data.originSeed}");
            Debug.Log($"[SaveLoad] pos = {data.playerPos}");

            StartCoroutine(ApplyLoadedData(data));
        }
        else {
            Debug.Log("[SaveLoad] No save file");
        }
    }
}