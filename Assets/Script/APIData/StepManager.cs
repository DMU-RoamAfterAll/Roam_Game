using UnityEngine;
using System;
using System.Collections;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android; // ACTIVITY_RECOGNITION 권한
#endif

#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// StepManager
/// - 센서의 "누적 걸음 수(total)"를 주기적으로 읽어 증가분(delta)만 availableSteps(게임 잔액)에 반영
/// - "새로하기"는 오늘 번 전체를 즉시 적용(available = todayEarned), 프라임 서명으로 중복 방지
/// - "다시하기"는 종료~재시작 사이 오프라인 증가분만 1회 흡수(스냅샷 차이), 프라임+세션 가드로 중복 방지
/// - 자정에는 baseline/날짜 갱신 및 프라임 초기화
/// - iOS/Android/에디터 모두 안전하게 동작하도록 폴링/초기화 순서를 정리
/// </summary>
public class StepManager : MonoBehaviour
{
    public static StepManager Instance { get; private set; }
    public event Action<int> AvailableStepsChanged;

    // ===== PlayerPrefs keys =====
    private const string KEY_BASELINE         = "step.baseline";         // 자정 기준선(센서 누적 total)
    private const string KEY_BASELINE_DATE    = "step.baselineDate";     // 기준선 날짜(yyyyMMdd)
    private const string KEY_AVAILABLE        = "step.available";        // 오늘 남은 걸음 잔액(게임 내 소비 반영)
    private const string KEY_LAST_TOTAL       = "step.lastTotal";        // 마지막으로 읽어 반영한 센서 누적 total
    private const string KEY_PRIME_SIG        = "step.prime.signature";  // 모드 프라임(중복 방지) 서명
    private const string KEY_EARNED_SNAPSHOT  = "step.earned.snapshot";  // 오늘 번 전체 스냅샷(= total - baseline)

    // ===== 공개 상태 =====
    public int rawStepCount;     // 현재 센서 누적값(디버그 표시용)
    public int availableSteps;   // 오늘 사용 가능한 걸음 잔액(게임에서 소비)
    public int sessionSteps { get; private set; }  // 앱 실행 이후 세션에서 증가한 총량(선택적 지표)

    // ===== 내부 상태 =====
    private int lastTotal;        // 직전 반영한 센서 누적값
    private int sessionLastTotal; // 세션 집계를 위한 직전 누적값

#if UNITY_ANDROID && !UNITY_EDITOR
    private const string PERMISSION = "android.permission.ACTIVITY_RECOGNITION";
    private AndroidJavaObject stepPlugin;
    private bool isInitialized = false;
#endif

#if UNITY_IOS && !UNITY_EDITOR
    private bool iosInitialized = false;
    internal static class IOSPedometer {
        [DllImport("__Internal")] public static extern bool iOS_Pedometer_IsSupported();
        [DllImport("__Internal")] public static extern void iOS_Pedometer_Start();
        [DllImport("__Internal")] public static extern void iOS_Pedometer_Stop();
        [DllImport("__Internal")] public static extern int  iOS_Pedometer_GetTodaySteps();
    }
#endif

    // StepManager 클래스 내부 상단
    private bool _readyForPrime = false;
    public  bool IsReadyForPrime => _readyForPrime;

    private bool _suppressNextPoll = false;

    private enum PendingPrime { None, NewGame, Resume }
    private PendingPrime _pendingPrime = PendingPrime.None;

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

#if UNITY_ANDROID && !UNITY_EDITOR
    private void TryStartSensorListener()
    {
        // 플러그인에 start() 메서드가 있을 때만
        try { stepPlugin?.Call("start"); } catch { /* no-op */ }
    }
#endif

    private IEnumerator Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(PERMISSION))
            Permission.RequestUserPermission(PERMISSION);
        while (!Permission.HasUserAuthorizedPermission(PERMISSION))
            yield return null;

        InitializePlugin();
        TryStartSensorListener();
#endif

#if UNITY_IOS && !UNITY_EDITOR
        try {
            if (IOSPedometer.iOS_Pedometer_IsSupported()) {
                IOSPedometer.iOS_Pedometer_Start();
                PrimeIOSLastTotal();
            }
        } catch (Exception ex) { Debug.LogError($"[StepManager][iOS] init error: {ex}"); }
#endif
        // Start() 코루틴의 마지막 부분 직전/직후에 배치
        OneShotDailyResetIfNeeded();

        // (기존 마이그레이션 코드 유지)
        var sig = PlayerPrefs.GetString(KEY_PRIME_SIG, "");
        if (sig.Split(':').Length >= 3) {
            PlayerPrefs.DeleteKey(KEY_PRIME_SIG);
            PlayerPrefs.Save();
        }

        // ★ 센서 total/플러그인 초기화가 끝났으므로 Prime 실행 가능
        _readyForPrime = true;

        // 대기 중인 Prime이 있다면 즉시 1회 처리
        FlushPendingPrimeIfAny();

        AvailableStepsChanged?.Invoke(availableSteps);
        yield break;
    }

    // ===== TimeManager 연동 =====
    private void SubscribeToTimeManager()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.onNewDay.AddListener(OnNewDay);
        else
            StartCoroutine(SubscribeNextFrame());
    }

    private IEnumerator SubscribeNextFrame()
    {
        yield return null;
        if (TimeManager.Instance != null)
            TimeManager.Instance.onNewDay.AddListener(OnNewDay);
    }

    // ===== 자정 이벤트 =====
    private void OnNewDay()
    {
        int currentTotal = GetCurrentTotalFallbackSafe();

        // 기준선/날짜 갱신 + 오늘 잔액 0
        ApplyDailyReset(currentTotal);

        // 모드 프라임 초기화
        PlayerPrefs.DeleteKey(KEY_PRIME_SIG);
        PlayerPrefs.SetString(KEY_BASELINE_DATE, Today());
        PlayerPrefs.Save();

        Debug.Log($"[StepManager] OnNewDay → baseline={currentTotal}, available=0 (prime cleared)");
    }

    // ===== 앱 시작 시 하루 경계 확인 =====
    private void OneShotDailyResetIfNeeded()
    {
        string today = Today();
        string savedDay = PlayerPrefs.GetString(KEY_BASELINE_DATE, today);

        int currentTotal = GetCurrentTotalFallbackSafe();

        if (savedDay != today) {
            ApplyDailyReset(currentTotal);
            PlayerPrefs.SetString(KEY_BASELINE_DATE, today);
            PlayerPrefs.DeleteKey(KEY_PRIME_SIG);
            PlayerPrefs.Save();
            Debug.Log($"[StepManager] Startup daily reset → baseline={currentTotal}, available=0");
        }
        else {
            // 안전 복구
            rawStepCount = currentTotal;

            // baseline이 없다면 현 total을 기준선으로 저장(최초 실행)
            if (!PlayerPrefs.HasKey(KEY_BASELINE)) {
                PlayerPrefs.SetInt(KEY_BASELINE, currentTotal);
                PlayerPrefs.Save();
            }

            // availableSteps: 저장 없으면 0으로 시작(중복 가산 방지)
            availableSteps = PlayerPrefs.HasKey(KEY_AVAILABLE)
                ? PlayerPrefs.GetInt(KEY_AVAILABLE)
                : 0;

            // lastTotal: 저장 없으면 현재 total로 프라임(대기 중 증가분 0)
            lastTotal = PlayerPrefs.HasKey(KEY_LAST_TOTAL)
                ? PlayerPrefs.GetInt(KEY_LAST_TOTAL)
                : currentTotal;

            // earned 스냅샷도 현재로 맞춰둠
            UpdateEarnedSnapshot(currentTotal);
        }
    }

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

    /// <summary>하루 초기화: 기준선=현재누적, 잔액=0</summary>
    private void ApplyDailyReset(int currentTotal)
    {
        PlayerPrefs.SetInt(KEY_BASELINE, currentTotal);
        availableSteps   = 0;
        lastTotal        = currentTotal;
        rawStepCount     = currentTotal;
        sessionSteps     = 0;
        sessionLastTotal = currentTotal;

        UpdateEarnedSnapshot(currentTotal);
        Persist();
        AvailableStepsChanged?.Invoke(availableSteps);
    }

    /// <summary>특정 total을 기준으로 재베이스라인</summary>
    private void Rebaseline(int currentTotal)
    {
        PlayerPrefs.SetInt(KEY_BASELINE, currentTotal);
        lastTotal        = currentTotal;
        rawStepCount     = currentTotal;
        sessionLastTotal = currentTotal;

        UpdateEarnedSnapshot(currentTotal);
        Persist();
    }

    /// <summary>현재 에러 없이 total을 얻되, 실패 시 rawStepCount로 폴백</summary>
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
        // 에디터/기타 플랫폼: rawStepCount를 시뮬레이션 소스로 사용
        return rawStepCount;
#endif
    }

    /// <summary>스냅샷: 오늘 번 전체(= total - baseline)를 저장</summary>
    private void UpdateEarnedSnapshot(int currentTotal)
    {
        int baseline  = PlayerPrefs.GetInt(KEY_BASELINE, currentTotal);
        int earnedNow = Mathf.Max(0, currentTotal - baseline);
        PlayerPrefs.SetInt(KEY_EARNED_SNAPSHOT, earnedNow);
        PlayerPrefs.Save();
    }

    private void Persist()
    {
        PlayerPrefs.SetInt(KEY_AVAILABLE,  availableSteps);
        PlayerPrefs.SetInt(KEY_LAST_TOTAL, lastTotal);
        PlayerPrefs.Save();
    }

    // ===== Pause/Resume =====
    private void OnApplicationPause(bool pause)
    {
        if (pause) {
            Persist();
#if UNITY_IOS && !UNITY_EDITOR
            if (iosInitialized) IOSPedometer.iOS_Pedometer_Stop();
#endif
        } else {
            _resumeAppliedThisSession = false; // 세션 가드 해제(다시하기 시 1회 허용)

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

    // ===== 조회 헬퍼 =====
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

    // ===== 전체 리셋(디버그/옵션 버튼 등에서 사용) =====
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

        UpdateEarnedSnapshot(currentTotal);
        Persist();
        AvailableStepsChanged?.Invoke(availableSteps);

        Debug.Log($"[StepManager] ResetAllSteps → baseline={currentTotal}, available=0, session=0 (prime cleared)");
    }

    // ===== 프라임 서명 유틸 =====
    private static string BuildPrimeSignature(string mode, string day) => $"{mode}:{day}";
    private static bool IsSamePrime(string sig) => PlayerPrefs.GetString(KEY_PRIME_SIG, "") == sig;
    private static void SavePrime(string sig) { PlayerPrefs.SetString(KEY_PRIME_SIG, sig); PlayerPrefs.Save(); }

    // ===== 새로하기(New Game): 오늘 번 전체를 즉시 적용 =====
    public void OnNewGamePrimeToday()
    {
        if (!_readyForPrime) { _pendingPrime = PendingPrime.NewGame; return; }
        OnNewGamePrimeToday_Internal();
    }

    private void OnNewGamePrimeToday_Internal()
    {
        int total  = GetCurrentTotalFallbackSafe();
        string day = Today();
        string sig = BuildPrimeSignature("newgame", day);

        // ★ baseline을 한 번만 선언(공용으로 사용)
        int baseline = PlayerPrefs.GetInt(KEY_BASELINE, total);

        if (IsSamePrime(sig)) {
            int earned = Mathf.Max(0, total - baseline);

            // 강제 리필 + 동기화
            availableSteps   = earned;
            rawStepCount     = total;
            lastTotal        = total;
            sessionLastTotal = total;

            PlayerPrefs.SetInt(KEY_AVAILABLE,       availableSteps);
            PlayerPrefs.SetInt(KEY_LAST_TOTAL,      lastTotal);
            PlayerPrefs.SetInt(KEY_EARNED_SNAPSHOT, earned);
            PlayerPrefs.Save();

            AvailableStepsChanged?.Invoke(availableSteps);
            _suppressNextPoll = true;  // 직후 폴링 1프레임 차단
            return;
        }

        // 새로하기 정상 처리
        int todayEarned = Mathf.Max(0, total - baseline);
        availableSteps   = todayEarned;
        rawStepCount     = total;
        lastTotal        = total;
        sessionLastTotal = total;

        PlayerPrefs.SetInt(KEY_AVAILABLE,       availableSteps);
        PlayerPrefs.SetInt(KEY_LAST_TOTAL,      lastTotal);
        PlayerPrefs.SetInt(KEY_EARNED_SNAPSHOT, todayEarned);
        SavePrime(sig);
        PlayerPrefs.Save();

        AvailableStepsChanged?.Invoke(availableSteps);
        _suppressNextPoll = true;     // 폴링 1프레임 차단
        ClampAvailableToEarned();     // 상한(earned)만 보정
    }

    private bool _resumeAppliedThisSession = false;

    public void OnResumeAbsorbDeltaOnce()
    {
        if (!_readyForPrime) { _pendingPrime = PendingPrime.Resume; return; }
        OnResumeAbsorbDeltaOnce_Internal();
    }

    private void OnResumeAbsorbDeltaOnce_Internal()
    {
        if (_resumeAppliedThisSession) return;

        int total  = GetCurrentTotalFallbackSafe();
        string day = Today();
        string sig = BuildPrimeSignature("resume", day);

        if (IsSamePrime(sig)) {
            rawStepCount     = total;
            lastTotal        = total;
            sessionLastTotal = total;
            Persist();
            AvailableStepsChanged?.Invoke(availableSteps);
            return;
        }

        int baseline      = PlayerPrefs.GetInt(KEY_BASELINE, total);
        int earnedNow     = Mathf.Max(0, total - baseline);
        int earnedPrev    = PlayerPrefs.GetInt(KEY_EARNED_SNAPSHOT, earnedNow);
        int offlineEarned = Mathf.Max(0, earnedNow - earnedPrev);

        if (offlineEarned > 0) {
            availableSteps += offlineEarned;
            AvailableStepsChanged?.Invoke(availableSteps);
        }

        rawStepCount     = total;
        lastTotal        = total;
        sessionLastTotal = total;

        UpdateEarnedSnapshot(total);
        SavePrime(sig);
        Persist();

        _resumeAppliedThisSession = true;

        // ★ 폴링 1프레임 차단 + 안전 클램프
        _suppressNextPoll = true;
        ClampAvailableToEarned();
    }
    // ===== 공용 누적 처리(폴링/Resume 공용) =====
    private void PollAndAccumulateOnce()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (!iosInitialized) return;
#endif
        int total = GetCurrentTotalFallbackSafe();
        if (total < 0) return;

        // 재부팅/리셋 감지
        if (total < lastTotal) {
            Debug.LogWarning("[StepManager] Counter reset detected (reboot?). Re-baselining.");
            Rebaseline(total);
            sessionLastTotal = total;
            AvailableStepsChanged?.Invoke(availableSteps);
            return;
        }

        int delta = total - lastTotal;
        if (delta > 0) {
            rawStepCount   = total;
            lastTotal      = total;
            availableSteps += delta;
            UpdateEarnedSnapshot(total);
            Persist();
            AvailableStepsChanged?.Invoke(availableSteps);

            ClampAvailableToEarned();
        }

        // 세션 집계
        int sessionDelta = total - sessionLastTotal;
        if (sessionDelta > 0) {
            sessionSteps     += sessionDelta;
            sessionLastTotal  = total;
        }
    }

    // ===== 폴링 타이머 =====
    private float _nextPollAt;
    [SerializeField] private float pollIntervalSec = 0.25f;
    private void Update()
    {
        // ★ Prime 직후 한 프레임 폴링 차단
        if (_suppressNextPoll) { _suppressNextPoll = false; return; }

    #if UNITY_ANDROID && !UNITY_EDITOR
        if (Permission.HasUserAuthorizedPermission(PERMISSION) && !isInitialized)
            InitializePlugin();
    #endif

        // ★ 준비가 되고 나서 대기중이던 Prime이 있으면 수행
        if (_readyForPrime && _pendingPrime != PendingPrime.None) {
            FlushPendingPrimeIfAny();
            // Prime 직후 폴링 1회 차단
            _suppressNextPoll = true;
            return;
        }

        if (Time.unscaledTime >= _nextPollAt) {
            _nextPollAt = Time.unscaledTime + pollIntervalSec;
            PollAndAccumulateOnce();
        }
    }

    // ===== 플랫폼별 초기화/프라임 =====
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

            // baseline 없으면 총계로 세팅(최초 실행 가드)
            if (!PlayerPrefs.HasKey(KEY_BASELINE)) {
                PlayerPrefs.SetInt(KEY_BASELINE, total);
                PlayerPrefs.Save();
                Debug.Log($"[StepManager] First run baseline set to {total}");
            }

            sessionLastTotal = total;
            sessionSteps     = 0;

            rawStepCount = total;
            lastTotal    = PlayerPrefs.GetInt(KEY_LAST_TOTAL, total);

            // available: 저장값 없으면 0(초기 중복 가산 방지)
            availableSteps = PlayerPrefs.HasKey(KEY_AVAILABLE)
                ? PlayerPrefs.GetInt(KEY_AVAILABLE)
                : 0;

            UpdateEarnedSnapshot(total);

            isInitialized = true;
            Debug.Log($"[StepManager] Init → total={total}, baseline={PlayerPrefs.GetInt(KEY_BASELINE)}, available={availableSteps}");
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

    private void PrimeAndroidLastTotal()
    {
        int total = ReadTotalFromPluginSafe();
        if (total < 0) total = rawStepCount;
        rawStepCount     = total;
        lastTotal        = PlayerPrefs.GetInt(KEY_LAST_TOTAL, total);
        sessionLastTotal = lastTotal;
        UpdateEarnedSnapshot(total);
    }
#endif

#if UNITY_IOS && !UNITY_EDITOR
    private void PrimeIOSLastTotal()
    {
        int today = 0;
        try { today = IOSPedometer.iOS_Pedometer_GetTodaySteps(); }
        catch { today = rawStepCount; }

        rawStepCount     = today;
        lastTotal        = PlayerPrefs.GetInt(KEY_LAST_TOTAL, today);
        sessionLastTotal = lastTotal;

        UpdateEarnedSnapshot(today);
        iosInitialized = true;
    }
#endif

    private void FlushPendingPrimeIfAny()
    {
        var p = _pendingPrime;
        _pendingPrime = PendingPrime.None;

        switch (p) {
            case PendingPrime.NewGame:
                OnNewGamePrimeToday_Internal();
                break;
            case PendingPrime.Resume:
                OnResumeAbsorbDeltaOnce_Internal();
                break;
        }
    }

    private void ClampAvailableToEarned()
    {
        int earned = GetTodayEarnedSteps(); // = total - baseline (>=0)
        if (availableSteps > earned) {
            availableSteps = earned;
            Persist();
            AvailableStepsChanged?.Invoke(availableSteps);
        }
    }
}