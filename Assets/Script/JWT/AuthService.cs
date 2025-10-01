using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class AuthService
{
    static string UsersBase => $"{GameDataManager.Data.baseUrl}/api/users";

    // 로그인
    public static IEnumerator Login(string username, string password, Action<bool, string> onDone)
    {
        var req = new LoginRequest { username = username, password = password };
        var json = JsonUtility.ToJson(req);

        using var www = new UnityWebRequest($"{UsersBase}/login", "POST");
        www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success) {
            var res = JsonUtility.FromJson<LoginResponse>(www.downloadHandler.text);
            TokenStore.Set(res.accessToken, res.refreshToken);
            onDone?.Invoke(true, null);
        } else {
            onDone?.Invoke(false, www.error);
        }
    }

    // 리프레시
    public static IEnumerator Refresh(Action<bool> onDone)
    {
        if (!TokenStore.HasRefresh) { onDone?.Invoke(false); yield break; }

        var req = new RefreshRequest { refreshToken = TokenStore.RefreshToken };
        var json = JsonUtility.ToJson(req);

        using var www = new UnityWebRequest($"{UsersBase}/refresh", "POST");
        www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success) {
            // 서버가 매번 새 refreshToken을 주면 교체, 아니면 기존 유지
            var res = JsonUtility.FromJson<RefreshResponse>(www.downloadHandler.text);
            var nextAccess = string.IsNullOrEmpty(res.accessToken) ? TokenStore.AccessToken : res.accessToken;
            var nextRefresh = string.IsNullOrEmpty(res.refreshToken) ? TokenStore.RefreshToken : res.refreshToken;
            TokenStore.Set(nextAccess, nextRefresh);
            onDone?.Invoke(true);
        } else {
            onDone?.Invoke(false);
        }
    }

    // 공통: 토큰 붙여 요청 + 401시 1회 리프레시 후 재시도
    public static IEnumerator SendAuthorized(string method, string url, string bodyJson,
                                             Action<string> onSuccess, Action<string> onError)
    {
        // 내부 함수
        IEnumerator DoSend(bool withToken, Action<UnityWebRequest> onComplete)
        {
            var www = new UnityWebRequest(url, method);
            if (!string.IsNullOrEmpty(bodyJson))
                www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyJson));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            if (withToken && TokenStore.HasAccess)
                www.SetRequestHeader("Authorization", $"Bearer {TokenStore.AccessToken}");
            yield return www.SendWebRequest();
            onComplete?.Invoke(www);
            www.Dispose();
        }

        // 1차 시도 (액세스 토큰 포함)
        UnityWebRequest last = null;
        yield return DoSend(true, w => last = w);

        if (last != null && last.result == UnityWebRequest.Result.Success) {
            onSuccess?.Invoke(last.downloadHandler.text);
            yield break;
        }

        // 401 → 리프레시 시도 후 재요청
        if (last != null && last.responseCode == 401)
        {
            bool ok = false;
            yield return Refresh(s => ok = s);
            if (ok) {
                yield return DoSend(true, w => last = w);
                if (last.result == UnityWebRequest.Result.Success) {
                    onSuccess?.Invoke(last.downloadHandler.text);
                    yield break;
                }
            }
        }

        onError?.Invoke(last == null ? "request_failed" : last.error);
    }
}
