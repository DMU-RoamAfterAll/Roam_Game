using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToTitleManager : MonoBehaviour {
    public void BackToTitle() {
        SaveLoadManager.Instance?.SaveNow();
        SceneManager.LoadScene(SceneList.Boot);        
    }
}