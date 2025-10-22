using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using TMPro;

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
    public Button continueButton;   // 인스펙터에서 연결
    public Button newGameButton;    // (선택)

    // 로그인 성공 후 사용자명 캐시
    private string _loggedInUsername;
    // 서버에 저장 존재 여부 (버튼 가드에도 사용해 경고 제거)
    private bool _hasRemoteSave;
    public string un;

    void Start() {
        baseUrl = $"{GameDataManager.Data.baseUrl}:8081/api/users";

        setUI();

        if (continueButton) continueButton.interactable = false; // 기본 비활성
        if (newGameButton)  newGameButton.interactable  = false; // 기본 비활성
        _hasRemoteSave = false;
    }

    public void setUI() {
        enterUI.SetActive(false);
        loginUI.SetActive(false);
        registerUI.SetActive(true);
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
            
            // 토큰 저장
            AuthManager.Instance?.SetTokens(resp.accessToken, resp.refreshToken);

            // ✅ 유저네임 저장(메모리 + PlayerPrefs) & 게임 데이터 반영
            AuthManager.Instance?.SetUserName(username);
            _loggedInUsername = username;
            GameDataManager.Data.playerName = username;

            // UI 전환
            enterUI.SetActive(true);
            registerUI.SetActive(false);
            loginUI.SetActive(false);

            // 서버 세이브 유무만 검사해서 버튼 상태만 결정
            yield return StartCoroutine(ProbeRemoteSave(username));
        }
        else
        {
            Debug.LogWarning($"[Login] 실패: error={req.error}, result={req.result}, code={req.responseCode}");
        }
    }

    /// <summary>
    /// 서버에 저장이 있는지 확인만 해서 버튼 상태 결정.
    /// ※ 여기서는 로컬 파일을 덮어쓰지 않고, pendingLoadData도 건드리지 않습니다.
    /// </summary>
    private IEnumerator ProbeRemoteSave(string username)
    {
        string baseSaveUrl = $"{GameDataManager.Data.baseUrl}:8081/api/save/{UnityWebRequest.EscapeURL(username)}";

        using (var get = UnityWebRequest.Get(baseSaveUrl))
        {
            AddAuth(get);
            get.downloadHandler = new DownloadHandlerBuffer();
            yield return get.SendWebRequest();

            un = username;

            if (get.result == UnityWebRequest.Result.Success && get.responseCode == 200)
            {
                // 서버에 저장 있음 → Continue 허용
                _hasRemoteSave = true;
                if (continueButton) continueButton.interactable = true;
                if (newGameButton)  newGameButton.interactable  = true;
                yield break;
            }

            if (get.responseCode == 404)
            {
                // 서버에 저장 없음 → Continue 차단, New Game 허용
                _hasRemoteSave = false;
                if (continueButton) continueButton.interactable = false;
                if (newGameButton)  newGameButton.interactable  = true;
                yield break;
            }

            Debug.LogWarning($"[Save] GET 실패 code={get.responseCode}, err={get.error}");
            _hasRemoteSave = false;
            if (continueButton) continueButton.interactable = false;
            if (newGameButton)  newGameButton.interactable  = true;
        }
    }

    private static void AddAuth(UnityWebRequest req)
    {
        var token = AuthManager.Instance != null ? AuthManager.Instance.AccessToken : null;
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", $"Bearer {token}");
    }

    // Continue 버튼: 항상 서버에서 다시 GET해서 불러옴
    public void OnClickContinue()
    {
        if (!_hasRemoteSave) {
            Debug.LogWarning("[Continue] 서버 저장이 없는 상태입니다. (버튼 가드)");
            if (continueButton) continueButton.interactable = false;
            return;
        }
        StartCoroutine(CoContinueFromServer());
    }

    private IEnumerator CoContinueFromServer()
    {
        string username = !string.IsNullOrEmpty(_loggedInUsername) ? _loggedInUsername : GameDataManager.Data.playerName;
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("[Continue] username이 비어 있습니다.");
            yield break;
        }

        string url = $"{GameDataManager.Data.baseUrl}:8081/api/save/{UnityWebRequest.EscapeURL(username)}";

        using (var get = UnityWebRequest.Get(url))
        {
            AddAuth(get);
            get.downloadHandler = new DownloadHandlerBuffer();
            yield return get.SendWebRequest();

            if (get.result == UnityWebRequest.Result.Success && get.responseCode == 200)
            {
                var body = get.downloadHandler.text;
                var serverSave = JsonUtility.FromJson<SaveData>(body);

                if (serverSave != null)
                {
                    // ★ 서버 저장으로 계속하기 → 서버 시드로 고정
                    GameDataManager.Instance?.ContinueSeed(serverSave.originSeed);

                    if (SaveLoadManager.Instance != null) {
                        SaveLoadManager.Instance.OverwriteLocal(serverSave);   // 파일 저장
                        SaveLoadManager.Instance.pendingLoadData = serverSave; // 맵 조립 후 적용
                    } else {
                        Debug.LogWarning("[Continue] SaveLoadManager.Instance 없음 (Boot 씬에 배치 확인)");
                    }

                    // 맵 진입
                    SwitchSceneManager.Instance?.EnterBaseFromBoot();
                }
                else
                {
                    Debug.LogWarning("[Continue] 서버 응답을 SaveData로 파싱하지 못했습니다.");
                    if (continueButton) continueButton.interactable = false;
                }
                yield break;
            }

            if (get.responseCode == 404)
            {
                Debug.LogWarning("[Continue] 서버 저장 없음(404)");
                if (continueButton) continueButton.interactable = false;
                if (newGameButton)  newGameButton.interactable  = true;
                _hasRemoteSave = false;
                yield break;
            }

            Debug.LogWarning($"[Continue] 원격 세이브 실패 code={get.responseCode}, err={get.error}");
            if (continueButton) continueButton.interactable = false;
            if (newGameButton)  newGameButton.interactable  = true;
            _hasRemoteSave = false;
        }
    }

    // New Game 버튼: 로컬/서버 초기화 후 진입
    public void OnClickNewGame()
    {
        if (SaveLoadManager.Instance != null) {
            SaveLoadManager.Instance.NewGameClear();          // 로컬 리셋(여기서 새 시드 생성은 SaveLoadManager에서 수행하도록 설계했으면 거기서 처리)
            SaveLoadManager.Instance.pendingLoadData = null;  // 과거 세이브 적용 방지
            SaveLoadManager.Instance.SaveNow();
        } else {
            Debug.LogWarning("[NewGame] SaveLoadManager.Instance 가 없습니다 (Boot 씬에 배치 확인)");
        }

        var username = !string.IsNullOrEmpty(_loggedInUsername) ? _loggedInUsername : GameDataManager.Data.playerName;
        if (SaveLoadManager.Instance != null)
            SaveLoadManager.Instance.SaveNowAndUpload(username, this); // 서버에 초기 스냅샷 PUT

        SwitchSceneManager.Instance?.EnterBaseFromBoot();
    }
}