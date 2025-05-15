using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine.UI;

[System.Serializable]
public class DialogueNode {
    public string text;
    public List<Choice> choice;
    public string next;
}

[System.Serializable]
public class DialogueData {
    public string rate;
    public string eventType;
    public Dictionary<string, DialogueNode> dialogues;
}

[System.Serializable]
public class Choice {
    public string text;
    public string next;
}

public class ReadJsonDialogue : MonoBehaviour {
    public TextAsset dialogueFile;
    public Text dialogueText;
    public GameObject choiceButtonPrefab;
    public Transform choiceButtonContainer;

    private Dictionary<string, DialogueNode> dialogues;
    private DialogueNode currentNode;

    void Start() {
        LoadDialogue();
        StartDialogue("start");
    }

    void LoadDialogue() {
        if (dialogueFile == null) {
            Debug.LogError("Dialogue file is not assigned");
            return;
        }

        DialogueData data = JsonConvert.DeserializeObject<DialogueData>(dialogueFile.text);
        dialogues = data.dialogues;
        Debug.Log($"Loaded dialogue with rate: {data.rate}, eventType: {data.eventType}");
    }

    void StartDialogue(string nodeKey) {
        if(dialogues.ContainsKey(nodeKey)) {
            currentNode = dialogues[nodeKey];
            DisplayNode(currentNode);
        }
        else {
            Debug.LogError($"Node '{nodeKey}' not found in dialogue data");
        }
    }

    void DisplayNode(DialogueNode node) {
        dialogueText.text = node.text;

        // 기존 버튼 삭제
        foreach (Transform child in choiceButtonContainer) {
            Destroy(child.gameObject);
        }

        // 선택지가 없는 경우 "다음으로" 버튼 생성
        if (node.choice == null || node.choice.Count == 0) {
            if (!string.IsNullOrEmpty(node.next)) {
                GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                Button button = buttonObj.GetComponent<Button>();
                Text buttonText = buttonObj.GetComponentInChildren<Text>();

                buttonText.text = "다음으로";

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    Debug.Log($"Moving to next node: {node.next}");
                    StartDialogue(node.next);
                });
            }
        } else {
            // 선택지 버튼 생성
            foreach (Choice choice in node.choice) {
                GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                Button button = buttonObj.GetComponent<Button>();
                Text buttonText = buttonObj.GetComponentInChildren<Text>();

                buttonText.text = choice.text;

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    Debug.Log($"Button clicked: {choice.next}");
                    StartDialogue(choice.next);
                });
            }
        }
    }
}