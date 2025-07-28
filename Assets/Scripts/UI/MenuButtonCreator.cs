using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MenuButtonCreator : MonoBehaviour
{
    public GameObject buttonPrefab;        // button prefab
    public RectTransform buttonParent;     // parent
    public TextAsset jsonFile;             // JSON file
    public Text outputText;                // print text UI

    void Start()
    {
        if (jsonFile == null)
        {
            Debug.LogError("JSON file is null!");
            return;
        }

        RootNode data = JsonUtility.FromJson<RootNode>(jsonFile.text);
        if (data == null || data.Menu1 == null || data.Menu1.value == null)
        {
            Debug.LogError("Can't find Menu1 data!");
            return;
        }

        foreach (MenuItem item in data.Menu1.value)
        {
            GameObject newButton = Instantiate(buttonPrefab, buttonParent);
            newButton.name = item.id;

            Text btnText = newButton.GetComponentInChildren<Text>();
            if (btnText != null)
                btnText.text = item.label;

            Button btn = newButton.GetComponent<Button>();
            if (btn != null)
            {
                // capture variable
                MenuItem capturedItem = item;

                btn.onClick.AddListener(() =>
                {
                    // Printing choice text
                    outputText.text = string.Join("\n", capturedItem.text);

                    // EX: action processing
                    if (capturedItem.action != null)
                    {
                        foreach (var act in capturedItem.action)
                        {
                            if (act.Get != null && act.Get.Length == 2)
                            {
                                string itemName = act.Get[0];
                                int count;
                                int.TryParse(act.Get[1], out count);
                                Debug.Log($"Get item: {itemName} x{count}");
                            }
                        }
                    }
                });
            }
        }
    }
}
