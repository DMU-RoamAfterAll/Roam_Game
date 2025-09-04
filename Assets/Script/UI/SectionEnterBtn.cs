using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class SectionEnterBtn : MonoBehaviour {
    [Header("Task UI")]
    public TaskCompletionSource<bool> tcs;
    public Button yesBtn;
    public Button noBtn;

    public Task<bool> ShowConfirmBtn(string message) {
        gameObject.SetActive(true);
        tcs = new TaskCompletionSource<bool>();

        yesBtn.onClick.AddListener(OnYesClicked);
        noBtn.onClick.AddListener(OnNoClicked);

        return tcs.Task;
    }

    void OnYesClicked() {
        CleanUp();
        tcs.TrySetResult(true);
    }

    void OnNoClicked() {
        CleanUp();
        MapSceneDataManager.Instance.cameraZoom.ZoomOutSection();
        tcs.TrySetResult(false);
    }

    void CleanUp() {
        yesBtn.onClick.RemoveListener(OnYesClicked);
        noBtn.onClick.RemoveListener(OnNoClicked);
        gameObject.SetActive(false);
    }

    public async Task CheckSectionAsync() {
        bool result = await ShowConfirmBtn("이동?");

        if(result) Debug.Log("이동");
        else Debug.Log("이동취소");
    }
}
