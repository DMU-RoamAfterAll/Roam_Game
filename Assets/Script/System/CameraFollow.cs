using UnityEngine;

public class CameraFollow : MonoBehaviour {
    public Transform target;           // 따라갈 대상
    public float smoothSpeed = 0.125f; // 부드럽게 따라가는 정도
    public Vector3 offset;             // 카메라 위치 오프셋

    void LateUpdate() {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.z = -10f; // 2D 카메라는 일반적으로 z축 -10에 위치

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}