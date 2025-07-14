using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerControl : MonoBehaviour {
    [Header("Section Data")]
    public GameObject preSection; //이전에 위치했던 Section
    public GameObject currentSection; //지금 위치하고있는 Section
    public SectionData sectionData; //위치하고 있는 Section의 SectionData 컴포넌트

    [Header("Game Data")]
    public float maxDistance;

    void Start() {
        currentSection = this.transform.parent.gameObject;
        maxDistance = GameDataManager.Data.initialMaxDistance;
    }

    void Update() {
        if(GameDataManager.Data.isMapSetUp) MovePlayerToSection(); //맵 생성이 안료되었을 때 이동 가능
    }

    ///클릭한 Section이 이미 방문아혔거나 감지범위 내일때 플레이어의 위치 이동 혹은 VirualSection을 통해서 이동
    void MovePlayerToSection() {
        if(Input.GetMouseButtonDown(0)) {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero); //마우스가 클릭한 위치에 따라 오브젝트 반환 (Collider2D 필요)

            foreach(var hit in hits) { //클릭한 위치에 있는 오브젝트 전부 반환
                if(hit.collider.CompareTag(Tag.Section) || hit.collider.CompareTag(Tag.MainSection) || hit.collider.CompareTag(Tag.Origin)) { //Section에 해당하는 태그들 확인
                    if(hit.collider.GetComponent<SectionData>().isCanMove || hit.collider.GetComponent<SectionData>().isVisited) { //클릭한 Section이 범위 안에 있는지 확인
                        Debug.Log("Move To Section");

                        preSection = this.transform.parent.gameObject;
                        currentSection = hit.collider.gameObject;
                        sectionData = currentSection.GetComponent<SectionData>(); //이전 오브젝트, 현재 오브젝트 저장 및 SectionData 컴포넌트 참조

                        Move(currentSection, sectionData);
                    }
                }
                if(hit.collider.CompareTag(Tag.VirtualSection)) {
                    preSection = this.transform.parent.gameObject;
                    currentSection = hit.collider.gameObject.GetComponent<VirtualSectionData>().truthSection;
                    sectionData = currentSection.GetComponent<SectionData>(); //최대거리를 넘는 Section인 경우 가상의 Section이 대신 상태변환을 대신해줌

                    Move(currentSection, sectionData);
                }
            }
        }
    }

    void Move(GameObject currentObj, SectionData sectionData) {
        this.transform.SetParent(currentObj.transform);
        this.transform.position = currentObj.transform.position; //Player의 위치 이동

        sectionData.SetPlayerOnSection();
        if(preSection.GetComponent<SectionData>() != null) preSection.GetComponent<SectionData>().SetPlayerOnSection(); //이동한 오브젝트의 상태 변환
        sectionData.SetSight(); //sight오브젝트 추가
        DetectSection();
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
    
        SectionData[] allSections = GameDataManager.Instance.sections //모든 Section가져오기
            .Concat(GameDataManager.Instance.mainSections)
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

    ///DetectSection을 시각화
    void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(this.transform.position, maxDistance);
    } 
}