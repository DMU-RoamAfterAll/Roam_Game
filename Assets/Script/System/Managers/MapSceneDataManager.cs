using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Linq;

public class MapSceneDataManager : MonoBehaviour {
    public static MapSceneDataManager Instance { get; private set; }

    public MapSceneData mapSceneData;

    public GameObject Player;
    public GameObject originSection;
    public Camera worldCamera;
    public string playerLocate;

    public static MapSceneData mapData => Instance.mapSceneData;

    public List<GameObject> areaObjects;
    public List<GameObject> sections;
    public List<GameObject> mainSections;

    public StepManager stepManagerUI;
    public SectionEnterBtn enterBtnUI;
    public CameraZoom cameraZoom;
    public PlayerControl pc;

    public bool isPopUpOn;

    public SectionData riverSection;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        if (mapData == null) {
            Debug.LogError("MapSceneData is None");
            return;
        }

        // === 씬-로컬 검색 ===
        var myScene = this.gameObject.scene;
        var roots = myScene.GetRootGameObjects();

        Player = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                      .Select(t => t.gameObject)
                      .FirstOrDefault(go => go.CompareTag(Tag.Player));

        originSection = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                             .Select(t => t.gameObject)
                             .FirstOrDefault(go => go.CompareTag(Tag.Origin));

        var cameras = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).ToArray();
        worldCamera = cameras.FirstOrDefault(c => c.CompareTag(Tag.MainCamera));

        cameraZoom = roots.SelectMany(r => r.GetComponentsInChildren<CameraZoom>(true)) // ★ 오타 수정
                          .FirstOrDefault();

        stepManagerUI = StepManager.Instance;
        
        pc = Player.GetComponent<PlayerControl>();

        // enterBtnUI는 Start 코루틴에서 한 프레임 늦게 끌 것 (다른 스크립트가 켜는 것보다 나중에)
        // playerLocate 등 가벼운 값은 지금 세팅
        playerLocate = "origin";
        isPopUpOn = false;
    }

    // ★ 한 프레임 늦춰서 UI 토글/원격 카운트 등 처리
    System.Collections.IEnumerator Start() {
        yield return null; // 다른 Start/OnEnable 끝난 뒤 안전하게

        var myScene = this.gameObject.scene;
        var roots = myScene.GetRootGameObjects();

        enterBtnUI = roots.SelectMany(r => r.GetComponentsInChildren<SectionEnterBtn>(true)) // ★ 오타 수정
                          .FirstOrDefault();

        if (enterBtnUI != null) {
            enterBtnUI.gameObject.SetActive(false);
            Debug.Log("SetActive(false) EnterBtnUI (scene-local)");
        }
        else {
            Debug.LogWarning("EnterBtnUI not found in MapScene (scene-local search)");
        }

        // 원격 areaNumber 계산은 프리즈 방지 위해 try/catch
        try {
            // 동기 .Result는 가급적 피하세요 (UnityWebRequest 권장).
            // 당장 로직 유지하되 예외만 잡아줌.
            var html = new HttpClient { Timeout = System.TimeSpan.FromSeconds(5) }
                       .GetStringAsync($"{GameDataManager.Data.baseUrl}/CNWV/Resources/AreaAssetData/")
                       .Result;
            mapSceneData.areaNumber = Regex.Matches(
                html,
                @"href\s*=\s*""[^""]+\.json""",
                RegexOptions.IgnoreCase
            ).Count;
        }
        catch (System.Exception ex) {
            Debug.LogWarning($"[MapSceneDataManager] areaNumber fetch failed: {ex.Message}");
            // 실패 시 기본값 유지 혹은 fallback 값 지정 가능
        }

        if(AreAreasFullyCleared()) CheckClearChapterOne();
    }

    void OnEnable() { if(AreAreasFullyCleared()) CheckClearChapterOne(); }

    // 지정한 Area 인덱스들에 대해, 각 Area의 자식들 중 SectionData가 모두 isCleared == true 인지 검사
    public bool AreAreasFullyCleared(params int[] areaIndices)
    {
        if (areaObjects == null || areaObjects.Count == 0 || areaIndices == null || areaIndices.Length == 0)
            return false;

        foreach (var idx in areaIndices)
        {
            if (idx < 0 || idx >= areaObjects.Count)
            {
                Debug.LogWarning($"[MapSceneDataManager] AreAreasFullyCleared: index {idx} out of range");
                return false;
            }

            var areaRoot = areaObjects[idx];
            if (areaRoot == null) return false;

            // 비활성 포함해서 자식들 중 SectionData 전부 가져옴
            var sectionsInArea = areaRoot.GetComponentsInChildren<SectionData>(true);
            if (sectionsInArea == null || sectionsInArea.Length == 0)
            {
                Debug.LogWarning($"[MapSceneDataManager] Area {idx} has no SectionData children");
                return false;
            }

            // 하나라도 미클리어면 false
            foreach (var sd in sectionsInArea)
            {
                if (sd != null && !sd.isCleared)
                    return false;
            }
        }

        return true;
    }

    // 챕터1 클리어 판정 (예: 0,1,5번 Area 모두 클리어인지)
    public void CheckClearChapterOne()
    {
        bool cleared = AreAreasFullyCleared(0, 1, 5);
        Debug.Log($"[MapSceneDataManager] Chapter One Cleared = {cleared}");

        
    }
}