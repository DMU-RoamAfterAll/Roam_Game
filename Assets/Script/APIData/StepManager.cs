using UnityEngine;
using System;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android; // ACTIVITY_RECOGNITION 권한
#endif

#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// 걸음수 관리:
/// - 매 프레임 센서 누적(total)을 읽어 증가분만 availableSteps에 누적
/// - "새로하기": 오늘 누적 전체를 즉시 적용(availableSteps = todayEarned), 중복 방지 프라임
/// - "다시하기": 마지막 종료 시점 대비 증가분만 1회 흡수, 중복 방지 프라임
/// - 자정에는 baseline/날짜 갱신 + 모드 프라임 서명 초기화
/// </summary>
public class StepManager : MonoBehaviour
{
    public static StepManager Instance { get; private set; }
    public event Action<int> AvailableStepsChanged;

#if UNITY_ANDROID && !UNITY_EDITOR
    private const string PERMISSION = "android.permission.ACTIVITY_RECOGNITION";
    private AndroidJavaObject stepPlugin;
    private bool isInitialized = false;
#endif

    // ===== PlayerPrefs keys =====
    private const string KEY_BASELINE      = "step.baseline";       // 자정 기준선(센서 누적)
    private const string KEY_BASELINE_DATE = "step.baselineDate";   // 기준선 날짜(yyyyMMdd)
    private const string KEY_AVAILABLE     = "step.available";      // 오늘 잔액(소비 반영)
    private const string KEY_LAST_TOTAL    = "step.lastTotal";      // 마지막으로 읽은 누적값

    // 모드 프라임(중복 적용 방지)용 서명
    //   값을 "mode:yyyyMMdd:total" 형태로 저장. 동일하면 해당 모드 처리를 스킵.
    private const string KEY_PRIME_SIG     = "step.prime.signature";

    // ===== 공개 상태 =====
    public int rawStepCount;    // 현재 센서 누적값(디버그)
    public int availableSteps;  // 오늘 사용 가능한 걸음 잔액

    // ==== 공개 / 세션 상태(선택) ====
    public int sessionSteps { get; private set; }
    private int sessionLastTotal;

    // ===== 내부 상태 =====
    private int lastTotal;      // 직전 프레임의 누적값(재부팅/리셋 감지)

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
        sessionSteps = 0;
#endif

#if UNITY_IOS && !UNITY_EDITOR
        try {
            if (IOSPedometer.iOS_Pedometer_IsSupported()) {
                IOSPedometer.iOS_Pedometer_Start();
                PrimeIOSLastTotal(); // iOS 첫 프레임 전 프라임
            }
        } catch (Exception ex) {
            Debug.LogError($"[StepManager][iOS] init error: {ex}");
        }
#endif

        OneShotDailyResetIfNeeded(); // baseline/available/lastTotal 복원 또는 일일 초기화
        AvailableStepsChanged?.Invoke(availableSteps);
    }

    private void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Permission.HasUserAuthorizedPermission(PERMISSION) && !isInitialized)
            InitializePlugin();

        if (!isInitialized || stepPlugin == null) return;

        int total = ReadTotalFromPluginSafe();
        if (total < 0) return;

        // 재부팅/리셋 감지
        if (total < lastTotal) {
            Debug.LogWarning("[StepManager] Counter reset detected (reboot?). Re-baselining.");
            Rebaseline(total);
            sessionLastTotal = total;
            AvailableStepsChanged?.Invoke(availableSteps);
        }

        // 증가분 누적
        int delta = total - lastTotal;
        if (delta > 0) {
            rawStepCount  = total;
            lastTotal     = total;
            availableSteps += delta;
            Persist();
            AvailableStepsChanged?.Invoke(availableSteps);
        }

        // 세션 집계
        int sessionDelta = total - sessionLastTotal;
        if (sessionDelta > 0) {
            sessionSteps += sessionDelta;
            sessionLastTotal = total;
        }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        if (!iosInitialized) {
            PrimeIOSLastTotal();
            return;
        }

        int total = 0;
        try { total = IOSPedometer.iOS_Pedometer_GetTodaySteps(); }
        catch (Exception ex) {
            Debug.LogError($"[StepManager][iOS] GetTodaySteps error : {ex}");
            total = rawStepCount;
        }

        // 자정/리셋 감지
        if (total < lastTotal) {
            Rebaseline(total);
            sessionLastTotal = total;
            AvailableStepsChanged?.Invoke(availableSteps);
        }

        // 증가분 누적
        int delta = total - lastTotal;
        if (delta > 0) {
            rawStepCount  = total;
            lastTotal     = total;
            availableSteps += delta;
            Persist();
            AvailableStepsChanged?.Invoke(availableSteps);
        }

        // 세션 집계
        int sessionDelta = total - sessionLastTotal;
        if (sessionDelta > 0) {
            sessionSteps     += sessionDelta;
            sessionLastTotal  = total;
        }
#endif
    }

    // ===== TimeManager 연동 =====
    private void SubscribeToTimeManager()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.onNewDay.AddListener(OnNewDay);
        else
            StartCoroutine(SubscribeNextFrame());
    }

    private System.Collections.IEnumerator SubscribeNextFrame()
    {
        yield return null;
        if (TimeManager.Instance != null)
            TimeManager.Instance.onNewDay.AddListener(OnNewDay);
    }

    // 자정 이벤트
    private void OnNewDay()
    {
        int currentTotal = GetCurrentTotalFallbackSafe();

        // 기준선/날짜 갱신 + 오늘 잔액 0으로 (게임 규칙)
        ApplyDailyReset(currentTotal);

        // 모드 프라임 서명 초기화(새 날엔 새로 적용 가능)
        PlayerPrefs.DeleteKey(KEY_PRIME_SIG);
        PlayerPrefs.SetString(KEY_BASELINE_DATE, Today());
        PlayerPrefs.Save();

        Debug.Log($"[StepManager] OnNewDay → baseline={currentTotal}, available=0 (prime cleared)");
    }

    // 앱 시작 시 하루 경계 확인
    private void OneShotDailyResetIfNeeded()
    {
        string today = Today();
        string savedDay = PlayerPrefs.GetString(KEY_BASELINE_DATE, today);

        if (savedDay != today) {
            int currentTotal = GetCurrentTotalFallbackSafe();
            ApplyDailyReset(currentTotal);
            PlayerPrefs.SetString(KEY_BASELINE_DATE, today);
            PlayerPrefs.DeleteKey(KEY_PRIME_SIG); // 어제 서명 제거
            PlayerPrefs.Save();
            Debug.Log($"[StepManager] Startup daily reset → baseline={currentTotal}, available=0");
        }
        else {
            // 복원
            int baseline = PlayerPrefs.GetInt(KEY_BASELINE, GetCurrentTotalFallbackSafe());
            availableSteps = PlayerPrefs.GetInt(KEY_AVAILABLE, Mathf.Max(0, rawStepCount - baseline));
            lastTotal = PlayerPrefs.GetInt(KEY_LAST_TOTAL, rawStepCount);
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void InitializePlugin()
    {
        try {
            using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                var activity = up.GetStatic<AndroidJavaObject>("currentActivity");
                stepPlugin = new AndroidJavaObject("com.ghgh10288.steptracker.StepTracker", activity);
            }

            int total = ReadTotalFromPluginSafe();
            if (total < 0) total = 0;

            sessionLastTotal = total;
            sessionSteps     = 0;

            rawStepCount = total;
            lastTotal    = PlayerPrefs.GetInt(KEY_LAST_TOTAL, total);

            int baseline = PlayerPrefs.GetInt(KEY_BASELINE, total);
            availableSteps = PlayerPrefs.GetInt(KEY_AVAILABLE, Mathf.Max(0, total - baseline));

            isInitialized = true;
            Debug.Log($"[StepManager] Init → total={total}, baseline={baseline}, available={availableSteps}");
        }
        catch (Exception ex) {
            Debug.LogError($"[StepManager] InitializePlugin failed: {ex}");
        }
    }

    private int ReadTotalFromPluginSafe()
    {
        try { return stepPlugin?.Call<int>("getStepCount") ?? -1; }
        catch (Exception ex) {
            Debug.LogError($"[StepManager] getStepCount error: {ex}");
            return -1;
        }
    }
#endif

#if UNITY_IOS && !UNITY_EDITOR
    private bool iosInitialized = false;

    internal static class IOSPedometer {
        [DllImport("__Internal")] public static extern bool iOS_Pedometer_IsSupported();
        [DllImport("__Internal")] public static extern void iOS_Pedometer_Start();
        [DllImport("__Internal")] public static extern void iOS_Pedometer_Stop();
        [DllImport("__Internal")] public static extern int  iOS_Pedometer_GetTodaySteps();
    }

    private void PrimeIOSLastTotal()
    {
        int today = 0;
        try { today = IOSPedometer.iOS_Pedometer_GetTodaySteps(); }
        catch { today = rawStepCount; }

        rawStepCount     = today;
        lastTotal        = PlayerPrefs.GetInt(KEY_LAST_TOTAL, today);
        sessionLastTotal = lastTotal;

        iosInitialized = true;
    }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    private void PrimeAndroidLastTotal()
    {
        int total = ReadTotalFromPluginSafe();
        if (total < 0) total = rawStepCount;
        rawStepCount     = total;
        lastTotal        = PlayerPrefs.GetInt(KEY_LAST_TOTAL, total);
        sessionLastTotal = lastTotal;
    }
#endif

    // ===== 퍼블릭 소비 API =====
    public bool TryConsumeSteps(int cost)
    {
        if (availableSteps >= cost) {
            availableSteps -= cost;
            Persist();
            AvailableStepsChanged?.Invoke(availableSteps);
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

    /// 기준선=현재누적, 잔액=0으로
    private void ApplyDailyReset(int currentTotal)
    {
        PlayerPrefs.SetInt(KEY_BASELINE, currentTotal);
        availableSteps = 0;
        lastTotal      = currentTotal;
        rawStepCount   = currentTotal;
        Persist();
        AvailableStepsChanged?.Invoke(availableSteps);
    }

    private void Rebaseline(int currentTotal)
    {
        PlayerPrefs.SetInt(KEY_BASELINE, currentTotal);
        lastTotal        = currentTotal;
        rawStepCount     = currentTotal;
        sessionLastTotal = currentTotal;
        Persist();
    }

    private void Persist()
    {
        PlayerPrefs.SetInt(KEY_AVAILABLE,  availableSteps);
        PlayerPrefs.SetInt(KEY_LAST_TOTAL, lastTotal);
        PlayerPrefs.Save();
    }

    private int GetCurrentTotalFallbackSafe()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        int t = -1;
        try { t = ReadTotalFromPluginSafe(); } catch { }
        return (t >= 0) ? t : rawStepCount;
#elif UNITY_IOS && !UNITY_EDITOR
        try { return IOSPedometer.iOS_Pedometer_GetTodaySteps(); }
        catch { return rawStepCount; }
#else
        return rawStepCount;
#endif
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) {
            Persist();
#if UNITY_IOS && !UNITY_EDITOR
            if (iosInitialized) IOSPedometer.iOS_Pedometer_Stop();
#endif
        } else {
#if UNITY_IOS && !UNITY_EDITOR
            if (IOSPedometer.iOS_Pedometer_IsSupported()) {
                IOSPedometer.iOS_Pedometer_Start();
                PrimeIOSLastTotal();
            }
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            if (isInitialized && stepPlugin != null) {
                PrimeAndroidLastTotal();
            }
#endif
        }
    }

    private void OnApplicationQuit()
    {
        Persist();
    }

    public int GetTodayEarnedSteps()
    {
        int total    = GetCurrentTotalFallbackSafe();
        int baseline = PlayerPrefs.GetInt(KEY_BASELINE, total);
        return Mathf.Max(0, total - baseline);
    }

    public int GetTodayUsedSteps()
    {
        int earned = GetTodayEarnedSteps();
        return Mathf.Max(0, earned - availableSteps);
    }

    public void ResetAllSteps()
    {
        int currentTotal = GetCurrentTotalFallbackSafe();
        PlayerPrefs.SetInt(KEY_BASELINE, currentTotal);
        PlayerPrefs.SetString(KEY_BASELINE_DATE, Today());

        availableSteps   = 0;
        sessionSteps     = 0;
        sessionLastTotal = currentTotal;
        lastTotal        = currentTotal;
        rawStepCount     = currentTotal;

        PlayerPrefs.DeleteKey(KEY_PRIME_SIG);

        Persist();
        AvailableStepsChanged?.Invoke(availableSteps);

        Debug.Log($"[StepManager] ResetAllSteps → baseline={currentTotal}, available=0, session=0 (prime cleared)");
    }

    // ====== 모드 프라임 ======

    private static string BuildPrimeSignature(string mode, string day, int total)
        => $"{mode}:{day}:{total}";

    private static bool IsSamePrime(string sig)
        => PlayerPrefs.GetString(KEY_PRIME_SIG, "") == sig;

    private static void SavePrime(string sig)
    {
        PlayerPrefs.SetString(KEY_PRIME_SIG, sig);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 새로하기(New Game):
    /// - 오늘 자정 이후 누적을 즉시 적용 → availableSteps = todayEarned
    /// - 다음 프레임 중복가산 방지(lastTotal = total) + 프라임 서명
    /// - **여러 번 호출해도 1회만 적용**
    /// </summary>
    public void OnNewGamePrimeToday()
    {
        int total  = GetCurrentTotalFallbackSafe();
        string day = Today();
        string sig = BuildPrimeSignature("newgame", day, total);

        if (IsSamePrime(sig)) {
            // 이미 같은 상태에서 처리됨 → 스킵
            rawStepCount     = total;
            lastTotal        = total;
            sessionLastTotal = total;
            Persist();
            AvailableStepsChanged?.Invoke(availableSteps);
            Debug.Log($"[StepManager] NewGame skipped (prime={sig})");
            return;
        }

        // 오늘 누적(= total - baseline)으로 바로 설정
        int baseline = PlayerPrefs.GetInt(KEY_BASELINE, total);
        int todayEarned = Mathf.Max(0, total - baseline);
        availableSteps = todayEarned;

        // 중복가산 방지 프라임
        rawStepCount     = total;
        lastTotal        = total;
        sessionLastTotal = total;

        SavePrime(sig);
        Persist();
        AvailableStepsChanged?.Invoke(availableSteps);

        Debug.Log($"[StepManager] NewGame → available={availableSteps} (todayEarned={todayEarned}), prime={sig}");
    }

    /// <summary>
    /// 다시하기(Continue):
    /// - 마지막 종료 시점(lastTotal) 대비 증가분만 1회 흡수
    /// - 다음 프레임 중복가산 방지(lastTotal = total) + 프라임 서명
    /// - **여러 번 호출해도 1회만 적용**
    /// </summary>
    public void OnResumeAbsorbDeltaOnce()
    {
        int total  = GetCurrentTotalFallbackSafe();
        string day = Today();
        string sig = BuildPrimeSignature("resume", day, total);

        if (IsSamePrime(sig)) {
            rawStepCount     = total;
            lastTotal        = total;
            sessionLastTotal = total;
            Persist();
            AvailableStepsChanged?.Invoke(availableSteps);
            Debug.Log($"[StepManager] Resume skipped (prime={sig})");
            return;
        }

        int pending = Mathf.Max(0, total - lastTotal);
        if (pending > 0) {
            availableSteps += pending;
        }

        rawStepCount     = total;
        lastTotal        = total;
        sessionLastTotal = total;

        SavePrime(sig);
        Persist();
        AvailableStepsChanged?.Invoke(availableSteps);

        Debug.Log($"[StepManager] Resume → +{pending}, available={availableSteps}, prime={sig}");
    }
}