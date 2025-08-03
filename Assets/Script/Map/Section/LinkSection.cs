using UnityEngine;

///최대거리 밖에 있는 Section이 가지고 있는 스크립트
public class LinkSection : MonoBehaviour {
    [Header("GameData")]
    public float minDistance;
    public GameObject linkSectionPrefab;

    [Header("Section")]
    public GameObject linkedSection;
    public GameObject virtualSection;
    public SectionData sectionData;

    void Start() {
        minDistance = MapSceneDataManager.mapData.initialMinDistance;
        linkSectionPrefab = MapSceneDataManager.mapData.linkSectionPrefab;

        sectionData = linkedSection.GetComponent<SectionData>();
        sectionData.linkSections.Add(this);
    }

    ///최대거리 밖에 있는 Section에 이동해야할 경우 임시로 VirualSection 생성
    public void CreateVirtualSection() {
        if(sectionData.isPlayerOn) {
            if (!this.GetComponent<SectionData>().isVisited) {
                Vector3 direction = (linkedSection.transform.position - transform.position).normalized;
                Vector3 createPosition = linkedSection.transform.position - direction * minDistance;

                virtualSection = Instantiate(linkSectionPrefab, createPosition, Quaternion.identity);
                virtualSection.GetComponent<VirtualSectionData>().truthSection = this.gameObject;
            }
        }
        else {
            Destroy(virtualSection);
        }
    }
}