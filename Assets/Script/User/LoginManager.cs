using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using TMPro;
using UnityEngine.SceneManagement;

#region 데이터 클래스
[System.Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}

[System.Serializable]
public class LoginResponse
{
    public string accessToken;
    public string refreshToken;
}
#endregion

public class LoginManager : MonoBehaviour
{
    private string baseUrl = "http://44.218.171.57:8080/api/users";

    private string accessToken;
    private string refreshToken;

    [Header("Login")]
    public TMP_InputField idInputField;
    public TMP_InputField pwInputField;
    public Button loginBtn;

    public void LoginBtn() // 로그인 버튼 onClick 이벤트 함수
    {
        StartCoroutine(Login(idInputField.text, pwInputField.text));
    }

    public void GoRegisterSceneBtn()
    {
        SceneManager.LoadScene("RegisterScene");
    }

    #region 로그인 코루틴 함수
    public IEnumerator Login(string username, string password)
    {
        if (string.IsNullOrEmpty(idInputField.text) || string.IsNullOrEmpty(pwInputField.text)) // 아이디나 비밀번호 칸 비워져있을 때
        {
            Debug.Log("빈 칸을 채워주세요.");
            yield break;
        }

        var loginData = new LoginRequest 
        { 
            username = username, 
            password = password 
        };
        string jsonData = JsonUtility.ToJson(loginData);

        UnityWebRequest request = new UnityWebRequest($"{baseUrl}/login", "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            accessToken = response.accessToken;
            refreshToken = response.refreshToken;
            Debug.Log("로그인 성공: 토큰 저장 완료");

            // 로그인 성공 이후 작업
            // SceneManager.LoadScene("MapScene");
        }
        else // 로그인 실패 시 표시할 텍스트
        {
            Debug.Log("로그인 실패: " + request.error);
        }
    }
    #endregion

}
