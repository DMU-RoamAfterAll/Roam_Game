using UnityEngine;

public class PlayerControl : MonoBehaviour {
    public float maxDistance;

    void Start() {
        maxDistance = GameDataManager.Data.initialMaxDistance;
    }

    void Update() {

    }

    void MovePlayerToSection() {
        if(Input.GetMouseButtonDown(0)) {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero);

            foreach(var hit in hits) {
                if(hit.collider.CompareTag(Tag.Section) || hit.collider.CompareTag(Tag.MainSection) || hit.collider.CompareTag(Tag.Origin)) {
                    this.transform.position = hit.collider.transform.position;
                }
            }


        }
    }

    public void DetectSection() {
        Collider2D[] hits = Physics2D.OverlapCircleAll(this.transform.position, maxDistance + 0.5f);

        foreach(var hit in hits) {
            if(hit.CompareTag(Tag.Section) || hit.CompareTag(Tag.MainSection) || hit.CompareTag(Tag.Origin)) {
                Debug.Log("CanMove");
                hit.gameObject.GetComponent<SectionData>().isCanMove = true;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(this.transform.position, maxDistance + 0.5f);
    }
}