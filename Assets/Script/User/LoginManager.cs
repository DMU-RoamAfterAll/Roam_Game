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

    [Header("UI Object")]
    public GameObject enterUI; 
    public GameObject registerUI;
    public GameObject loginUI;

    [Header("Enter UI Buttons")]
    public Button continueButton;   // ← 인스펙터에서 연결
    public Button newGameButton;    // (선택)

    void Start() {
        baseUrl = $"{GameDataManager.Data.baseUrl}:8081/api/users";
        enterUI.SetActive(false);
        loginUI.SetActive(false);
        registerUI.SetActive(true);

        if (continueButton) continueButton.interactable = false; // 기본 비활성
    }

    public void LoginBtn() {
        StartCoroutine(Login(idInputField.text, pwInputField.text));
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

        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300)
        {
            var resp = JsonUtility.FromJson<AuthLoginResponse>(req.downloadHandler.text);
            AuthManager.Instance?.SetTokens(resp.accessToken, resp.refreshToken);

            // 로그인 UI 전환
            enterUI.SetActive(true);
            registerUI.SetActive(false);
            loginUI.SetActive(false);

            // ★ 서버 세이브 확인/생성 → 로컬 저장 → Continue 활성화
            yield return StartCoroutine(FetchOrCreateSaveAndReadyUI(username));
        }
        else
        {
            Debug.LogWarning($"[Login] 실패: error={req.error}, result={req.result}, code={req.responseCode}");
        }
    }

    // -----------------------------------------------------------
    //    세이브 GET → 있으면 로컬에 저장
    //    404면 기본 값으로 POST 생성 → 로컬 저장
    // -----------------------------------------------------------
    private IEnumerator FetchOrCreateSaveAndReadyUI(string username)
    {
        string baseSaveUrl = $"{GameDataManager.Data.baseUrl}:8081/api/save/{UnityWebRequest.EscapeURL(username)}";

        // ===== 1) GET: 세이브 조회 =====
        using (var get = UnityWebRequest.Get(baseSaveUrl))
        {
            AddAuth(get);
            get.downloadHandler = new DownloadHandlerBuffer();
            yield return get.SendWebRequest();

            if (get.result == UnityWebRequest.Result.Success && get.responseCode == 200)
            {
                // 서버 세이브를 로컬에 반영
                var body = get.downloadHandler.text;
                SaveData serverSave = JsonUtility.FromJson<SaveData>(body);
                SaveLoadManager.Instance.OverwriteLocal(serverSave);

                if (continueButton) continueButton.interactable = true;
                yield break;
            }

            // 404면 새로 만든다
            if (get.responseCode != 404)
            {
                Debug.LogWarning($"[Save] GET 실패 code={get.responseCode}, err={get.error}");
                yield break;
            }
        }

        // ===== 2) POST: 세이브 최초 생성 =====
        var newSave = new SaveData {
            playerName = username,
            originSeed = GameDataManager.Data.seed,
            playerPos = Vector3.zero,
            currentSectionId = null,
            preSectionId = null,
            tutorialClear = false,
            visitedSectionIds = new System.Collections.Generic.List<string>()
        };

        using (var post = new UnityWebRequest(baseSaveUrl, "POST"))
        {
            AddAuth(post);
            string payload = JsonUtility.ToJson(newSave);
            post.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            post.downloadHandler = new DownloadHandlerBuffer();
            post.SetRequestHeader("Content-Type", "application/json");

            yield return post.SendWebRequest();

            if (post.result == UnityWebRequest.Result.Success && post.responseCode >= 200 && post.responseCode < 300)
            {
                SaveLoadManager.Instance.OverwriteLocal(newSave);
                if (continueButton) continueButton.interactable = true;
            }
            else
            {
                // 이미 있으면(409) → 다시 GET 시도해도 됨
                Debug.LogWarning($"[Save] POST 실패 code={post.responseCode}, err={post.error}");
            }
        }
    }

    private static void AddAuth(UnityWebRequest req)
    {
        var token = AuthManager.Instance != null ? AuthManager.Instance.AccessToken : null;
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", $"Bearer {token}");
    }
}