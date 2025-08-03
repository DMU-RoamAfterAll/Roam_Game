using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using TMPro;
using UnityEngine.SceneManagement;

#region 데이터 클래스
// 로그인 요청 데이터 클래스
[System.Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}

// 로그인 응답 데이터 클래스 (JWT 토큰 수신용)
[System.Serializable]
public class LoginResponse
{
    public string accessToken;
    public string refreshToken;
}
#endregion

public class LoginManager : MonoBehaviour
{
    private string baseUrl;

    private string accessToken;
    private string refreshToken;

    // UI 입력창 아웃렛 접속
    [Header("Login")]
    public TMP_InputField idInputField;
    public TMP_InputField pwInputField;
    public Button loginBtn;

    void Start() {
        baseUrl = $"{GameDataManager.Data.baseUrl}:8081/api/users";
    }
    // 로그인 버튼 onClick 이벤트 함수
    public void LoginBtn() 
    {
        StartCoroutine(Login(idInputField.text, pwInputField.text));
    }

    // 회원가입 씬으로 이동
    public void GoRegisterSceneBtn()
    {
        SceneManager.LoadScene("RegisterScene");
    }

    #region 로그인 코루틴 함수
    public IEnumerator Login(string username, string password)
    {
        // 아이디나 비밀번호 칸 비워져있는지 확인
        if (string.IsNullOrEmpty(idInputField.text) || 
            string.IsNullOrEmpty(pwInputField.text)) 
        {
            Debug.Log("빈 칸을 채워주세요.");
            yield break;
        }

        // 로그인 데이터 생성
        var loginData = new LoginRequest 
        { 
            username = username, 
            password = password 
        };
        // 데이터 JSON화
        string jsonData = JsonUtility.ToJson(loginData);

        // POST 요청 설정
        UnityWebRequest request = new UnityWebRequest($"{baseUrl}/login", "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // 요청 전송
        yield return request.SendWebRequest();

        // 로그인 성공 시 작업
        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            accessToken = response.accessToken;
            refreshToken = response.refreshToken;
            Debug.Log("로그인 성공: 토큰 저장 완료");

            // 로그인 성공 이후 작업
            // SceneManager.LoadScene("MapScene");
        }
        else // 실패 시
        {
            Debug.Log("로그인 실패: " + request.error);
        }
    }
    #endregion

}
