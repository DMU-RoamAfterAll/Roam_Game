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
            if (map.TryGetValue(id, out var sec)) {
                sec.isVisited = true;
                sec.LightObj();
            }
            else Debug.LogWarning($"[Load] visitedId '{raw}' -> '{id}' not found in map");
        }

        var pc = player != null ? player.GetComponent<PlayerControl>() : null;
        // PlayerControl 복원 (current / previous도 정규화): null;
        if (pc != null) {
            if (!string.IsNullOrEmpty(data.currentSectionId)) {
                var curId = NormalizeId(data.currentSectionId);  // ★
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
        GameDataManager.Instance?.NewSeed();

        DeleteSave();              // 파일 삭제
        save = new SaveData();     // 메모리 초기화

        StepManager.Instance.ResetAllSteps();
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
                    // 규칙에 따라 필요하면: sd.isCanMove = false;
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
            GameDataManager.Instance?.ContinueSeed(data.originSeed);
            
            Debug.Log($"[SaveLoad] user = {data.playerName}, seed = {data.originSeed}");
            Debug.Log($"[SaveLoad] pos = {data.playerPos}");

            GameDataManager.Data.tutorialClear = data.tutorialClear;

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

    // SaveLoadManager.cs 내부 아무 public 메서드들 아래에 추가
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

    // SaveLoadManager.cs 내부 (클래스 안)
    public void SaveNowAndUpload(string username, MonoBehaviour caller = null)
    {
        // 1) 로컬 세이브 즉시 반영(현재 위치/섹션 포함)
        SaveNow();

        // 2) 서버 업로드
        if (caller != null) caller.StartCoroutine(UploadCurrentSaveToServer(username));
        else                StartCoroutine(UploadCurrentSaveToServer(username));
    }

    /// <summary>
    /// 현재 Save 스냅샷을 서버에 PUT 업로드
    /// </summary>
    public IEnumerator UploadCurrentSaveToServer(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("[SaveLoad] Upload skipped: username is empty");
            yield break;
        }

        // 서버 URL (스웨거 경로에 맞춰 수정)
        string url = $"{GameDataManager.Data.baseUrl}:8081/api/save/{UnityWebRequest.EscapeURL(username)}";

        // 현재 상태 스냅샷(여기서 currentSectionId / preSectionId 정규화됨)
        var data = TakeSnapshot();
        string json = JsonUtility.ToJson(data);

        using (var req = new UnityWebRequest(url, "PUT"))
        {
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            // (선택) 인증 헤더
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

    // ★★★★★ 마지막 3구간만 남기기 + 확장자 제거 ★★★★★
    private static string NormalizeId(string raw) {
        if (string.IsNullOrWhiteSpace(raw)) return raw;

        // 슬래시 통일 + 앞/뒤 정리
        string s = raw.Replace('\\', '/').Trim().TrimEnd('/');

        // (선택) Resources 접두사 잘라내기
        const string RES1 = "Assets/Resources/";
        const string RES2 = "Resources/";
        if (s.StartsWith(RES1, System.StringComparison.OrdinalIgnoreCase)) s = s.Substring(RES1.Length);
        else if (s.StartsWith(RES2, System.StringComparison.OrdinalIgnoreCase)) s = s.Substring(RES2.Length);

        var parts = s.Split('/');
        if (parts.Length == 0) return s;

        // 마지막 세그먼트에서 확장자 제거
        string last = parts[^1];
        int dot = last.LastIndexOf('.');
        if (dot > 0) last = last.Substring(0, dot);
        parts[^1] = last;

        // 끝에서 3개 취합(부족하면 가능한 만큼)
        int keep = 3;
        int start = Mathf.Max(0, parts.Length - keep);
        int len = parts.Length - start;
        return string.Join("/", parts, start, len);
    }
}