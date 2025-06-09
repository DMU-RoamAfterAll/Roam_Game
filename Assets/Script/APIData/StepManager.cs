using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

#if UNITY_ANDROID

using UnityEngine.Android;

#endif

public class StepManager : MonoBehaviour {
    public TMP_Text totalStepsText;
    public TMP_Text sessionStepsText;

    #if UNITY_IOS && !UNITY_EDITOR
    
    [DllImport("__Internal")]
    private static extern void StartStepTracking();

    [DllImport("__Internal")]
    private static extern int GetTodayStepCount();

    [DllImport("__Internal")]
    private static extern int GetSessionStepCount();

    #elif UNITY_ANDROID && !UNITY_EDITOR

    private static int androidTodaySteps = 0;
    private static int androidSessionSteps = 0;

    private AndroidJavaObject stepPlugin;
    private int initialSteps = 0;
    private bool initialized = false;

    #endif

    void Start() {
        totalStepsText = GameObject.Find("TotalStepData").GetComponent<TMP_Text>();
        sessionStepsText = GameObject.Find("SessionStepData").GetComponent<TMP_Text>();

        Application.targetFrameRate = 60;
        
        #if UNITY_IOS && !UNITY_EDITOR

        StartStepTracking();

        #elif UNITY_ANDROID && !UNITY_EDITOR

        const string ActivityRecognitionPermission = "android.permission.ACTIVITY_RECOGNITION";
        if (!Permission.HasUserAuthorizedPermission(ActivityRecognitionPermission)) {
            Permission.RequestUserPermission(ActivityRecognitionPermission);
        }

        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            stepPlugin = new AndroidJavaObject("com.ghgh10288.steptracker.StepTracker", activity);
            initialSteps = stepPlugin.Call<int>("getStepCount");
            initialized = true;
            Debug.Log("[StepManager] StepTracker plugin initialized.");
        }

        #endif
    }

    void Update() {
        #if UNITY_IOS && !UNITY_EDITOR
        int today = GetTodayStepCount();
        int session = GetSessionStepCount();

        totalStepsText.text = $"today step Count : {today:N0}";
        sessionStepsText.text = $"session step Count : {session:N0}";

        #elif UNITY_ANDROID && !UNITY_EDITOR
        if (initialized && stepPlugin != null) {
            Debug.Log($"[StepManager] Calling getStepCount and getSessionStepCount...");
            androidTodaySteps = stepPlugin.Call<int>("getStepCount");
            androidSessionSteps = stepPlugin.Call<int>("getSessionStepCount");

            totalStepsText.text = $"today step Count : {androidTodaySteps:N0}";
            //sessionStepsText.text = $"session step Count : {androidSessionSteps:N0}";
            sessionStepsText.text = null;
            Debug.Log($"[StepManager] Android Step Count - Today: {androidTodaySteps}, Session: {androidSessionSteps}");
        }
        #else
        totalStepsText.text = "is Only in iPhone";
        sessionStepsText.text = "";
        #endif
    }

    public void OnStepSensorUnavailable() {
        totalStepsText.text = "걸음 센서 감지되지 않음";
        sessionStepsText.text = "";
        Debug.LogWarning("[StepManager] 걸음 센서가 감지되지 않았습니다.");
    }
}