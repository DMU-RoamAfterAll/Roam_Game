using UnityEngine;
using TMPro;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class StepManager : MonoBehaviour {
    [Header("UI")]
    public TMP_Text stepCountText;          // 하루 전체 걸음 수 표시
    public TMP_Text availableStepsText;     // 남은 걸음 수 표시

    #if UNITY_ANDROID && !UNITY_EDITOR
    private const string PERMISSION = "android.permission.ACTIVITY_RECOGNITION";
    private AndroidJavaObject stepPlugin;
    private bool isInitialized = false;
    #endif

    private int rawStepCount;
    public int availableSteps { get; private set; }

    void Start() {
        if (stepCountText       == null) stepCountText       = GameObject.Find("StepCountText")      .GetComponent<TMP_Text>();
        if (availableStepsText  == null) availableStepsText  = GameObject.Find("AvailableStepsText").GetComponent<TMP_Text>();

        Application.targetFrameRate = 60;

        #if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(PERMISSION))
            Permission.RequestUserPermission(PERMISSION);
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

            // 3) UI 출력
            UpdateUI();
        }
        #else
        UpdateUI();
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
            stepCountText.text = "StepSensor Init Fail!";
            availableStepsText.text = "N/A";
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
        stepCountText.text      = "Can't Attach Sensor";
        availableStepsText.text = "N/A";
        Debug.LogWarning("[StepManager] 걸음 센서가 감지되지 않았습니다.");
    }

    public void UpdateUI() {
        stepCountText.text       = $"Total Steps: {rawStepCount:N0}";
        availableStepsText.text  = $"Available Steps: {availableSteps:N0}";
    }

    #if UNITY_EDITOR

    [ContextMenu("+ 100 steps")]
    private void Add100Steps() {
        availableSteps += 100;
        UpdateUI();
    }
    #endif
}