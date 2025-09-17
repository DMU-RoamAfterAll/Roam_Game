using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuButtons : MonoBehaviour {
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;

    private string mapSceneName => SceneList.Map;

    void Start() {
        bool hasSave = SaveLoadManager.Instance != null && SaveLoadManager.Instance.HasSave();
        if (continueButton) continueButton.interactable = hasSave;
    }

    public void OnClickNewGame() {
        SaveLoadManager.Instance.NewGameClear(true);

        SwitchSceneManager.Instance.EnterBaseFromBoot();
    }

    public void OnClickContinue() {
        StartCoroutine(CoContinueFlow());
    }

    private IEnumerator CoContinueFlow() {
        if(SaveLoadManager.Instance == null ||
            !SaveLoadManager.Instance.TryLoad(out var data)) {
                OnClickNewGame();
                yield break;
            }

            SaveLoadManager.Instance.pendingLoadData = data;
            SwitchSceneManager.Instance.EnterBaseFromBoot();
            yield break;
    }
}