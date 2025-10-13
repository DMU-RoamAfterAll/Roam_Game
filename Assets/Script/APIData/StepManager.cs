using UnityEngine;
using System;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android; // ACTIVITY_RECOGNITION 권한
#endif

/// <summary>
/// 안드로이드 STEP_COUNTER 누적값을 읽어 "오늘 걸음수 잔액(availableSteps)"로 관리.
/// - 일일 리셋은 TimeManager.onNewDay 이벤트로만 수행
/// - 앱 시작 시 저장된 날짜와 오늘이 다르면 1회 초기화
/// - 기기 재부팅 등으로 센서 누적이 줄어들면 자동 재기준선
/// - 상태(PlayerPrefs) 영속 저장
/// </summary>
public class StepManager : MonoBehaviour
{
    public static StepManager Instance { get; private set; }

#if UNITY_ANDROID && !UNITY_EDITOR
    private const string PERMISSION = "android.permission.ACTIVITY_RECOGNITION";
    private AndroidJavaObject stepPlugin;
    private bool isInitialized = false;
#endif

    // ===== PlayerPrefs keys =====
    private const string KEY_BASELINE       = "step.baseline";       // 자정 기준선(센서 누적)
    private const string KEY_BASELINE_DATE  = "step.baselineDate";   // 기준선 날짜(yyyyMMdd)
    private const string KEY_AVAILABLE      = "step.available";      // 오늘 잔액(소비 반영)
    private const string KEY_LAST_TOTAL     = "step.lastTotal";      // 마지막으로 읽은 누적값

    // ===== 공개 상태 =====
    public int rawStepCount;    // 현재 센서 누적값(디버그)
    public int availableSteps;  // 오늘 사용 가능한 걸음 잔액

    // ===== 내부 상태 =====
    private int lastTotal;      // 직전 프레임의 누적값(재부팅 감지)

    // ===== Lifecycle =====
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SubscribeToTimeManager();
    }

    private void OnDisable()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.onNewDay.RemoveListener(OnNewDay);
    }

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(PERMISSION))
            Permission.RequestUserPermission(PERMISSION);
#else
        // 에디터/기타 플랫폼: 저장값 복원(테스트 편의상 기본 9999)
        availableSteps = PlayerPrefs.GetInt(KEY_AVAILABLE, 9999);
        rawStepCount   = PlayerPrefs.GetInt(KEY_LAST_TOTAL, 9999);
        lastTotal      = rawStepCount;
#endif
        // 앱 시작 시 "지난 저장 날짜 != 오늘"이면 1회 초기화
        OneShotDailyResetIfNeeded();
    }

    private void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // 권한 승인 이후 1회 초기화
        if (Permission.HasUserAuthorizedPermission(PERMISSION) && !isInitialized)
            InitializePlugin();

        if (!isInitialized || stepPlugin == null) return;

        // 1) 현재 누적값 읽기
        int total = ReadTotalFromPluginSafe();
        if (total < 0) return;

        // 2) 재부팅/리셋 감지 → 현재값으로 재기준선
        if (total < lastTotal)
        {
            Debug.LogWarning("[StepManager] Counter reset detected (reboot?). Re-baselining.");
            Rebaseline(total);
        }

        // 3) 증가분만큼 오늘 잔액 증가
        int delta = total - lastTotal;
        if (delta > 0)
        {
            availableSteps += delta;
            rawStepCount    = total;
            lastTotal       = total;
            Persist();
        }
#endif
    }

    // ===== TimeManager 연동 =====
    private void SubscribeToTimeManager()
    {
        // TimeManager가 이미 존재하면 바로 구독
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay.AddListener(OnNewDay);
        }
        else
        {
            // 씬 로딩 순서에 따라 늦게 뜰 수 있으니, 다음 프레임에 한 번 더 시도
            StartCoroutine(SubscribeNextFrame());
        }
    }

    private System.Collections.IEnumerator SubscribeNextFrame()
    {
        yield return null;
        if (TimeManager.Instance != null)
            TimeManager.Instance.onNewDay.AddListener(OnNewDay);
    }

    // TimeManager가 자정에 호출
    private void OnNewDay()
    {
        int currentTotal = GetCurrentTotalFallbackSafe(); // 플랫폼별 현재 누적값
        ApplyDailyReset(currentTotal);
        PlayerPrefs.SetString(KEY_BASELINE_DATE, Today());
        PlayerPrefs.Save();
        Debug.Log($"[StepManager] OnNewDay → baseline={currentTotal}, available=0");
    }

    // 앱 시작 시에만 1회 날짜 비교(오프라인 자정 경과 대비)
    private void OneShotDailyResetIfNeeded()
    {
        string today = Today();
        string savedDay = PlayerPrefs.GetString(KEY_BASELINE_DATE, today);

        if (savedDay != today)
        {
            int currentTotal = GetCurrentTotalFallbackSafe();
            ApplyDailyReset(currentTotal);
            PlayerPrefs.SetString(KEY_BASELINE_DATE, today);
            PlayerPrefs.Save();
            Debug.Log($"[StepManager] Startup daily reset → baseline={currentTotal}, available=0");
        }
        else
        {
            // 첫 실행 시 baseline/available/lastTotal 복원(없는 경우 기본)
            int baseline = PlayerPrefs.GetInt(KEY_BASELINE, GetCurrentTotalFallbackSafe());
            availableSteps = PlayerPrefs.GetInt(KEY_AVAILABLE, Mathf.Max(0, rawStepCount - baseline));
            lastTotal = PlayerPrefs.GetInt(KEY_LAST_TOTAL, rawStepCount);
        }
    }

    // ===== Android 초기화 =====
#if UNITY_ANDROID && !UNITY_EDITOR
    private void InitializePlugin()
    {
        try
        {
            using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = up.GetStatic<AndroidJavaObject>("currentActivity");
                stepPlugin = new AndroidJavaObject("com.ghgh10288.steptracker.StepTracker", activity);
            }

            int total = ReadTotalFromPluginSafe();
            if (total < 0) total = 0;

            rawStepCount = total;
            lastTotal    = PlayerPrefs.GetInt(KEY_LAST_TOTAL, total);

            // baseline/available 복원(없으면 baseline=현재, available=0)
            int baseline = PlayerPrefs.GetInt(KEY_BASELINE, total);
            availableSteps = PlayerPrefs.GetInt(KEY_AVAILABLE, Mathf.Max(0, total - baseline));

            isInitialized = true;
            Debug.Log($"[StepManager] Init → total={total}, baseline={baseline}, available={availableSteps}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[StepManager] InitializePlugin failed: {ex}");
        }
    }

    private int ReadTotalFromPluginSafe()
    {
        try { return stepPlugin?.Call<int>("getStepCount") ?? -1; }
        catch (Exception ex)
        {
            Debug.LogError($"[StepManager] getStepCount error: {ex}");
            return -1;
        }
    }
#endif

    // ===== 퍼블릭 API =====
    public bool TryConsumeSteps(int cost)
    {
        if (availableSteps >= cost)
        {
            availableSteps -= cost;
            Persist();
            return true;
        }
        return false;
    }

    public void OnStepSensorUnavailable()
    {
        Debug.LogWarning("[StepManager] 걸음 센서가 감지되지 않았습니다.");
    }

    // ===== 내부 유틸 =====
    private static string Today() => DateTime.Now.ToString("yyyyMMdd");

    /// <summary>자정 리셋 실제 처리(기준선=현재누적, 잔액=0, lastTotal/raw 업데이트)</summary>
    private void ApplyDailyReset(int currentTotal)
    {
        PlayerPrefs.SetInt(KEY_BASELINE, currentTotal);
        availableSteps = 0;
        lastTotal      = currentTotal;
        rawStepCount   = currentTotal;
        Persist();
    }

    /// <summary>센서 누적이 줄었을 때(재부팅 등) 현재값으로 재기준선</summary>
    private void Rebaseline(int currentTotal)
    {
        PlayerPrefs.SetInt(KEY_BASELINE, currentTotal);
        lastTotal    = currentTotal;
        rawStepCount = currentTotal;
        Persist();
    }

    /// <summary>상태 저장</summary>
    private void Persist()
    {
        PlayerPrefs.SetInt(KEY_AVAILABLE,  availableSteps);
        PlayerPrefs.SetInt(KEY_LAST_TOTAL, lastTotal);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 현재 누적값을 안전하게 얻기(플러그인이 없거나 에디터인 경우 rawStepCount 사용)
    /// </summary>
    private int GetCurrentTotalFallbackSafe()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        int t = ReadTotalFromPluginSafe();
        return (t >= 0) ? t : rawStepCount;
#else
        return rawStepCount;
#endif
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) Persist();
    }

    private void OnApplicationQuit()
    {
        Persist();
    }

    // ===== 에디터 편의 기능 =====
#if UNITY_EDITOR
    [ContextMenu("Debug/+100 steps")]
    private void Add100Steps()
    {
        availableSteps += 100;
        Persist();
        Debug.Log($"[StepManager] Debug add → available={availableSteps}");
    }

    [ContextMenu("Debug/Reset baseline to current total")]
    private void Debug_ResetBaselineToCurrent()
    {
        int current = rawStepCount;
        ApplyDailyReset(current);
        PlayerPrefs.SetString(KEY_BASELINE_DATE, Today());
        PlayerPrefs.Save();
        Debug.Log($"[StepManager] Baseline reset → baseline={current}, available=0");
    }

    [ContextMenu("Debug/Clear PlayerPrefs (step)")]
    private void Debug_ClearPrefs()
    {
        PlayerPrefs.DeleteKey(KEY_BASELINE);
        PlayerPrefs.DeleteKey(KEY_BASELINE_DATE);
        PlayerPrefs.DeleteKey(KEY_AVAILABLE);
        PlayerPrefs.DeleteKey(KEY_LAST_TOTAL);
        PlayerPrefs.Save();
        Debug.Log("[StepManager] Cleared step-related PlayerPrefs");
    }
#endif
}