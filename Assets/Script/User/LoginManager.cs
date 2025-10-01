using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using TMPro;
using UnityEngine.SceneManagement;

#region DTOs (이름을 고유하게!)
[System.Serializable]
public class AuthLoginRequest {
    public string username;
    public string password;
}

[System.Serializable]
public class AuthLoginResponse {
    public string accessToken;
    public string refreshToken;
}
#endregion

public class LoginManager : MonoBehaviour
{
    private string baseUrl;

    [Header("Login UI")]
    public TMP_InputField idInputField;
    public TMP_InputField pwInputField;
    public Button loginBtn;

    void Start() {
        baseUrl = $"http://125.176.246.14:8081/api/users";
        Debug.Log($"[Login] baseUrl = {baseUrl}");
        // AuthManager 존재 여부
        Debug.Log($"[Login] AuthManager present? {(AuthManager.Instance != null ? "YES" : "NO")}");


        if (AuthManager.Instance == null)
            Debug.LogWarning("[Login] AuthManager Instance 없음. 씬에 AuthManager를 추가하세요.");
    }

    public void LoginBtn() {
        // 버튼 클릭 시 입력 상태
        Debug.Log($"[Login] Click login. user='{idInputField?.text}', pwLen={(pwInputField?.text?.Length ?? 0)}");
        StartCoroutine(Login(idInputField.text, pwInputField.text));
    }

    public void GoRegisterSceneBtn() {
        SceneManager.LoadScene("RegisterScene");
    }

    private IEnumerator Login(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) {
            Debug.Log("빈 칸을 채워주세요.");
            yield break;
        }

        var loginData = new AuthLoginRequest { username = username, password = password };
        string jsonData = JsonUtility.ToJson(loginData);

        string url = $"{baseUrl}/login";

        // 요청 준비
        Debug.Log($"[Login] POST {url}");
        //Debug.Log($"[Login] payload={jsonData}");

        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        float t0 = Time.realtimeSinceStartup; // 시간 측정

        yield return req.SendWebRequest();
        
        float ms = (Time.realtimeSinceStartup - t0) * 1000f;

        // 결과 요약
        Debug.Log($"[Login] response result={req.result}, code={req.responseCode}, elapsed={ms:F0}ms");

        // 응답 바디(문제 추적용)
        // Debug.Log($"[Login] body={req.downloadHandler.text}");

        if (req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300)
        {
            var resp = JsonUtility.FromJson<AuthLoginResponse>(req.downloadHandler.text);

            // 토큰 길이만 출력
            Debug.Log($"[Login] tokens received: accessLen={(resp?.accessToken?.Length ?? 0)}, refreshLen={(resp?.refreshToken?.Length ?? 0)}");

            if (AuthManager.Instance != null) {
                AuthManager.Instance.SetTokens(resp.accessToken, resp.refreshToken);
                // 토큰 저장 처리 호출됨
                Debug.Log("[Login] AuthManager.SetTokens called");
            } else {
                Debug.LogError("[Login] AuthManager 가 없어 자동 갱신이 비활성입니다. 씬에 AuthManager를 추가하세요.");
            }

            SceneManager.LoadScene("BootScene");
        }
        else
        {
            // 실패 상세
            Debug.LogWarning($"[Login] 실패: error={req.error}, result={req.result}, code={req.responseCode}");
        }
    }
}
