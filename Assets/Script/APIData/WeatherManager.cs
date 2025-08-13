using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif


public class WeatherManager : MonoBehaviour {
    public TMP_Text weatherText;

    private string baseUrl;
    public string city = "Seoul";

    private float _lastRequestTime = -Mathf.Infinity;
    private const float REQUEST_COOLDOWN = 60f;


    void Start() {
        weatherText = GameObject.FindGameObjectWithTag(Tag.WeatherUI).GetComponent<TMP_Text>();
        baseUrl = $"http://125.176.246.14:8000";

        RefreshWeather();

        #if UNITY_ANDROID
        
        if(!Permission.HasUserAuthorizedPermission(Permission.FineLocation)) Permission.RequestUserPermission(Permission.FineLocation);
        
        #endif
    }

    public void RefreshWeather() {
        if(Time.time < _lastRequestTime + REQUEST_COOLDOWN) {
            weatherText.text = "Loading...";
            return;
        }

        _lastRequestTime = Time.time;
        StartCoroutine(LocationSend());
    }

    IEnumerator LocationSend() {
        #if UNITY_EDITOR || UNITY_STANDALONE_OSX

        city = "Seoul";
        yield return StartCoroutine(GetByCity(city));

        #elif UNITY_IOS || UNITY_ANDROID

        if(!Input.location.isEnabledByUser) {
            weatherText.text = "locate system is off";
            yield break;
        }

        Input.location.Start();
        int maxWait = 10;

        while(Input.location.status == LocationServiceStatus.Initializing && maxWait > 0) {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if(Input.location.status == LocationServiceStatus.Failed) {
            weatherText.text = "cant bring locate";
            yield break;
        }
        else {
            float lat = Input.location.lastData.latitude;
            float lon = Input.location.lastData.longitude;
            Debug.Log($"Current GPS = lat={lat}. lon={lon}");
            yield return StartCoroutine(GetByCoords(lat, lon));
        }

        #else

        weatherText.text = "cant support platform";

        #endif
    }

    IEnumerator GetByCity(string city) {
        string url = $"{baseUrl}/api/weatherAPI/?city={UnityWebRequest.EscapeURL(city)}";
        yield return StartCoroutine(GetWeather(url));
    }

    IEnumerator GetByCoords(float lat, float lon) {
        string url = $"{baseUrl}/api/weatherAPI/?lat={lat}&lon={lon}";
        yield return StartCoroutine(GetWeather(url));
    }

    IEnumerator GetWeather(string url) {
        using var www = UnityWebRequest.Get(url);
        www.SetRequestHeader("Accept", "application/json");
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError ||
            www.result == UnityWebRequest.Result.ProtocolError) 
        {
            weatherText.text = $"Error: {www.error}";
        }
        else {
            // (1) JSON 전체를 로그로 확인해 보면 확실합니다.
            Debug.Log($"Weather JSON: {www.downloadHandler.text}");

            // (2) 새 스펙으로 파싱
            var resp = JsonUtility.FromJson<WeatherResponse>(www.downloadHandler.text);

            // (3) 화면에 도시·날씨·온도 모두 표시
            weatherText.text =
                $"CITY : {resp.city}\n" +
                $"WEATHER : {resp.description}\n" +
                $"TEMP : {resp.temp:0.0}";
        }
    }

    [System.Serializable]
    private class WeatherResponse {
        public string description;
        public float temp;
        public string city;
    }

    [System.Serializable]
    public class Weather {
        public string description;
    }

    [System.Serializable]
    public class Main {
        public float temp;
    }
}