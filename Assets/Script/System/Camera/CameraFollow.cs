using UnityEngine;

public class CameraFollow : MonoBehaviour {
    [Header("Data")]
    public Transform target;
    public float smoothSpeed;
    public float moveSpeed;
    public bool isLockOn;
    public Vector3 offset;

    private Vector2 lastTouchPosition;

    void Start() {
        target = MapSceneDataManager.Instance.Player.transform;
        
        smoothSpeed = 0.25f;
        moveSpeed = 500f;
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

    ///플레이어에게 카메라 고정
    void MoveCamera() {
        #if UNITY_EDITOR || UNITY_STANDALONE_OSX

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 cameraPosition = this.transform.position;

        cameraPosition.x += Time.deltaTime * horizontal * moveSpeed;
        cameraPosition.y += Time.deltaTime * vertical * moveSpeed;

        this.transform.position = new Vector3(cameraPosition.x, cameraPosition.y, -10f);

        #elif UNITY_IOS || UNITY_ANDROID

        if(Input.touchCount > 0) {
            Touch touch = Input.GetTouch(0);
            if(touch.phase == TouchPhase.Began) lastTouchPosition = touch.position;
            else if(touch.phase == TouchPhase.Moved) {
                Vector2 delta = touch.position - lastTouchPosition;
                Vector3 pos = this.transform.position;

                pos.x -= delta.x * moveSpeed * Time.deltaTime * 0.005f;
                pos.y -= delta.y * moveSpeed * Time.deltaTime * 0.005f;
                this.transform.position = pos;
                lastTouchPosition = touch.position;
            }
        }

        #else

        weatherText.text = "cant support platform";

        #endif
    }

    ///플레이어가 원할 때 시점 고정
    void LockOn() {
        if (target == null) {
            target = MapSceneDataManager.Instance.Player.transform;
        }

        Vector3 desiredPosition = target.position + offset;

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, -10f);
    }

    public void SwitchLockOn() {
        isLockOn = !isLockOn;
    }

    ///카메라 이동구역 설정
    void LimitMoveArea() {
        
    }
}