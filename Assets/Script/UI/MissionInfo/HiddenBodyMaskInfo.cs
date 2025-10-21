using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HiddenBodyMaskInfo : MonoBehaviour {
    public TextMeshProUGUI tagName;
    public TextMeshProUGUI titleName;

    const string hiddenFolderPath = "StoryGameData/SectionData/SectionEvent/HiddenSection/";

    void Start() {
        tagName.text = "날씨";
        titleName.text = WeatherManager.Instance.weatherCur;
    }

    public void EnterHidden() {
        GameDataManager.Instance.sectionPath = hiddenFolderPath + WeatherManager.Instance.hiddenFileName;
        SwitchSceneManager.Instance.MoveScene(SceneList.Story);
    }
}