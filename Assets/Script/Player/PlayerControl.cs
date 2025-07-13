using UnityEngine;

public class PlayerControl : MonoBehaviour {
    public GameObject preSection;
    public GameObject currentSection;
    public SectionData sectionData;
    public float maxDistance;

    void Start() {
        currentSection = this.transform.parent.gameObject;
        maxDistance = GameDataManager.Data.initialMaxDistance;
    }

    void Update() {
        MovePlayerToSection();
    }

    void MovePlayerToSection() {
        if(Input.GetMouseButtonDown(0)) {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero);

            foreach(var hit in hits) {
                if(hit.collider.CompareTag(Tag.Section) || hit.collider.CompareTag(Tag.MainSection) || hit.collider.CompareTag(Tag.Origin)) {
                    if(hit.collider.GetComponent<SectionData>().isCanMove) {
                        Debug.Log("Move To Section");

                        preSection = this.transform.parent.gameObject;
                        currentSection = hit.collider.gameObject;
                        sectionData = currentSection.GetComponent<SectionData>();

                        Move(currentSection, sectionData);
                    }
                }
                if(hit.collider.CompareTag(Tag.VirtualSection)) {
                    preSection = this.transform.parent.gameObject;
                    currentSection = hit.collider.gameObject.GetComponent<VirtualSectionData>().truthSection;
                    sectionData = currentSection.GetComponent<SectionData>();

                    Move(currentSection, sectionData);
                }
            }
        }
    }

    void Move(GameObject currentObj, SectionData sectionData) {
        this.transform.SetParent(currentObj.transform);
        this.transform.position = currentObj.transform.position;

        sectionData.SetPlayerOnSection();
        if(preSection.GetComponent<SectionData>() != null) preSection.GetComponent<SectionData>().SetPlayerOnSection();
        sectionData.SetSight();
        DetectSection();
    }

    public void DetectSection() {
        Collider2D[] hits = Physics2D.OverlapCircleAll(this.transform.position, maxDistance);

        foreach(var hit in hits) {
            if(hit.CompareTag(Tag.Section) || hit.CompareTag(Tag.MainSection) || hit.CompareTag(Tag.Origin)) {
                Debug.Log("CanMove");
                hit.gameObject.GetComponent<SectionData>().isCanMove = true;
            }
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(this.transform.position, maxDistance);
    } 
}