using System;
using UnityEngine;
using TMPro;
using System.Globalization;

public class TimeManager : MonoBehaviour {
    public TMP_Text timeText;

    void Start() {
        timeText = this.GetComponent<TMP_Text>();
    }

    void Update() {
        DateTime currentTime;

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
        timeText.text = currentTime.ToString("dddd, MMMM d, yyyy - hh:mm:ss tt", enUS);
    }
}