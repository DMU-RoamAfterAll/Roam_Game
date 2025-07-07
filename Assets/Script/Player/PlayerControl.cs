using UnityEngine;

public class PlayerControl : MonoBehaviour {
    public GameObject hitObject;
    public SectionData hitSectionData;

    public GameObject preHitObject;

    void Start() {
        hitObject = GameDataManager.Instance.originSection;
    }

    void Update() {
        if(Input.GetMouseButtonDown(0)) {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if(hit.collider != null) {
                if(hit.collider.CompareTag(Tag.Section) || hit.collider.CompareTag(Tag.MainSection) || hit.collider.CompareTag(Tag.Origin)) {
                    preHitObject = hitObject;

                    hitObject = hit.collider.gameObject;
                    hitSectionData = hitObject.GetComponent<SectionData>();

                    MoveToObject(hitObject);

                    hitSectionData.isPlayerOn = true;

                    hitSectionData.ActiveSightSection();
                    hitSectionData.ActiveLinkSection();

                    if(preHitObject != GameDataManager.Instance.originSection) {
                        preHitObject.GetComponent<SectionData>().isPlayerOn = false;
                        preHitObject.GetComponent<SectionData>().ActiveLinkSection();
                    }
                }
                else if(hit.collider.CompareTag(Tag.LinkSection)) {
                    preHitObject = hitObject;

                    hitObject = hit.collider.GetComponent<LinkPosition>().linkedObject;
                    hitSectionData = hitObject.GetComponent<SectionData>();

                    MoveToObject(hitObject);

                    hitSectionData.isPlayerOn = true;

                    hitSectionData.ActiveSightSection();
                    hitSectionData.ActiveLinkSection();

                    if(preHitObject != GameDataManager.Instance.originSection) {
                        preHitObject.GetComponent<SectionData>().isPlayerOn = false;
                        preHitObject.GetComponent<SectionData>().ActiveLinkSection();
                    }
                }
            }
        }
    }

    void MoveToObject(GameObject hitCollider) {
        this.gameObject.transform.position = hitCollider.transform.position;
        this.gameObject.transform.SetParent(hitCollider.transform);

        GameDataManager.Instance.playerLocate = hitCollider.tag;
    }
}