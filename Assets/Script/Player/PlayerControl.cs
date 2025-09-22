using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class PlayerControl : MonoBehaviour {
    [Header("Section Data")]
    public GameObject preSection; //이전에 위치했던 Section
    public GameObject currentSection; //지금 위치하고있는 Section
    public SectionData sectionData; //위치하고 있는 Section의 SectionData 컴포넌트

    [Header("Game Data")]
    public float maxDistance;
    public CameraZoom cameraZoom;
    public LayerMask sectionMask;
    public LayerMask virtualSectionMask;

    public bool isCanMove;
    bool _confirmBusy;

    void Start() {
        isCanMove = false;
        _confirmBusy = false;
        currentSection = this.transform.parent.gameObject;

        maxDistance = MapSceneDataManager.mapData.initialMaxDistance;
        cameraZoom = MapSceneDataManager.Instance.cameraZoom;
    }

    void Update() {
        if(isCanMove) ClickSection(); //맵 생성이 안료되었을 때 이동 가능
    }

    ///클릭한 Section이 이미 방문아혔거나 감지범위 내일때 플레이어의 위치 이동 혹은 VirualSection을 통해서 이동
    void ClickSection() {
        // 1) 입력 처리 (마우스 클릭 또는 터치)
        bool inputDown =
            Input.GetMouseButtonDown(0)
            || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        if (!inputDown) return;

        if(IsPointerOverUI()) return;

        var cam = MapSceneDataManager.Instance.worldCamera != null
            ? MapSceneDataManager.Instance.worldCamera
            : Camera.main;
        Vector2 sp = (Input.touchCount > 0) ? (Vector2)Input.GetTouch(0).position
                                            : (Vector2)Input.mousePosition;
        Vector3 w3 = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, cam.nearClipPlane));
        Vector2 wp = new Vector3(w3.x, w3.y);

        var realHits = Physics2D.OverlapPointAll(wp, sectionMask);
        foreach(var col in realHits) {
            if(!col) continue;
            if(col.CompareTag(Tag.Section) || col.CompareTag(Tag.MainSection)
                || col.CompareTag(Tag.Origin) || col.CompareTag(Tag.IrisSection)) {
                    var sd = col.GetComponent<SectionData>();
                    if(sd != null) { _=HandleSectionClickAsync(col.gameObject, sd); }
                    return;
                }
        }

        if(virtualSectionMask.value != 0) {
            var vHits = Physics2D.OverlapPointAll(wp, virtualSectionMask);
            foreach(var col in vHits) {
                if(!col || !col.CompareTag(Tag.VirtualSection)) continue;
                var vsd = col.GetComponent<VirtualSectionData>();
                if(vsd && vsd.truthSection) {
                    var realSd = vsd.truthSection.GetComponent<SectionData>();
                    if(realSd) { _=HandleSectionClickAsync(vsd.truthSection, realSd); }
                    return;
                }
            }
        }
    

        // 2) 화면 좌표 → 월드 좌표 변환
        Vector2 screenPos = Input.GetMouseButtonDown(0)
            ? (Vector2)Input.mousePosition
            : Input.GetTouch(0).position;
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

        // 3) 클릭/터치 위치에 있는 섹션 판별
        foreach (var hit in Physics2D.RaycastAll(worldPos, Vector2.zero)) {
            // 일반 섹션 클릭 시
            if (hit.collider.CompareTag(Tag.Section)
            || hit.collider.CompareTag(Tag.MainSection)
            || hit.collider.CompareTag(Tag.Origin)
            || hit.collider.CompareTag(Tag.IrisSection)) {
                
                var sd = hit.collider.GetComponent<SectionData>();
                // ← 여기서 비용 체크

                _ = HandleSectionClickAsync(hit.collider.gameObject, sd);
                return;    
            }

            // 가상 섹션(범위 초과) 클릭 시
            if (hit.collider.CompareTag(Tag.VirtualSection)) {
                var vsd = hit.collider.GetComponent<VirtualSectionData>();
                var realSd = vsd.truthSection.GetComponent<SectionData>();
                // ← 동일하게 비용 체크
                _ = HandleSectionClickAsync(vsd.truthSection, realSd);
                return;
            }
        }
    }

    bool IsPointerOverUI() {
        if(EventSystem.current == null) return false;
        if(Input.touchCount > 0) return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return EventSystem.current.IsPointerOverGameObject();
    }

    async Task HandleSectionClickAsync(GameObject targetObj, SectionData sd) {
        cameraZoom.ZoomInSection(targetObj.transform.position);

        if(_confirmBusy) return;
        _confirmBusy = true;

        try {
            Debug.Log("Need StepCost = " + GetStepCost(sd.sectionPosition));

            bool ok = await MapSceneDataManager.Instance.enterBtnUI.ShowConfirmBtn("Move To Section?");
            if(!ok) return;

            if(!MapSceneDataManager.Instance.stepManagerUI.TryConsumeSteps(GetStepCost(sd.sectionPosition))) {
                cameraZoom.ZoomOutSection();
                Debug.Log("You Need More Step");
                return;
            }
            
            MoveToSection(targetObj, sd);
        }
        finally {
            _confirmBusy = false;
        }
    }

    void MoveToSection(GameObject currentObj, SectionData sectionD) {
        cameraZoom.ZoomOutSection();
        preSection = transform.parent.gameObject;
        currentSection = currentObj;
        sectionData = sectionD;

        this.transform.SetParent(currentObj.transform);
        this.transform.position = currentObj.transform.position; //Player의 위치 이동

        sectionD.SetPlayerOnSection();
        if(preSection.GetComponent<SectionData>() != null) preSection.GetComponent<SectionData>().SetPlayerOnSection(); //이동한 오브젝트의 상태 변환
        sectionD.SetOption();
        DetectSection();

        SwitchSceneManager.Instance.sectionPath = sectionD.id;
        SwitchSceneManager.Instance.MoveScene(SceneList.Story);
    }

    public void DetectSection() {
        Collider2D[] hits = Physics2D.OverlapCircleAll(this.transform.position, maxDistance); //Ray를 원형으로 생성

        HashSet<SectionData> detectedSections = new HashSet<SectionData>();

        foreach(var hit in hits) {
            if(hit.CompareTag(Tag.Section) || hit.CompareTag(Tag.MainSection) || hit.CompareTag(Tag.Origin)) {
                SectionData sd = hit.GetComponent<SectionData>();

                if(sd != null) detectedSections.Add(sd); //감지된 Section 저장
            }
        }
    
        SectionData[] allSections = MapSceneDataManager.Instance.sections //모든 Section가져오기
            .Concat(MapSceneDataManager.Instance.mainSections)
            .Select(go => go.GetComponent<SectionData>())
            .Where(sd => sd != null)
            .ToArray();

        foreach(var section in allSections) {
            bool canMove = false;

            // 감지된 Section이면 무조건 true
            if (detectedSections.Contains(section)) {
                canMove = true;
            }
            else {
                // LinkSection 검사
                LinkSection[] links = this.gameObject.GetComponents<LinkSection>();

                foreach (var link in links) {
                    if (link.linkedSection == section.gameObject) {
                        canMove = true;
                        break; // 하나라도 일치하면 더 볼 필요 없음
                    }
                }
            }

            // isCanMove 설정
            section.isCanMove = canMove;

            // 색상 업데이트
            section.UpdateSectionColor();
        }
    }

    int GetStepCost(Vector2 sectionPosition) {
        int step = (int)(Vector2.Distance(this.gameObject.transform.position, sectionPosition));

        return step * 10;
    }

    ///DetectSection을 시각화
    void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(this.transform.position, maxDistance);
    }
}