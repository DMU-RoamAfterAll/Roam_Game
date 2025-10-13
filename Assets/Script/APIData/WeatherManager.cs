using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif


public class WeatherManager : MonoBehaviour {
    public static WeatherManager Instance { get; private set; } //씬에서 모두 접근 가능하도록 Instance화

    private string baseUrl;
    public string city = "Seoul";
    public bool isHidden;

    private const float REQUEST_COOLDOWN = 300f;
    private const float AUTO_REFRESH_INTERVAL = 305f;
    private float _lastRequestTime = -Mathf.Infinity;

    public WeatherResponse resp;

    private string _lastServerTime;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(this.gameObject);

        Instance = this;
    }

    void Start()
    {
        baseUrl = $"{GameDataManager.Data.baseUrl}:8000";
        isHidden = false;

        RefreshWeather();
        StartCoroutine(AutoRefreshLoop());

#if UNITY_ANDROID

        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation)) Permission.RequestUserPermission(Permission.FineLocation);
        
#endif
    }

    IEnumerator AutoRefreshLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(AUTO_REFRESH_INTERVAL);
            RefreshWeather();
        }
    }

    public void RefreshWeather()
    {
        if (Time.time < _lastRequestTime + REQUEST_COOLDOWN)
        {
            return;
        }

        _lastRequestTime = Time.time;
        StartCoroutine(LocationSend());
    }

    IEnumerator LocationSend()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_OSX

        city = "Seoul";
        yield return StartCoroutine(GetByCity(city));

#elif UNITY_IOS || UNITY_ANDROID

        if(!Input.location.isEnabledByUser) {
            yield break;
        }

        Input.location.Start();
        int maxWait = 10;

        while(Input.location.status == LocationServiceStatus.Initializing && maxWait > 0) {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if(Input.location.status == LocationServiceStatus.Failed) {
            yield break;
        }
        else {
            float lat = Input.location.lastData.latitude;
            float lon = Input.location.lastData.longitude;
            Debug.Log($"Current GPS = lat={lat}. lon={lon}");
            yield return StartCoroutine(GetByCoords(lat, lon));
        }

#else

        Debug.Log("Not Support Platform")

#endif
    }

    IEnumerator GetByCity(string city)
    {
        string url = $"{baseUrl}/api/weatherAPI/?city={UnityWebRequest.EscapeURL(city)}";
        yield return StartCoroutine(GetWeather(url));
    }

    IEnumerator GetByCoords(float lat, float lon)
    {
        string url = $"{baseUrl}/api/weatherAPI/?lat={lat}&lon={lon}";
        yield return StartCoroutine(GetWeather(url));
    }

    IEnumerator GetWeather(string url)
    {
        using var www = UnityWebRequest.Get(url);
        www.SetRequestHeader("Accept", "application/json");
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError ||
            www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log("Error");
        }
        else
        {
            // (1) JSON 전체를 로그로 확인해 보면 확실합니다.
            Debug.Log($"Weather JSON: {www.downloadHandler.text}");

            // (2) 새 스펙으로 파싱
            resp = JsonUtility.FromJson<WeatherResponse>(www.downloadHandler.text);
        }
    }

    [System.Serializable]
    public class WeatherResponse
    {
        public string main;
        public string description;
        public float temp;
        public string city;
    }

    [System.Serializable]
    public class Weather
    {
        public string description;
    }

    [System.Serializable]
    public class Main
    {
        public float temp;
    }

    public void HiddenEvent() {
        if(!GameDataManager.Data.tutorialClear && isHidden) return;

        isHidden = true;
        //false조건도 달아야 함!!!!!!

        var wm = WeatherManager.Instance;
        var resp = wm != null ? wm.resp : null;
        var main = resp != null ? resp.main : null;

        float percent = 0.3f;
        if(!SecureRng.Chance(percent)) return;

        switch (main) {
            case "Thundersorm" :
                Debug.Log("폭풍이다");
                break;

            case "Drizzle" :
                Debug.Log("가랑비다");
                break;

            case "Rain" :
                Debug.Log("비온다");
                break;

            case "Snow" :
                Debug.Log("눈온다");
                break;
            
            case "Mist":
            case "Smoke":
            case "Haze":
            case "Dust":
            case "Fog":
            case "Sand":
            case "Ash":
            case "Squall":
            case "Tornado":
                Debug.Log("습하거나탁하다");
                break;

            case "Clear" :
                Debug.Log("맑다");
                break;

            case "Clouds" :
                Debug.Log("흐릿하다");
                break;

            default :
                Debug.Log("알수없는 날씨");
                break;
        }
    }

    void OnEnable() {
        SubscribeToTimeManager();
    }

    void SubscribeToTimeManager() {
        if (TimeManager.Instance != null) TimeManager.Instance.onNewDay.AddListener(OnNewDay);
        else StartCoroutine(SubscribeNextFrame());
    }

    System.Collections.IEnumerator SubscribeNextFrame() {
        yield return null;
        if (TimeManager.Instance != null) TimeManager.Instance.onNewDay.AddListener(OnNewDay);
    }

    void OnNewDay() {
        isHidden = false;
    }
}