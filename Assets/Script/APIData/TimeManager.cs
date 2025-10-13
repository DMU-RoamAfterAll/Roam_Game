using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TimeManager : MonoBehaviour {
    public static TimeManager Instance { get; private set; }

    public DateTime currentTime;
    public bool useUtc = false;

    public UnityEvent onNewDay;            // 여기에 리스너를 붙이면 됨
    const string LastDateKey = "TimeManager.lastDate";
    DateTime _lastDate;

    void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
        Instance = this;

        var saved = PlayerPrefs.GetString(LastDateKey, "");
        if (!string.IsNullOrEmpty(saved) &&
            DateTime.TryParse(saved, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed)) {
            _lastDate = parsed.Date; // ← 오타 수정
        } else {
            _lastDate = Now().Date;
        }
    }

    void OnEnable()  { StartCoroutine(Tick()); }
    void OnDisable() { StopAllCoroutines(); }

    DateTime Now() => useUtc ? DateTime.UtcNow : DateTime.Now;

    IEnumerator Tick() {
        while (true) {
            var now = Now();
            var nextMidnight = now.Date.AddDays(1);
            var wait = Mathf.Max(1f, (float)(nextMidnight - now).TotalSeconds);
            yield return new WaitForSecondsRealtime(wait);
            CheckAndFire();
        }
    }

    void OnApplicationFocus(bool hasFocus) { if (hasFocus) CheckAndFire(); }
    void OnApplicationPause(bool paused)   { if (!paused)  CheckAndFire(); }

    void CheckAndFire() {
        var today = Now().Date;
        int daysPassed = (today - _lastDate).Days;
        if (daysPassed <= 0) return;

        for (int i = 0; i < daysPassed; i++)
            onNewDay?.Invoke();

        _lastDate = today;
        PlayerPrefs.SetString(LastDateKey, _lastDate.ToString("O")); // ← 오타 수정
        PlayerPrefs.Save();
    }

    void Update() {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        try {
            var seoul = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
            currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, seoul);
        } catch {
            currentTime = DateTime.UtcNow.AddHours(9);
        }
#else
        currentTime = DateTime.Now;
#endif
    }

    // (선택) 외부에서 즉시 재검사하고 싶을 때 호출
    public void CheckNow() => CheckAndFire();
}