using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExpandablePanel : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public GameObject content;
    public Button toggleButton;
    // public TextMeshProUGUI toggleButtonText; // ← 없어도 되는 경우

    private bool isExpanded = true;

    void Start()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(TogglePanel);
    }

    public void TogglePanel()
    {
        isExpanded = !isExpanded;

        if (content != null)
            content.SetActive(isExpanded);

        //if (toggleButtonText != null)
          //  toggleButtonText.text = isExpanded ? "-" : "+";
    }
}
