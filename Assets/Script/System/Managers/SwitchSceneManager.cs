using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SwitchSceneManager : MonoBehaviour {
    public static SwitchSceneManager Instance { get; private set; }

    public string sectionPath;

    [Header("Base Scene (항상 유지하는 씬)")]
    [SerializeField] private string baseSceneName = SceneList.Map; // "MapScene"

    [Header("Boot 설정")]
    [SerializeField] private bool autoLoadBaseOnStart = false;     // ← 기본 false
    [SerializeField] private bool unloadBootAfterEnterBase = true; // 버튼 진입 후 Boot 언로드할지

    private readonly List<string> overlayStack = new List<string>();
    private bool _busy;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start() {
        // 자동 로드를 끔: BootScene만 보이게 유지
        if (autoLoadBaseOnStart)
            yield return EnsureBaseLoaded();  // 필요하면 켜서 사용
        else
            yield break;
    }

    // ====== BootScene 버튼에서 호출할 진입 메서드 ======
    public void EnterBaseFromBoot()
    {
        if (_busy) return;
        StartCoroutine(CoEnterBaseFromBoot());
    }

    private IEnumerator CoEnterBaseFromBoot() {
        _busy = true;

        // 1) Map Additive 로드
        var baseScene = SceneManager.GetSceneByName(baseSceneName);
        if (!baseScene.isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(baseSceneName, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
            baseScene = SceneManager.GetSceneByName(baseSceneName);
        }

        // 2) Boot 중복 컴포넌트 비활성
        var boot = SceneManager.GetActiveScene(); // 지금은 Boot가 Active
        foreach (var root in boot.GetRootGameObjects())
        {
            foreach (var es in root.GetComponentsInChildren<EventSystem>(true))
                es.gameObject.SetActive(false);
            foreach (var al in root.GetComponentsInChildren<AudioListener>(true))
                al.enabled = false;
        }

        // 3) Map 활성화
        SetSceneRootActive(baseSceneName, true);
        SceneManager.SetActiveScene(baseScene);
        DisableEnterBtnUIInScene(baseSceneName);

        // 4) (옵션) Boot 언로드
        if (unloadBootAfterEnterBase) {
            var unload = SceneManager.UnloadSceneAsync(boot);
            while (unload != null && !unload.isDone) yield return null;
        }

        _busy = false;
    }

    // ====== 기존 Overlay 라우팅 ======
    public void MoveScene(string targetScene)
    {
        if (string.IsNullOrEmpty(targetScene)) return;

        if (targetScene == baseSceneName)
        {
            if (!_busy) StartCoroutine(CoPopAllToBase());
            return;
        }

        if (!_busy) StartCoroutine(CoPushOverlay(targetScene));
    }

    public void MoveScene(SceneName target) => MoveScene(target.ToString());

    private IEnumerator EnsureBaseLoaded()
    {
        var baseScene = SceneManager.GetSceneByName(baseSceneName);
        if (!baseScene.isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(baseSceneName, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
            baseScene = SceneManager.GetSceneByName(baseSceneName);
        }

        SetSceneRootActive(baseSceneName, true);
        SceneManager.SetActiveScene(baseScene);
    }

    private IEnumerator CoPushOverlay(string overlayScene)
    {
        _busy = true;

        var load = SceneManager.LoadSceneAsync(overlayScene, LoadSceneMode.Additive);
        while (!load.isDone) yield return null;

        if (overlayStack.Count == 0)
            SetSceneRootActive(baseSceneName, false);

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(overlayScene));

        overlayStack.Add(overlayScene);
        _busy = false;
    }

    private IEnumerator CoPopOverlay()
    {
        _busy = true;

        if (overlayStack.Count == 0) { _busy = false; yield break; }

        string top = overlayStack[overlayStack.Count - 1];
        var unload = SceneManager.UnloadSceneAsync(top);
        while (unload != null && !unload.isDone) yield return null;

        overlayStack.RemoveAt(overlayStack.Count - 1);

        if (overlayStack.Count > 0)
        {
            string newTop = overlayStack[overlayStack.Count - 1];
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(newTop));
        }
        else
        {
            SetSceneRootActive(baseSceneName, true);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(baseSceneName));
            DisableEnterBtnUIInScene(baseSceneName);
        }

        _busy = false;
    }

    private IEnumerator CoPopAllToBase()
    {
        _busy = true;

        for (int i = overlayStack.Count - 1; i >= 0; i--)
        {
            string top = overlayStack[i];
            var unload = SceneManager.UnloadSceneAsync(top);
            while (unload != null && !unload.isDone) yield return null;
        }
        overlayStack.Clear();

        SetSceneRootActive(baseSceneName, true);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(baseSceneName));
        DisableEnterBtnUIInScene(baseSceneName);

        _busy = false;
    }

    private static void SetSceneRootActive(string sceneName, bool active)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded) return;

        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            roots[i].SetActive(active);
    }

    public void CloseTopOverlay()
    {
        if (!_busy) StartCoroutine(CoPopOverlay());
    }

    private static void DisableEnterBtnUIInScene(string sceneName)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded) return;

        foreach (var root in scene.GetRootGameObjects())
        {
            var btn = root.GetComponentInChildren<SectionEnterBtn>(true);
            if (btn != null)
            {
                btn.gameObject.SetActive(false);
                Debug.Log($"[SwitchSceneManager] EnterBtnUI disabled in scene '{sceneName}' → {btn.gameObject.name}");
                break;
            }
        }
    }

    public static void GoToMapScene() {
        Instance?.MoveScene(SceneList.Map);

        // (선택) 스토리 종료 처리: 현재 섹션 클리어, 튜토리얼 완료, 저장
        var msdm = MapSceneDataManager.Instance;
        var player = msdm != null ? msdm.Player : null;
        var pc = player ? player.GetComponent<PlayerControl>() : null;

        if (pc != null && pc.sectionData != null) {
            pc.sectionData.isCleared = true;

            var tuto = pc.sectionData.GetComponent<TutorialManager>();
            if (tuto != null) tuto.CompleteSection();
        }

        SaveLoadManager.Instance?.SaveNow();

        WeatherManager.Instance.HiddenEvent();
    }

    public static void GoToMissionScene() {
        Instance?.MoveScene(SceneList.Mission);
    }

    public static void GoToTitileScene() {
        SceneManager.LoadScene(SceneList.Boot);
        
    }

    #if UNITY_EDITOR
    // 🔧 에디터 메뉴는 에디터 전용으로 유지
    [MenuItem("Tools/Scenes/GoTo MapScene")]
    private static void GoToMapSceneMenu() => GoToMapScene();

    [MenuItem("Tools/Scenes/GoTo MissionScene")]
    private static void GoToMissionSceneMenu() => GoToMissionScene();
    
    #endif
}