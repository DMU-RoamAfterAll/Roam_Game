using UnityEngine;

public static class TokenStore {
    private const string KEY_ACCESS = "auth.accessToken";
    private const string KEY_REFRESH = "auth.refreshToken";

    public static string AccessToken { get; private set; }
    public static string RefreshToken { get; private set; }

    public static void Set(string access, string refresh) {
        AccessToken = access;
        RefreshToken = refresh;
        PlayerPrefs.SetString(KEY_ACCESS, access ?? "");
        PlayerPrefs.SetString(KEY_REFRESH, refresh ?? "");
        PlayerPrefs.Save();
    }

    public static void Load() {
        AccessToken = PlayerPrefs.GetString(KEY_ACCESS, "");
        RefreshToken = PlayerPrefs.GetString(KEY_REFRESH, "");
    }

    public static void Clear() {
        AccessToken = null;
        RefreshToken = null;
        PlayerPrefs.DeleteKey(KEY_ACCESS);
        PlayerPrefs.DeleteKey(KEY_REFRESH);
        PlayerPrefs.Save();
    }

    public static bool HasRefresh => !string.IsNullOrEmpty(RefreshToken);
    public static bool HasAccess => !string.IsNullOrEmpty(AccessToken);
}
