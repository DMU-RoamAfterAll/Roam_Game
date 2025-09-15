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

    public List<SectionData> sectionDatas;
    public SaveData save;

    [Header("File Path Check")]
    [SerializeField] private string path;
    [SerializeField] private string folder;

    [Header("Option")]
    [SerializeField] private bool autoSaveOnPause = true;
    [SerializeField] private bool prettyPrint = true;
    [SerializeField] public SaveData pendingLoadData;
    public bool HasSave() {
        // path가 아직 초기화 전이어도 안전하게 기본 경로로 검사
        string p = !string.IsNullOrEmpty(path)
            ? path
            : System.IO.Path.Combine(Application.persistentDataPath, "playerDataTest.json");
        return File.Exists(p);
    }

    public void DeleteSave() {
        string p = !string.IsNullOrEmpty(path)
            ? path
            : System.IO.Path.Combine(Application.persistentDataPath, "playerDataTest.json");
        if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
    }

    void Awake() {
        if(Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void Start() {
        folder = Application.persistentDataPath;
        path = Path.Combine(folder, "playerDataTest.json");
        Debug.Log($"[SaveLoad] Save Path = {path}");

        save = new SaveData();
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

            save = data;

            // ★ 로드 직후 저장된 값들을 '2구간 정규화'로 통일
            save.currentSectionId = NormalizeId(save.currentSectionId);
            save.preSectionId     = NormalizeId(save.preSectionId);
            if (save.visitedSectionIds != null) {
                var set = new HashSet<string>();
                foreach (var raw in save.visitedSectionIds)
                    set.Add(NormalizeId(raw));
                save.visitedSectionIds = new List<string>(set); // 중복 제거 포함
            }
            
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

        GameObject player = MapSceneDataManager.Instance.Player;
        if(player != null) {
            player.transform.position = data.playerPos;
        }

        // ---- 섹션 맵 만들기: sections + mainSections 모두 포함 (ID -> SectionData)
        var msdm = MapSceneDataManager.Instance;
        var allGOs = new List<GameObject>();
        if (msdm != null) {
            if (msdm.sections != null)     allGOs.AddRange(msdm.sections);
            if (msdm.mainSections != null) allGOs.AddRange(msdm.mainSections);
        }

        // 맵 만들기 (정규화된 키로!)
        var map = new Dictionary<string, SectionData>();
        foreach (var go in allGOs) {
            if (go == null) continue;
            if (go.TryGetComponent<SectionData>(out var sd) && !string.IsNullOrEmpty(sd.id)) {
                var key = NormalizeId(sd.id);   // ★ 2구간 정규화
                map[key] = sd;
            }
        }

        // 방문 복원 (저장값도 정규화)
        Debug.Log($"[Load] visited ids in save: {data.visitedSectionIds?.Count ?? 0}");
        foreach (var raw in data.visitedSectionIds) {
            var id = NormalizeId(raw);          // ★ 2구간 정규화
            if (map.TryGetValue(id, out var sec)) sec.isVisited = true;
            else Debug.LogWarning($"[Load] visitedId '{raw}' -> '{id}' not found in map");
        }

        // PlayerControl 복원 (current / previous도 정규화)
        var pc = player != null ? player.GetComponent<PlayerControl>() : null;
        if (pc != null) {
            if (!string.IsNullOrEmpty(data.currentSectionId)) {
                var curId = NormalizeId(data.currentSectionId);  // ★
                if (map.TryGetValue(curId, out var cur)) {
                    pc.currentSection = cur.gameObject;
                    pc.sectionData    = cur;
                    player.transform.SetParent(cur.transform, true);
                    player.transform.position = cur.transform.position;
                    cur.isPlayerOn = true;
                } else {
                    Debug.LogWarning($"[Load] currentSectionId '{data.currentSectionId}' -> '{curId}' not found");
                }
            } else {
                Debug.LogWarning("[Load] currentSectionId is null/empty in save");
            }

            if (!string.IsNullOrEmpty(data.preSectionId)) {
                var preId = NormalizeId(data.preSectionId);      // ★
                if (map.TryGetValue(preId, out var pre)) {
                    pc.preSection = pre.gameObject;
                    pre.isPlayerOn = false;
                } else {
                    Debug.LogWarning($"[Load] preSectionId '{data.preSectionId}' -> '{preId}' not found");
                }
            }

            foreach (var sd in map.Values) sd.UpdateSectionColor();
            pc.DetectSection();
        }
    }

    public void NewGameClear(bool resetSceneFlags = true) {
        DeleteSave();              // 파일 삭제
        save = new SaveData();     // 메모리 초기화

        if (!resetSceneFlags) return;

        var msdm = MapSceneDataManager.Instance;
        if (msdm != null && msdm.sections != null) {
            foreach (var go in msdm.sections) {
                if (go == null) continue;
                if (go.TryGetComponent<SectionData>(out var sd)) {
                    sd.isVisited = false;
                    sd.isCleared = false;
                    sd.isPlayerOn = false;
                    // 규칙에 따라 필요하면: sd.isCanMove = false;
                    sd.UpdateSectionColor();
                }
            }
        }
        Debug.Log("[SaveLoad] NewGameClear: file deleted, in-memory save reset, section flags reset.");
    }

    private SaveData TakeSnapshot() {
        save.playerName = GameDataManager.Data.playerName;
        save.originSeed = GameDataManager.Data.seed;

        GameObject player = MapSceneDataManager.Instance.Player;
        if (player != null) save.playerPos = player.transform.position;
   
        PlayerControl pc = player.GetComponent<PlayerControl>();
        if (pc != null) {
            save.currentSectionId = pc.sectionData != null
                ? NormalizeId(pc.sectionData.id) : null;                           // ★ 2구간 정규화
            save.preSectionId     = pc.preSection != null
                ? NormalizeId(pc.preSection.GetComponent<SectionData>()?.id) : null; // ★ 2구간 정규화
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

    public void AddVisitedSectionIds(string id) {
        if (save == null) save = new SaveData();
        var norm = NormalizeId(id);                 // ★ 2구간 정규화
        if (!string.IsNullOrEmpty(norm) && !save.visitedSectionIds.Contains(norm)) {
            save.visitedSectionIds.Add(norm);
        }
    }

    #if UNITY_EDITOR
    [ContextMenu("Print Saved JSON (from file)")]
    private void PrintSavedJsonFromFile() {
        try {
            if (string.IsNullOrEmpty(path))
                path = System.IO.Path.Combine(Application.persistentDataPath, "playerDataTest.json");

            if (File.Exists(path)) {
                var json = File.ReadAllText(path);
                Debug.Log($"[SaveLoad] JSON in file >>>\n{json}");
            } else {
                Debug.Log("[SaveLoad] No save file to print.");
            }
        } catch (Exception e) {
            Debug.LogError($"[SaveLoad] Print failed: {e.Message}");
        }
    }
    #endif


    #if UNITY_EDITOR
    [ContextMenu("Debug/Compare Saved IDs vs Scene IDs")]
    private void DebugCompareIds() {
        // 씬에 있는 SectionData 수집 (sections + mainSections)
        var msdm = MapSceneDataManager.Instance;
        var allGOs = new System.Collections.Generic.List<GameObject>();
        if (msdm != null) {
            if (msdm.sections != null)     allGOs.AddRange(msdm.sections);
            if (msdm.mainSections != null) allGOs.AddRange(msdm.mainSections);
        }

        var sceneIds = new HashSet<string>();
        foreach (var go in allGOs) {
            if (go == null) continue;
            if (go.TryGetComponent<SectionData>(out var sd) && !string.IsNullOrEmpty(sd.id)) {
                var raw  = sd.id;
                var norm = NormalizeId(raw);
                sceneIds.Add(norm);
                Debug.Log($"[SceneID] raw='{raw}', norm='{norm}'");
            }
        }
        Debug.Log($"[SceneID] total(norm) = {sceneIds.Count}");

        if (save == null) { Debug.LogWarning("[Saved] no in-memory save"); return; }

        // current / previous
        string curRaw  = save.currentSectionId;
        string curNorm = NormalizeId(curRaw);
        Debug.Log($"[Saved] current raw='{curRaw}', norm='{curNorm}', inScene={sceneIds.Contains(curNorm)}");

        string preRaw  = save.preSectionId;
        string preNorm = NormalizeId(preRaw);
        Debug.Log($"[Saved] previous raw='{preRaw}', norm='{preNorm}', inScene={sceneIds.Contains(preNorm)}");

        // visited
        if (save.visitedSectionIds != null) {
            foreach (var v in save.visitedSectionIds) {
                var n = NormalizeId(v);
                Debug.Log($"[Saved] visited raw='{v}', norm='{n}', inScene={sceneIds.Contains(n)}");
            }
        }
    }
    #endif

    // ★★★★★ 여기서부터 '마지막 2구간'만 남기는 정규화 규칙 ★★★★★
    private static string NormalizeId(string raw) {
        if (string.IsNullOrEmpty(raw)) return raw;
        // 경로 구분자 통일 및 불필요한 공백/슬래시 제거
        string s = raw.Replace('\\', '/').Trim().TrimEnd('/');
        var parts = s.Split('/');
        if (parts.Length == 0) return s;

        // 마지막 세그먼트(파일명)에서 .json 제거
        string last = parts[^1];
        if (last.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            last = last[..^5];

        // 항상 마지막 2세그먼트를 사용
        if (parts.Length >= 2) {
            string secondLast = parts[^2];
            return $"{secondLast}/{last}";
        }

        // 세그먼트가 1개뿐이면 경고 후 그 값 사용 (호환/디버그용)
        Debug.LogWarning($"[SaveLoad] NormalizeId expected 2+ segments but got '{raw}'");
        return last;
    }
}