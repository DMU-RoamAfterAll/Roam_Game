using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeaderInfo : MonoBehaviour {
    public Transform parentObj;

    public TextMeshProUGUI tagName;
    public TextMeshProUGUI titleName;
    public TextMeshProUGUI count;

    void Start() {
        UpdateHeaderInfo();
    }

    void UpdateHeaderInfo() {
        parentObj = this.transform.parent;

        tagName.text = $"[임무]";
        switch(parentObj.name) {
            case "ForestSection" :
                titleName.text = $"검은 숲";
                break;
            
            case "HollowSection" :
                titleName.text = $"폐허";
                break;

            case "Area03" :
                titleName.text = $"[미정]";
                break;

            case "Area04" :
                titleName.text = $"[미정]";
                break;

            case "Area05" :
                titleName.text = $"[미정]";
                break;

            default :
                titleName.text = $"[미정]";
                break;
        }

        count.text = $"({parentObj.childCount - 1}개)";
    }
}