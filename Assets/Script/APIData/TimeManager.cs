using System;
using UnityEngine;
using TMPro;
using System.Globalization;

public class TimeManager : MonoBehaviour {
    public static TimeManager Instance { get; private set; } //씬에서 모두 접근 가능하도록 Instance화

    public DateTime currentTime;

    void Awake() {
        if (Instance != null && Instance != this)  {
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(this.gameObject);

        Instance = this;
    }

    void Update() {
        #if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR

        try {
            TimeZoneInfo seoulZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");
            currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, seoulZone);
        } catch (TimeZoneNotFoundException) {
            Debug.LogWarning("TimeZone not found, fallback to UTC+9");
            currentTime = DateTime.UtcNow.AddHours(9);
        } catch (Exception ex) {
            Debug.LogWarning($"TimeZone conversion error : {ex.Message}");
            currentTime = DateTime.UtcNow;
        }

        #else

        currentTime = DateTime.Now;

        #endif
        CultureInfo enUS = new CultureInfo("en-US");
    }
}