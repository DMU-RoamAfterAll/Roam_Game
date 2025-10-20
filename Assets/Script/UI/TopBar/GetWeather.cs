using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GetWeather : MonoBehaviour {
    public TextMeshProUGUI weatherText;

    void Awake() {
        if(!weatherText) weatherText = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable() {
        var wm = WeatherManager.Instance;
        if(wm != null) {
            wm.WeatherChanged += OnWeatherChanged;
            OnWeatherChanged(wm.weatherCur);
        }
    }

    void OnDisable() {
        var wm = WeatherManager.Instance;
        if(wm != null) wm.WeatherChanged -= OnWeatherChanged;
    }

    void OnWeatherChanged(string main) {
        weatherText.text = ToKorean(main);
    }

    string ToKorean(string main) {
        switch (main) {
            case "Thunderstorm": return "천둥";
            case "Rain":         return "비";
            case "Snow":         return "눈";
            case "Mist":         return "안개";
            case "Clear":        return "맑음";
            case "Clouds":       return "흐림";
            default:             return "???";
        }
    }
}