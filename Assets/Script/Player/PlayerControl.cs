using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public class PlayerControl : MonoBehaviour {
    [Header("Section Data")]
    public GameObject preSection;
    public GameObject currentSection;
    public SectionData sectionData;

    [Header("Game Data")]
    public float maxDistance;
    public CameraZoom cameraZoom;

    [Header("Input Masks (auto set by names below)")]
    public LayerMask sectionMask;

    [SerializeField] string[] sectionLayerNames = { "World" };

    [Header("Debug")]
    [SerializeField] bool debugClicks = true;

    public bool isCanMove;
    bool _confirmBusy;

    // --- 자동 마스크 세팅 ---
    void Reset()      { AutoAssignMasks(); }
    void OnValidate() { AutoAssignMasks(); }
    void Awake()      { AutoAssignMasks(); }

    void Start() {
        isCanMove   = false;
        _confirmBusy = false;
        if (transform.parent) currentSection = transform.parent.gameObject;

        maxDistance = MapSceneDataManager.mapData.initialMaxDistance;
        cameraZoom  = MapSceneDataManager.Instance.cameraZoom;

        if (debugClicks) {
            Debug.Log($"[PC] Start: sectionMask={sectionMask.value} ({MaskToLayers(sectionMask)}), " +
                      $"layerNames=[{string.Join(",", sectionLayerNames)}]");
        }
    }

    void Update() {
        if (isCanMove) ClickSection();
    }

    void AutoAssignMasks() {
        sectionMask = NamesToLayerMask(sectionLayerNames);
        if (debugClicks) {
            Debug.Log($"[PC] AutoAssignMasks → sectionMask={sectionMask.value} ({MaskToLayers(sectionMask)})");
        }
    }

    static LayerMask NamesToLayerMask(params string[] names) {
        int mask = 0;
        if (names == null) return 0;
        foreach (var n in names) {
            if (string.IsNullOrWhiteSpace(n)) continue;
            int li = LayerMask.NameToLayer(n);
            if (li >= 0) mask |= 1 << li;
            else Debug.LogWarning($"[PC] Physics Layer '{n}' not found.");
        }
        return mask;
    }

    static string MaskToLayers(LayerMask mask) {
        if (mask.value == 0) return "(none)";
        var layers = Enumerable.Range(0, 32).Where(i => (mask.value & (1 << i)) != 0).Select(LayerMask.LayerToName);
        return string.Join(",", layers);
    }

    // 포인터가 UI 위에 있는지 + 어떤 UI들이 막는지까지 로그
    bool IsPointerOverUI(Vector2 screenPos) {
        if (EventSystem.current == null) return false;

        var ped = new PointerEventData(EventSystem.current) { position = screenPos };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);

        if (debugClicks && results.Count > 0) LogUIRaycastResults(results);

        return results.Count > 0; // 하나라도 맞으면 UI가 막는 중
    }

    // 디테일 로그: 이름, 계층 경로, 레이어, raycastTarget, CanvasGroup 설정 등
    void LogUIRaycastResults(List<RaycastResult> results) {
        Debug.Log($"[PC][UI] Raycast hits = {results.Count}");
        for (int i = 0; i < results.Count; i++) {
            var go = results[i].gameObject;
            var graphic = go ? go.GetComponent<Graphic>() : null;
            var cg = go ? go.GetComponentInParent<CanvasGroup>() : null;

            string path = GetHierarchyPath(go ? go.transform : null);
            string layer = go ? LayerMask.LayerToName(go.layer) : "(null)";
            bool rt = graphic ? graphic.raycastTarget : false;
            string cgInfo = cg ? $"blocksRaycasts={cg.blocksRaycasts}, ignoreParentGroups={cg.ignoreParentGroups}" : "(no CanvasGroup)";

            Debug.Log($"  #{i} name='{go?.name}', path='{path}', layer='{layer}', raycastTarget={rt}, {cgInfo}");
        }
    }

    // 깔끔한 경로 출력용
    string GetHierarchyPath(Transform t) {
        if (t == null) return "(null)";
        var parts = new List<string>();
        while (t != null) { parts.Add(t.name); t = t.parent; }
        parts.Reverse();
        return string.Join("/", parts);
    }

    // ----- 클릭 처리 -----
    void ClickSection() {
        // 0) 입력 감지
        bool inputDown = Input.GetMouseButtonDown(0) ||
                         (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        if (!inputDown) return;

        if (debugClicks) {
            string src = Input.touchCount > 0 ? "TOUCH" : "MOUSE";
            Debug.Log($"[PC] Click start ({src})");
        }

        Vector2 sp = (Input.touchCount > 0) ? (Vector2)Input.GetTouch(0).position
                                            : (Vector2)Input.mousePosition;

        // 1) UI 위면 차단 (여기서 막히면 Darkness UI가 raycastTarget=true일 수 있음)
        if (IsPointerOverUI(sp)) {
            if (debugClicks) Debug.Log("[PC] Blocked by UI raycast (Image.raycastTarget=true?)");
            return;
        }

        // 2) 카메라 선택/좌표 변환
        var cam = MapSceneDataManager.Instance.worldCamera != null
            ? MapSceneDataManager.Instance.worldCamera
            : Camera.main;

        if (cam == null) {
            Debug.LogWarning("[PC] No Camera found (worldCamera & Camera.main are null)");
            return;
        }

        // 오쏘그래픽이면 nearClip도 무시되지만, 안전하게 스크린→월드 변환
        Vector3 w3 = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, Mathf.Abs(cam.nearClipPlane)));
        Vector2 wp = new Vector2(w3.x, w3.y);

        if (debugClicks) {
            Debug.Log($"[PC] Cam='{cam.name}', ortho={cam.orthographic}, near={cam.nearClipPlane}, far={cam.farClipPlane}");
            Debug.Log($"[PC] Screen({sp.x:F1},{sp.y:F1}) -> World({wp.x:F2},{wp.y:F2})");
            Debug.Log($"[PC] sectionMask={sectionMask.value} ({MaskToLayers(sectionMask)})");
        }

        // 3) OverlapPointAll (LayerMask로 필터)
        var hits = Physics2D.OverlapPointAll(wp, sectionMask);
        if (debugClicks) Debug.Log($"[PC] OverlapPointAll hits={hits.Length}");

        foreach (var col in hits) {
            if (!col) continue;
            var go = col.gameObject;
            string info = $"name='{go.name}', tag='{go.tag}', layer='{LayerMask.LayerToName(go.layer)}'";
            if (debugClicks) Debug.Log("[PC]  - hit " + info);

            if (go.CompareTag(Tag.Section) || go.CompareTag(Tag.MainSection) ||
                go.CompareTag(Tag.Origin)  || go.CompareTag(Tag.IrisSection)) {
                var sd = go.GetComponent<SectionData>();
                if (sd != null) {
                    if (debugClicks) Debug.Log($"[PC] Section matched → HandleSectionClickRoutine id='{sd.id}'");
                    StartCoroutine(HandleSectionClickRoutine(go, sd));
                    return;
                } else if (debugClicks) {
                    Debug.Log("[PC]   (no SectionData component)");
                }
            }

            if (go.CompareTag(Tag.VirtualSection)) {
                var vsd    = go.GetComponent<VirtualSectionData>();
                var real   = vsd ? vsd.truthSection : null;
                var realSd = real ? real.GetComponent<SectionData>() : null;
                if (real && realSd) {
                    if (debugClicks) Debug.Log($"[PC] Virtual matched → real='{real.name}', id='{realSd.id}'");
                    StartCoroutine(HandleSectionClickRoutine(real, realSd));;
                    return;
                }
            }
        }

        // 4) 보조: RaycastAll (마스크 무시, 태그로만 탐지) — 진짜 막혔는지 진단용
        var rayHits = Physics2D.RaycastAll(wp, Vector2.zero);
        if (debugClicks) Debug.Log($"[PC] Physics2D.RaycastAll (no mask) hits={rayHits.Length}");
        foreach (var h in rayHits) {
            var go = h.collider ? h.collider.gameObject : null;
            if (!go) continue;
            if (debugClicks) {
                Debug.Log($"[PC]  - RC hit name='{go.name}', tag='{go.tag}', layer='{LayerMask.LayerToName(go.layer)}'");
            }

            if (go.CompareTag(Tag.Section) || go.CompareTag(Tag.MainSection) ||
                go.CompareTag(Tag.Origin)  || go.CompareTag(Tag.IrisSection)) {
                var esd = go.GetComponent<SectionData>();
                if (esd != null) {
                    if (debugClicks) Debug.Log($"[PC] (RC) Section matched → HandleSectionClickRoutine id='{esd.id}'");
                    StartCoroutine(HandleSectionClickRoutine(go, esd));;
                    return;
                }
            }

            if (go.CompareTag(Tag.VirtualSection)) {
                var vsd    = go.GetComponent<VirtualSectionData>();
                var real   = vsd ? vsd.truthSection : null;
                var realSd = real ? real.GetComponent<SectionData>() : null;
                if (real && realSd) {
                    if (debugClicks) Debug.Log($"[PC] (RC) Virtual matched → real='{real.name}', id='{realSd.id}'");
                    StartCoroutine(HandleSectionClickRoutine(real, realSd));
                    return;
                }
            }
        }

        if (debugClicks) Debug.Log("[PC] No section found under pointer.");
    }

    IEnumerator WaitTaskBool(Task<bool> task) {
        while(!task.IsCompleted) yield return null;
    }

    IEnumerator HandleSectionClickRoutine(GameObject targetObj, SectionData sd) {
        if (debugClicks) Debug.Log($"[PC] HandleSectionClickAsync → target='{targetObj.name}', id='{sd?.id}'");
        // 2) 줌인
        cameraZoom.ZoomInSection(targetObj.transform.position);

        // 3) 중복 입력 방지
        if (_confirmBusy) yield break;
        _confirmBusy = true;

        try {
            int cost = GetStepCost(sd.transform.position);
            Debug.Log($"[PC] Need StepCost = {cost}");

            // 4) Task<bool> → 코루틴 대기
            var confirmTask = MapSceneDataManager.Instance.enterBtnUI.ShowConfirmBtn("Move To Section?", cost);
            yield return WaitTaskBool(confirmTask);
            bool ok = confirmTask.Result;

            if (!ok) {
                if (debugClicks) Debug.Log("[PC] move canceled");
                cameraZoom.ZoomOutSection();
                yield break;
            }

            if (!MapSceneDataManager.Instance.stepManagerUI.TryConsumeSteps(cost)) {
                Debug.Log("[PC] Not enough steps");
                cameraZoom.ZoomOutSection();
                yield break;
            }

            // 5) 실제 섹션 이동
            bool enterStory = !(sd != null && sd.isCleared);
            StartCoroutine(MoveToSection(targetObj, sd, 0.5f, enterStory));
        }
        finally {
            _confirmBusy = false;
        }
    }

    IEnumerator MoveToSection(GameObject toObj, SectionData toSd, float duration = 0.5f, bool enterStory = true) {
        cameraZoom.ZoomOutSection();
        
        var fromObj = transform.parent ? transform.parent.gameObject : null;
        var fromSd = fromObj ? fromObj.GetComponent<SectionData>() : null;

        preSection = fromObj;
        currentSection = toObj;
        sectionData = toSd;

        Vector3 startPos = transform.position;
        Vector3 endPos = toObj.transform.position;
        float t = 0f;
        while (t < 1f) {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        transform.SetParent(toObj.transform, true);
        transform.position = endPos;

        yield return new WaitForSeconds(0.3f);

        if(fromSd != null) fromSd.SetPlayerOn(false);
        toSd.SetPlayerOn(true);
        DetectSection();

        if(enterStory) {
            toSd.SetOption();
            GameDataManager.Instance.sectionPath = toSd.id;
            SwitchSceneManager.Instance.MoveScene(SceneList.Story);
        }
        else {
            SaveLoadManager.Instance?.SaveNow();
            SaveLoadManager.Instance?.SaveNowAndUpload(GameDataManager.Data.playerName);
        }
    }

    public void DetectSection() {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, maxDistance);
        if (debugClicks) Debug.Log($"[PC] DetectSection around {transform.position} r={maxDistance} hits={hits.Length}");

        var detectedSections = hits
            .Select(h => h ? h.GetComponent<SectionData>() : null)
            .Where(sd => sd != null)
            .ToHashSet();

        var allSections = MapSceneDataManager.Instance.sections
            .Concat(MapSceneDataManager.Instance.mainSections)
            .Select(go => go ? go.GetComponent<SectionData>() : null)
            .Where(sd => sd != null)
            .ToArray();

        foreach (var section in allSections) {
            bool canMove = detectedSections.Contains(section);

            if (!canMove) {
                // ❌ 기존: foreach (var link in GetComponents<LinkSection>())
                // ✅ 수정: 현재 섹션의 링크들로 확인
                var cur = sectionData; // 현재 플레이어가 서 있는 섹션
                if (cur != null && cur.linkSections != null) {
                    foreach (var link in cur.linkSections) {
                        if (link && link.linkedSection == section.gameObject) { 
                            canMove = true; 
                            break; 
                        }
                    }
                }
            }

            section.isCanMove = canMove;
            section.UpdateSectionColor();
        }
    }

    int GetStepCost(Vector2 sectionPosition) {
        int step = (int)(Vector2.Distance(this.transform.position, sectionPosition));
        return step;
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}