using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

[Serializable]
public class SaveData {
    public string playerName;
    public int originSeed;
    public Vector3 playerPos;
    public string currentSectionId;
    public string preSectionId;
    public bool tutorialClear;
    public List<string> visitedSectionIds = new List<string>();
    public List<string> clearedSectionIds = new List<string>();
    public List<string> canMoveSectionIds = new List<string>();
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

        GameDataManager.Instance?.SendMessage("PersistCoreToPrefs", SendMessageOptions.DontRequireReceiver);
    }

    public bool TryLoad(out SaveData data) {
        data = null;
        try {
            if (!File.Exists(path)) return false;
            var json = File.ReadAllText(path);
            data = JsonUtility.FromJson<SaveData>(json);
            if(data == null) return false;

            save = data;

            // 저장된 경로 정규화
            save.currentSectionId = NormalizeId(save.currentSectionId);
            save.preSectionId     = NormalizeId(save.preSectionId);
            if (save.visitedSectionIds != null) {
                var set = new HashSet<string>();
                foreach (var raw in save.visitedSectionIds)
                    set.Add(NormalizeId(raw));
                save.visitedSectionIds = new List<string>(set);
            }
            if(save.clearedSectionIds != null) {
                var set = new HashSet<string>();
                foreach (var raw in save.clearedSectionIds)
                    set.Add(NormalizeId(raw));
                save.clearedSectionIds = new List<string>(set);
            }
            if(save.canMoveSectionIds != null) {
                var set = new HashSet<string>();
                foreach (var raw in save.canMoveSectionIds)
                    set.Add(NormalizeId(raw));
                save.canMoveSectionIds = new List<string>(set);
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

        // ---- 섹션 맵: sections + mainSections 모두 포함
        var msdm = MapSceneDataManager.Instance;
        var allGOs = new List<GameObject>();
        if (msdm != null) {
            if (msdm.sections != null)     allGOs.AddRange(msdm.sections);
            if (msdm.mainSections != null) allGOs.AddRange(msdm.mainSections);
        }

        var map = new Dictionary<string, SectionData>();
        foreach (var go in allGOs) {
            if (go == null) continue;
            if (go.TryGetComponent<SectionData>(out var sd) && !string.IsNullOrEmpty(sd.id)) {
                var key = NormalizeId(sd.id);
                map[key] = sd;
            }
        }

        // visited/cleared/canMove 복원
        Debug.Log($"[Load] visited ids in save: {data.visitedSectionIds?.Count ?? 0}");
        foreach (var raw in data.visitedSectionIds) {
            var id = NormalizeId(raw);
            if (map.TryGetValue(id, out var sec)) {
                sec.isVisited = true;
                sec.LightObj();
            }
            else Debug.LogWarning($"[Load] visitedId '{raw}' -> '{id}' not found in map");
        }

        Debug.Log($"[Load] cleared ids in save: {data.clearedSectionIds?.Count ?? 0}");
        foreach (var raw in data.clearedSectionIds) {
            var id = NormalizeId(raw);
            if (map.TryGetValue(id, out var sec)) {
                sec.isCleared = true;
            }
            else Debug.LogWarning($"[Load] clearId '{raw}' -> '{id}' not found in map");
        }

        Debug.Log($"[Load] canMove ids in save: {data.canMoveSectionIds?.Count ?? 0}");
        foreach (var raw in data.canMoveSectionIds) {
            var id = NormalizeId(raw);
            if (map.TryGetValue(id, out var sec)) {
                sec.isCanMove = true;
            }
            else Debug.LogWarning($"[Load] canMoveId '{raw}' -> '{id}' not found in map");
        }

        var pc = player != null ? player.GetComponent<PlayerControl>() : null;

        if (pc != null) {
            if (!string.IsNullOrEmpty(data.currentSectionId)) {
                var curId = NormalizeId(data.currentSectionId);
                if (map.TryGetValue(curId, out var cur)) {
                    pc.currentSection = cur.gameObject;
                    pc.sectionData    = cur;
                    player.transform.SetParent(cur.transform, true);
                    player.transform.position = cur.transform.position;
                    cur.isPlayerOn = true;
                }
                else {
                    Debug.LogWarning($"[Load] currentSectionId '{data.currentSectionId}' -> '{curId}' not found");
                }
            }
            else if(pendingLoadData != null && string.IsNullOrEmpty(data.currentSectionId) && !GameDataManager.Data.tutorialClear) {
                pc.currentSection = MapSceneDataManager.Instance.originSection;
                pc.sectionData    = null;
                player.transform.SetParent(pc.currentSection.transform, true);
                player.transform.position = pc.currentSection.transform.position;
            }
            else {
                Debug.LogWarning("[Load] currentSectionId is null/empty in save");
            }

            if (!string.IsNullOrEmpty(data.preSectionId)) {
                var preId = NormalizeId(data.preSectionId);
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

    /// <summary>
    /// 새로하기:
    /// - 세이브 파일 제거 & 메모리 초기화
    /// - StepManager에 "오늘 누적 즉시 적용" 프라임 호출(중복 방지)
    /// - 섹션 플래그 초기화
    /// </summary>
    public void NewGameClear(bool resetSceneFlags = true) {
        GameDataManager.Instance?.NewSeed();
        GameDataManager.Instance?.SetTutorialClear(false);

        DeleteSave();
        save = new SaveData();

        if (StepManager.Instance != null)
            StepManager.Instance.OnNewGamePrimeToday();

        GameDataManager.Data.tutorialClear = false;

        if (!resetSceneFlags) return;

        var msdm = MapSceneDataManager.Instance;
        if (msdm != null && msdm.sections != null) {
            foreach (var go in msdm.sections) {
                if (go == null) continue;
                if (go.TryGetComponent<SectionData>(out var sd)) {
                    sd.isVisited = false;
                    sd.isCleared = false;
                    sd.isPlayerOn = false;
                    sd.isCanMove = false;
                    sd.UpdateSectionColor();
                }
            }
        }
        Debug.Log("[SaveLoad] NewGameClear: file deleted, in-memory save reset, section flags reset.");
    }

    private SaveData TakeSnapshot() {
        save.playerName = GameDataManager.Data.playerName;
        save.tutorialClear = GameDataManager.Data.tutorialClear;
        save.originSeed = GameDataManager.Data.seed;

        if(!MapSceneDataManager.Instance) return save;

        GameObject player = MapSceneDataManager.Instance.Player;
        if (player != null) save.playerPos = player.transform.position;

        PlayerControl pc = player.GetComponent<PlayerControl>();
        if (pc != null) {
            save.currentSectionId = pc.sectionData != null
                ? NormalizeId(pc.sectionData.id) : null;
            save.preSectionId     = pc.preSection != null
                ? NormalizeId(pc.preSection.GetComponent<SectionData>()?.id) : null;
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

    /// <summary>
    /// 계속하기(다시하기):
    /// - 저장을 읽고, StepManager에 "증가분만 1회 흡수" 프라임 호출(중복 방지)
    /// - 그 다음 씬/플레이어 상태를 복원
    /// </summary>
    public void LoadData() {
        if(TryLoad(out var data)) {
            GameDataManager.Instance?.ContinueSeed(data.originSeed);

            Debug.Log($"[SaveLoad] user = {data.playerName}, seed = {data.originSeed}");
            Debug.Log($"[SaveLoad] pos = {data.playerPos}");

            GameDataManager.Data.tutorialClear = data.tutorialClear;

            // ★★★ 여기서 '증가분만 흡수' 1회 처리 (중복 방지 프라임 포함)
            StepManager.Instance?.OnResumeAbsorbDeltaOnce();

            StartCoroutine(ApplyLoadedData(data));
        }
        else {
            Debug.Log("[SaveLoad] No save file");
        }
    }

    public void AddVisitedSectionIds(string id) {
        if (save == null) save = new SaveData();
        var norm = NormalizeId(id);
        if (!string.IsNullOrEmpty(norm) && !save.visitedSectionIds.Contains(norm)) {
            save.visitedSectionIds.Add(norm);
        }
    }

    public void AddClearedSectionIds(string id) {
        if (save == null) save = new SaveData();
        var norm = NormalizeId(id);
        if (!string.IsNullOrEmpty(norm) && !save.clearedSectionIds.Contains(norm)) {
            save.clearedSectionIds.Add(norm);
        }
    }

    public void AddCanMoveSectionIds(string id) {
        if (save == null) save = new SaveData();
        var norm = NormalizeId(id);
        if (!string.IsNullOrEmpty(norm) && !save.canMoveSectionIds.Contains(norm)) {
            save.canMoveSectionIds.Add(norm);
        }
    }

    public void OverwriteLocal(SaveData data, bool normalize = true)
    {
        if (data == null) return;

        if (normalize)
        {
            data.currentSectionId = NormalizeId(data.currentSectionId);
            data.preSectionId     = NormalizeId(data.preSectionId);
            if (data.visitedSectionIds != null)
            {
                for (int i = 0; i < data.visitedSectionIds.Count; i++)
                    data.visitedSectionIds[i] = NormalizeId(data.visitedSectionIds[i]);
            }
            if(data.clearedSectionIds != null) {
                for (int i = 0; i < data.clearedSectionIds.Count; i++)
                    data.clearedSectionIds[i] = NormalizeId(data.clearedSectionIds[i]);
            }
            if(data.canMoveSectionIds != null) {
                for (int i = 0; i < data.canMoveSectionIds.Count; i++)
                    data.canMoveSectionIds[i] = NormalizeId(data.canMoveSectionIds[i]);
            }
        }

        save = data;
        try
        {
            var json = JsonUtility.ToJson(save, prettyPrint);
            System.IO.File.WriteAllText(path, json);
            Debug.Log("[SaveLoad] OverwriteLocal OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveLoad] OverwriteLocal failed: {e.Message}");
        }
    }

    public void SaveNowAndUpload(string username, MonoBehaviour caller = null)
    {
        SaveNow();
        if (caller != null) caller.StartCoroutine(UploadCurrentSaveToServer(username));
        else                StartCoroutine(UploadCurrentSaveToServer(username));
    }

    public IEnumerator UploadCurrentSaveToServer(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("[SaveLoad] Upload skipped: username is empty");
            yield break;
        }

        string url = $"{GameDataManager.Data.baseUrl}:8081/api/save/{UnityWebRequest.EscapeURL(username)}";

        var data = TakeSnapshot();
        string json = JsonUtility.ToJson(data);

        using (var req = new UnityWebRequest(url, "PUT"))
        {
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            var token = AuthManager.Instance != null ? AuthManager.Instance.AccessToken : null;
            if (!string.IsNullOrEmpty(token))
                req.SetRequestHeader("Authorization", $"Bearer {token}");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300)
            {
                Debug.Log($"[SaveLoad] Upload OK ({req.responseCode})");
            }
            else
            {
                Debug.LogWarning($"[SaveLoad] Upload FAILED code={req.responseCode}, err={req.error}");
            }
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

        string curRaw  = save.currentSectionId;
        string curNorm = NormalizeId(curRaw);
        Debug.Log($"[Saved] current raw='{curRaw}', norm='{curNorm}', inScene={sceneIds.Contains(curNorm)}");

        string preRaw  = save.preSectionId;
        string preNorm = NormalizeId(preRaw);
        Debug.Log($"[Saved] previous raw='{preRaw}', norm='{preNorm}', inScene={sceneIds.Contains(preNorm)}");

        if (save.visitedSectionIds != null) {
            foreach (var v in save.visitedSectionIds) {
                var n = NormalizeId(v);
                Debug.Log($"[Saved] visited raw='{v}', norm='{n}', inScene={sceneIds.Contains(n)}");
            }
        }
        if (save.clearedSectionIds != null) {
            foreach (var v in save.clearedSectionIds) {
                var n = NormalizeId(v);
                Debug.Log($"[Saved] cleared raw='{v}', norm='{n}', inScene={sceneIds.Contains(n)}");
            }
        }

        if (save.canMoveSectionIds != null) {
            foreach (var v in save.canMoveSectionIds) {
                var n = NormalizeId(v);
                Debug.Log($"[Saved] canMove raw='{v}', norm='{n}', inScene={sceneIds.Contains(n)}");
            }
        }
    }
#endif

    // ★★★★★ 마지막 3구간만 남기기 + 확장자 제거 ★★★★★
    private static string NormalizeId(string raw) {
        if (string.IsNullOrWhiteSpace(raw)) return raw;

        string s = raw.Replace('\\', '/').Trim().TrimEnd('/');

        const string RES1 = "Assets/Resources/";
        const string RES2 = "Resources/";
        if (s.StartsWith(RES1, StringComparison.OrdinalIgnoreCase)) s = s.Substring(RES1.Length);
        else if (s.StartsWith(RES2, StringComparison.OrdinalIgnoreCase)) s = s.Substring(RES2.Length);

        var parts = s.Split('/');
        if (parts.Length == 0) return s;

        string last = parts[^1];
        int dot = last.LastIndexOf('.');
        if (dot > 0) last = last.Substring(0, dot);
        parts[^1] = last;

        int keep = 3;
        int start = Mathf.Max(0, parts.Length - keep);
        int len = parts.Length - start;
        return string.Join("/", parts, start, len);
    }
}