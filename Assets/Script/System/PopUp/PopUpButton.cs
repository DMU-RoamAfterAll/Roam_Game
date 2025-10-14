using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PopUpButton : MonoBehaviour {
    public PopUpManager manager;
    public PopUpId popUpId;

    void Awake() {
        var btn = GetComponent<Button>();
        btn.onClick.AddListener(() => manager.Show(popUpId));
    }
}