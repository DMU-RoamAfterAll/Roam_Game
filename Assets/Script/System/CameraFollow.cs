using UnityEngine;

public class CameraFollow : MonoBehaviour {
    [Header("Data")]
    public Transform target;
    public float smoothSpeed;
    public float moveSpeed;
    public bool isLockOn;
    public Vector3 offset;

    void Start() {
        smoothSpeed = 0.25f;
        moveSpeed = 20f;
        isLockOn = true;
    }

    void Update() {
        MoveCamera();
    }

    void LateUpdate() {
        if(isLockOn) {
            LockOn();
        }
    }

    void MoveCamera() {
        #if UNITY_EDITOR || UNITY_STANDALONE_OSX

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 cameraPosition = this.transform.position;

        cameraPosition.x += Time.deltaTime * horizontal * moveSpeed;
        cameraPosition.y += Time.deltaTime * vertical * moveSpeed;

        this.transform.position = cameraPosition;

        #elif UNITY_IOS || UNITY_ANDROID


        #else

        weatherText.text = "cant support platform";

        #endif
    }

    void LockOn() {
        if (target == null) {
            target = GameDataManager.Instance.Player.transform;
        }

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.z = -10f; // 2D 카메라는 일반적으로 z축 -10에 위치

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }

    public void SwitchLockOn() {
        isLockOn = !isLockOn;
    }

    void LimitMoveArea() {

    }
}