using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class BodyMaskInfo : MonoBehaviour {
    public SectionData sd;

    public TextMeshProUGUI tagName;
    public TextMeshProUGUI titleName;

    public GameObject check;
    public GameObject backGround;

    void Start() {
        tagName.text = $"[{sd.eventType}]";
        titleName.text = sd.content;

        if(sd.isCleared) {
            check.SetActive(true);
            backGround.SetActive(false);
        }
        else {
            check.SetActive(false);
            backGround.SetActive(true);
        }
    }
}