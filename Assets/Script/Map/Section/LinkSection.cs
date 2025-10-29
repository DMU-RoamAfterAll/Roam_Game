using UnityEngine;
using System.Collections.Generic;

// LinkSection.cs
public class LinkSection : MonoBehaviour {
    [Header("GameData")]
    public float minDistance;            // ← 실제로는 '도달 가능 거리' 의미로 사용
    public GameObject linkSectionPrefab;

    [Header("Section")]
    public GameObject linkedSection;     // 내가 이어주는 "상대 섹션(진짜)"
    public GameObject virtualSection;    // 플레이어가 클릭할 "가상 섹션"
    public SectionData sectionData;      // 상대 섹션의 SectionData (현재 구조 유지)

    void Start() {
        // 플레이어가 닿을 수 있는 반경으로 사용 (maxDistance가 맞음)
        minDistance      = MapSceneDataManager.mapData.initialMaxDistance;
        linkSectionPrefab = MapSceneDataManager.mapData.linkSectionPrefab;

        if (linkedSection) {
            sectionData = linkedSection.GetComponent<SectionData>();
            if (sectionData.linkSections == null) sectionData.linkSections = new List<LinkSection>();
            sectionData.linkSections.Add(this);
        }
    }

    void Update() {
        CreateVirtualSection();
    }

    public void CreateVirtualSection() {
        // 상대 섹션에 플레이어가 올라가 있을 때만
        if (sectionData != null && !sectionData.isNotSightOn && sectionData.isCanMove) {
            // 내(=이 LinkSection이 붙은 쪽)의 SectionData가 아직 방문 전일 때만
            var mySd = GetComponent<SectionData>();
            if (mySd != null && !mySd.isVisited && !mySd.isCanMove) {
                // 이미 만들어져 있으면 재생성 금지
                if (virtualSection != null) return;

                if (linkSectionPrefab == null) {
                    Debug.LogWarning("[LinkSection] linkSectionPrefab is null"); 
                    return;
                }

                // 상대(=linkedSection) 근처, 플레이어가 닿을 수 있는 지점으로 스폰
                Vector3 dir = (linkedSection.transform.position - transform.position).normalized;
                Vector3 pos = linkedSection.transform.position - dir * (minDistance - 0.1f);

                virtualSection = Instantiate(linkSectionPrefab, pos, Quaternion.identity);

                // 안전하게 태그/레이어 강제 (클릭 마스크 통과)
                virtualSection.tag = Tag.VirtualSection;
                int world = LayerMask.NameToLayer("World");
                if (world >= 0) virtualSection.layer = world;

                var vsd = virtualSection.GetComponent<VirtualSectionData>();
                if (vsd) vsd.truthSection = gameObject;
            }
        }
        else {
            // 필요 없으면 안전 파괴
            if (virtualSection != null) {
                Destroy(virtualSection);
                virtualSection = null;
            }
        }
    }
}