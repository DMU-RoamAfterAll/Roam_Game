using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FullScreenDarkness : MonoBehaviour {
    public Camera cam;
    public float margin = 0.5f;

    SpriteRenderer sr;

    void Awake() {
        sr = GetComponent<SpriteRenderer>();
        if (!cam) cam = Camera.main;
    }

    void LateUpdate() {
        if (!cam || !sr || !sr.sprite) return;

        float h = cam.orthographicSize * 2f;
        float w = h * cam.aspect;

        var s = sr.sprite.bounds.size;

        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, transform.position.z);

        transform.localScale = new Vector3((w / s.x) + margin, (h / s.y) + margin, 1f);
    }
}