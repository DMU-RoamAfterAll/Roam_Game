using UnityEngine;

public class LinkSection : MonoBehaviour {
    public GameObject linkedSection;
    public GameObject virtualSection;
    public GameObject linkSectionPrefab;

    public SectionData sectionData;

    public float minDistance;

    void Start() {
        sectionData = linkedSection.GetComponent<SectionData>();

        minDistance = GameDataManager.Data.initialMinDistance;
        linkSectionPrefab = GameDataManager.Data.linkSectionPrefab;

        sectionData.linkSections.Add(this);
    }

    public void AdjustPosition() {
        if(sectionData.isPlayerOn) {
            if (!this.GetComponent<SectionData>().isCanMove) {
                Vector3 direction = (linkedSection.transform.position - transform.position).normalized;
                Vector3 spawnPosition = linkedSection.transform.position - direction * minDistance;

                virtualSection = Instantiate(linkSectionPrefab, spawnPosition, Quaternion.identity);
                virtualSection.GetComponent<VirtualSectionData>().truthSection = this.gameObject;
            }
        }
        else {
            Destroy(virtualSection);
        }
    }
}