using UnityEngine;

public class CameraZoom : MonoBehaviour {
    public Camera _camera;

    float minCameraSize = 5f;
    float maxCameraSize = 30f;
    
    void Start() {
        _camera = this.gameObject.GetComponent<Camera>();
    }

    void Update() {
        Zoom();
    }
    public void ZoomInSection(Vector2 targetTransform) {
        this.gameObject.transform.position = new Vector3(targetTransform.x, targetTransform.y, -10f);
        _camera.orthographicSize = minCameraSize;
    }

    public void ZoomOutSection() {
        this.gameObject.transform.localPosition = Vector3.zero;
        _camera.orthographicSize = maxCameraSize;
    }

    void Zoom() {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if(scroll != 0) {
            float currentSize = _camera.orthographicSize;
            if(scroll > 0) {
                _camera.orthographicSize -= 1f;
            }
            else if(scroll < 0) {
                _camera.orthographicSize += 1f;
            }

            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, minCameraSize, maxCameraSize);
        }
    }
}