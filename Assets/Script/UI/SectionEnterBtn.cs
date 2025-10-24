using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SectionEnterBtn : MonoBehaviour {
    [Header("Task UI")]
    public TaskCompletionSource<bool> tcs;
    public Button yesBtn;
    public Button noBtn;
    public TextMeshProUGUI costText;

    public Task<bool> ShowConfirmBtn(string message, int cost = 0) {
        gameObject.SetActive(true);
        tcs = new TaskCompletionSource<bool>();

        yesBtn.onClick.AddListener(OnYesClicked);
        noBtn.onClick.AddListener(OnNoClicked);

        if(cost != 0) costText.text = $"이동하기 위해 {cost}보가 필요합니다.";
        else costText.text = $"이동하시겠습니까?";
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
