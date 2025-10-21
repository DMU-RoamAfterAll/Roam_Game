using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToTitleManager : MonoBehaviour {
    public void BackToTitle() {
        SaveLoadManager.Instance?.SaveNow();
        SaveLoadManager.Instance?.SaveNowAndUpload(GameDataManager.Data.playerName);
        SceneManager.LoadScene(SceneList.Boot);        
    }
}