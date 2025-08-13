using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MoveScene : MonoBehaviour {

    public void MoveToAPIScene() { SceneManager.LoadScene("APIScene"); }

    public void MoveToLoginScene() { SceneManager.LoadScene("LoginScene"); }

    public void MoveToMapScene() { SceneManager.LoadScene("MapScene"); }

    public void MoveToRegisterScene() { SceneManager.LoadScene("RegisterScene"); }

    public void MoveToStroyScene() { SceneManager.LoadScene("StoryScene"); }
}
