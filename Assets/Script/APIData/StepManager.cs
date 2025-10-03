using UnityEngine;
using System;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;       // 권한 요청
#endif

/// <summary>
/// 안드로이드 STEP_COUNTER 누적값을 읽어 "오늘 걸음수 잔액(availableSteps)"로 관리.
/// - 앱이 꺼져 있어도 다음 실행 때 누적값으로 오늘 걸음 환산 가능
/// - 자정(날짜 변경) 시 기준선 갱신
/// - 기기 재부팅 등으로 카운터가 리셋되면 자동 재기준선
/// - 소비 내역(availableSteps) PlayerPrefs로 영속 저장
/// </summary>
public class StepManager : MonoBehaviour
{
    public static StepManager Instance { get; private set; }  // 싱글톤

#if UNITY_ANDROID && !UNITY_EDITOR
    private const string PERMISSION = "android.permission.ACTIVITY_RECOGNITION";
    private AndroidJavaObject stepPlugin;
    private bool isInitialized = false;
#endif

    // === 퍼시스턴스 키 ===
    private const string KEY_BASELINE       = "step.baseline";       // 자정 기준선(센서 누적)
    private const string KEY_BASELINE_DATE  = "step.baselineDate";   // 기준선 날짜(yyyyMMdd)
    private const string KEY_AVAILABLE      = "step.available";      // 오늘 잔액(소비 반영)
    private const string KEY_LAST_TOTAL     = "step.lastTotal";      // 마지막으로 읽은 누적값

    // === 공개 상태 ===
    public int rawStepCount;    // 현재 센서 누적값(디버그 용도)
    public int availableSteps;  // 오늘 사용 가능한 걸음 잔액(보상/이동 소비는 여기서 차감)

    // === 내부 상태 ===
    private int lastTotal;      // 직전 프레임의 누적값(재부팅/리셋 감지용)

    // ===== Singleton =====
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // 권한이 없으면 요청
        if (!Permission.HasUserAuthorizedPermission(PERMISSION))
            Permission.RequestUserPermission(PERMISSION);
#else
        // 에디터/기타 플랫폼: 저장값 복원(초기 테스트를 쉽게 하려면 기본 9999도 허용)
        availableSteps = PlayerPrefs.GetInt(KEY_AVAILABLE, 9999);
        rawStepCount   = PlayerPrefs.GetInt(KEY_LAST_TOTAL, 9999);
        lastTotal      = rawStepCount;

        // 에디터에서도 날짜 바뀌면 baseline 갱신(현재 total을 사용)
        ResetDailyBaselineIfNeeded(rawStepCount);
#endif
    }

    private void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // 권한 승인 이후 1회 초기화
        if (Permission.HasUserAuthorizedPermission(PERMISSION) && !isInitialized)
            InitializePlugin();

        if (isInitialized && stepPlugin != null)
        {
            // 1) 현재 누적값 읽기(재부팅 이후 누적)
            int total = 0;
            try { total = stepPlugin.Call<int>("getStepCount"); }
            catch (Exception ex)
            {
                Debug.LogError($"[StepManager] getStepCount error: {ex}");
                return;
            }

            // 2) 재부팅/리셋 감지: 누적이 줄었으면 기준선/lastTotal 재설정
            if (total < lastTotal)
            {
                Debug.LogWarning("[StepManager] Counter reset detected (reboot?). Re-baselining to current total.");
                Rebaseline(total); // baseline = total, availableSteps는 유지(원하면 0으로 초기화해도 됨)
            }

            // 3) 날짜 변경(자정 경과) 시 기준선 갱신
            ResetDailyBaselineIfNeeded(total);

            // 4) 증가분만큼 오늘 잔액 증가
            int delta = total - lastTotal;
            if (delta > 0)
            {
                availableSteps += delta;
                rawStepCount    = total;
                lastTotal       = total;
                Persist();
            }
        }
#endif
    }

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

            int total = stepPlugin.Call<int>("getStepCount"); // 현재 누적
            rawStepCount = total;

            // 직전 total(없으면 현 total)
            lastTotal = PlayerPrefs.GetInt(KEY_LAST_TOTAL, total);

            // 날짜 확인해 baseline 세팅
            ResetDailyBaselineIfNeeded(total);

            // availableSteps 복원(없으면 total - baseline으로 환산)
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
#endif

    // ===== 퍼블릭 API =====
    /// <summary>
    /// cost만큼 오늘 잔액을 사용(가능하면 true)
    /// </summary>
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

    /// <summary>
    /// Java 플러그인이 센서를 못 찾을 때 호출(선택)
    /// </summary>
    public void OnStepSensorUnavailable()
    {
        Debug.LogWarning("[StepManager] 걸음 센서가 감지되지 않았습니다.");
    }

    // ===== 내부 유틸 =====
    private static string Today() => DateTime.Now.ToString("yyyyMMdd");

    /// <summary>
    /// 날짜가 바뀌었으면 baseline을 현재 누적값으로 갱신하고 오늘 잔액 0으로 리셋.
    /// </summary>
    private void ResetDailyBaselineIfNeeded(int currentTotal)
    {
        string today = Today();
        string savedDay = PlayerPrefs.GetString(KEY_BASELINE_DATE, today);

        if (savedDay != today)
        {
            PlayerPrefs.SetString(KEY_BASELINE_DATE, today);
            PlayerPrefs.SetInt(KEY_BASELINE, currentTotal);

            availableSteps = 0;
            lastTotal      = currentTotal;
            rawStepCount   = currentTotal;

            Persist();
            Debug.Log($"[StepManager] New day → baseline={currentTotal}, available reset to 0");
        }
    }

    /// <summary>
    /// 센서 누적이 갑자기 줄었을 때(재부팅) 기준선과 lastTotal을 현재값으로 재설정.
    /// </summary>
    private void Rebaseline(int currentTotal)
    {
        PlayerPrefs.SetInt(KEY_BASELINE, currentTotal);
        lastTotal    = currentTotal;
        rawStepCount = currentTotal;
        Persist();
    }

    /// <summary>
    /// 현재 상태를 PlayerPrefs에 반영.
    /// </summary>
    private void Persist()
    {
        PlayerPrefs.SetInt(KEY_AVAILABLE,  availableSteps);
        PlayerPrefs.SetInt(KEY_LAST_TOTAL, lastTotal);
        PlayerPrefs.Save();
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
        PlayerPrefs.SetInt(KEY_BASELINE, current);
        PlayerPrefs.SetString(KEY_BASELINE_DATE, Today());
        availableSteps = 0;
        lastTotal = current;
        Persist();
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