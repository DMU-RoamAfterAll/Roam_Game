using UnityEngine;

public class PlayerControl : MonoBehaviour {
    void Update() {
        if(Input.GetMouseButtonDown(0)) {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if(hit.collider != null) {
                if(hit.collider.CompareTag("Section") || hit.collider.CompareTag("MainSection") || hit.collider.CompareTag("LinkSection")) {
                    this.gameObject.transform.position = hit.collider.gameObject.transform.position;
                }
            }
        }
    }
}