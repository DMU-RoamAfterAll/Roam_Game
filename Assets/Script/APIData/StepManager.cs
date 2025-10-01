using UnityEngine;
using TMPro;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class StepManager : MonoBehaviour {
    public static StepManager Instance { get; private set; } //씬에서 모두 접근 가능하도록 Instance화

    #if UNITY_ANDROID && !UNITY_EDITOR
    private const string PERMISSION = "android.permission.ACTIVITY_RECOGNITION";
    private AndroidJavaObject stepPlugin;
    private bool isInitialized = false;
    #endif

    public int rawStepCount;
    public int availableSteps;

    void Awake() {
        if (Instance != null && Instance != this)  {
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(this.gameObject);

        Instance = this;
    }

    void Start() {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(PERMISSION))
            Permission.RequestUserPermission(PERMISSION);
        #elif UNITY_EDITOR
        rawStepCount = 9999;
        availableSteps = 9999;
        #endif
    }

    void Update() {
        #if UNITY_ANDROID && !UNITY_EDITOR
        // 권한 승인 후 한 번만 초기화
        if (Permission.HasUserAuthorizedPermission(PERMISSION) && !isInitialized)
            InitializePlugin();

        if (isInitialized && stepPlugin != null) {
            // 1) 플러그인에서 전체 걸음수 읽기
            int todayTotal = stepPlugin.Call<int>("getStepCount");

            // 2) 증가분(delta)을 계산해 raw와 available 모두 갱신
            int delta = todayTotal - rawStepCount;
            if (delta > 0) {
                rawStepCount    += delta;
                availableSteps  += delta;
            }
        }
        #endif
    }

    #if UNITY_ANDROID && !UNITY_EDITOR
    private void InitializePlugin() {
        try {
            using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                var activity = up.GetStatic<AndroidJavaObject>("currentActivity");
                stepPlugin = new AndroidJavaObject("com.ghgh10288.steptracker.StepTracker", activity);

                // 시작 시점: 플러그인 전체 걸음수와 동일하게 초기화
                rawStepCount   = stepPlugin.Call<int>("getStepCount");
                availableSteps = rawStepCount;

                isInitialized = true;
                Debug.Log($"[StepManager] 초기 걸음 수: {rawStepCount}");
            }
        }
        catch (System.Exception ex) {
            Debug.LogError($"[StepManager] 초기화 실패: {ex}");
        }
    }
    #endif

    /// 소비 메서드: availableSteps만 감소
    public bool TryConsumeSteps(int cost) {
        if (availableSteps >= cost) {
            availableSteps -= cost;
            return true;
        }
        return false;
    }

    // Java 측에서 센서를 찾지 못했을 때 호출
    public void OnStepSensorUnavailable() {
        Debug.LogWarning("[StepManager] 걸음 센서가 감지되지 않았습니다.");
    }

    #if UNITY_EDITOR

    [ContextMenu("+ 100 steps")]
    private void Add100Steps() {
        availableSteps += 100;
    }
    #endif
}