using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchieveTextInfo : MonoBehaviour {
    public string content;

    public TextMeshProUGUI contentText;

    public GameObject check;
    public GameObject backGround;

    void Start() {
        contentText.text =$"[{content}]";

        check.SetActive(true);
        backGround.SetActive(false);
    }
}