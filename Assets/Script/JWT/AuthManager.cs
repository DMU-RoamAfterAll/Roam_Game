using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    // 저장 키
    protected string KEY_ACCESS = "auth_access_token";
    protected string KEY_REFRESH = "auth_refresh_token";
    protected string KEY_USERNAME = "auth_username";

    protected string username = "";

    // 서버 URL 루트 (GameDataManager에서 가져옴)
    private string baseUrl; // ex) http://125.176.246.14:8081

    // 현재 토큰들
    public string AccessToken { get; private set; }
    public string RefreshToken { get; private set; }

    private Coroutine refreshLoop;

    [Serializable] private class RefreshRequest { public string refreshToken; }
    [Serializable] private class RefreshResponse { public string accessToken; public string refreshToken; }

    void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() {
        baseUrl = $"{GameDataManager.Data.baseUrl}:8081"; // ⬅️ 3번 참고
        var savedAccess  = PlayerPrefs.GetString(KEY_ACCESS, string.Empty);
        var savedRefresh = PlayerPrefs.GetString(KEY_REFRESH, string.Empty);
        username          = PlayerPrefs.GetString(KEY_USERNAME, string.Empty); // ★ 추가

        if (!string.IsNullOrEmpty(savedRefresh)) {
            AccessToken  = savedAccess;
            RefreshToken = savedRefresh;
            refreshLoop  = StartCoroutine(AutoRefreshLoop());
        }
    }

    /// <summary>로그인 성공 직후 호출해서 토큰을 저장하고 자동 갱신을 시작.</summary>
    public void SetTokens(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;

        PlayerPrefs.SetString(KEY_ACCESS, AccessToken ?? string.Empty);
        PlayerPrefs.SetString(KEY_REFRESH, RefreshToken ?? string.Empty);
        PlayerPrefs.Save();

        if (refreshLoop != null) StopCoroutine(refreshLoop);
        refreshLoop = StartCoroutine(AutoRefreshLoop());
    }

    /// <summary>Authorization 헤더 부착 도우미</summary>
    public void AttachAuth(UnityWebRequest req)
    {
        if (!string.IsNullOrEmpty(AccessToken))
            req.SetRequestHeader("Authorization", $"Bearer {AccessToken}");
    }

    /// <summary>5분 이내 만료 예정인지 확인(가능하면 JWT exp 사용)</summary>
    private bool ShouldRefreshSoon()
    {
        if (string.IsNullOrEmpty(AccessToken)) return true;

        if (TryGetJwtExpiryUtc(AccessToken, out var expUtc))
        {
            // 만료 5분 전부터 선제 갱신
            return DateTime.UtcNow >= expUtc - TimeSpan.FromMinutes(5);
        }

        // exp 파싱 실패 시: 보수적으로 30분마다 리프레시 시도
        return true;
    }

    private IEnumerator AutoRefreshLoop()
    {
        var wait = new WaitForSeconds(60f); // 1분 주기 체크
        while (true)
        {
            // 리프레시 토큰이 없다면 루프만 유지
            if (string.IsNullOrEmpty(RefreshToken))
            {
                yield return wait;
                continue;
            }

            if (ShouldRefreshSoon())
            {
                yield return RefreshAccessToken();
            }

            yield return wait;
        }
    }

    /// <summary>/api/users/refresh 로 리프레시 요청</summary>
    private IEnumerator RefreshAccessToken()
    {
        if (string.IsNullOrEmpty(RefreshToken))
            yield break;

        string url = $"{baseUrl}/api/users/refresh";
        var body = new RefreshRequest { refreshToken = RefreshToken };
        string json = JsonUtility.ToJson(body);

        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success && req.responseCode >= 200 && req.responseCode < 300)
        {
            var resp = JsonUtility.FromJson<RefreshResponse>(req.downloadHandler.text);
            // 서버가 새 refreshToken도 주면 갱신, 아니면 기존 유지
            var newAccess = resp.accessToken;
            var newRefresh = string.IsNullOrEmpty(resp.refreshToken) ? RefreshToken : resp.refreshToken;
            SetTokens(newAccess, newRefresh);
            Debug.Log("[Auth] access 토큰 갱신 성공");
        }
        else
        {
            Debug.LogWarning($"[Auth] 토큰 갱신 실패 code={req.responseCode}, body={req.downloadHandler.text}");
            if (req.responseCode == 401 || req.responseCode == 403)
            {
                // 리프레시도 만료/무효 → 완전 로그아웃 처리
                LogoutLocalOnly();
                // 필요하면 로그인 씬으로 보내기
                // UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
            }
        }
    }

    /// <summary>로컬 저장된 토큰만 제거</summary>
    public void LogoutLocalOnly()
    {
        AccessToken = null;
        RefreshToken = null;
        username = string.Empty; // ★ 추가

        PlayerPrefs.DeleteKey(KEY_ACCESS);
        PlayerPrefs.DeleteKey(KEY_REFRESH);
        PlayerPrefs.DeleteKey(KEY_USERNAME); // ★ 추가
        PlayerPrefs.Save();

        if (refreshLoop != null) { StopCoroutine(refreshLoop); refreshLoop = null; }
    }

    /// <summary>JWT exp 추출 (초 단위 Unix epoch)</summary>
    private static bool TryGetJwtExpiryUtc(string jwt, out DateTime expUtc)
    {
        expUtc = default;
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return false;

            // Base64Url 디코딩
            string payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var m = Regex.Match(json, "\"exp\"\\s*:\\s*(\\d+)");
            if (!m.Success) return false;

            long exp = long.Parse(m.Groups[1].Value);
            expUtc = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
            return true;
        }
        catch { return false; }
    }

    public string GetToken() => AccessToken;

    public string GetUserName() => username;

    public void SetUserName(string name) {
        username = name;                                     // 메모리에 보관
        PlayerPrefs.SetString(KEY_USERNAME, username);       // ★ 영구 저장
        PlayerPrefs.Save();
    }
}
