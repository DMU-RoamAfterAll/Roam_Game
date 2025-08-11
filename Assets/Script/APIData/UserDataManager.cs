using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Resources;

public class UserDataManager : MonoBehaviour
{
    protected string apiUrl = "http://125.176.246.14:8081"; //api 주소
    public string username = "admin"; //테스트용 유저 이름
    public string accessToken = ""; //로그인 토큰

    /// <summary>
    /// api를 통해 서버에 아이템을 추가하는 함수
    /// </summary>
    /// <param name="itemCode">아이템 코드</param>
    /// <param name="itemAmount">아이템 갯수</param>
    /// <returns></returns>
    public IEnumerator GetItem(string itemCode, int itemAmount)
    {
        string url =
        $"{apiUrl}/api/inventory/items" +
        $"?username={UnityWebRequest.EscapeURL(username)}" +
        $"&itemCode={UnityWebRequest.EscapeURL(itemCode)}" +
        $"&amount={itemAmount}";

        using (var www = UnityWebRequest.PostWwwForm(url, "")) //body부분을 비우고 post
        {
            www.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            www.downloadHandler = new DownloadHandlerBuffer();
            www.timeout = 10;

            yield return www.SendWebRequest();

            Debug.Log($"[{GetType().Name}] code={www.responseCode}, result={www.result}");
            if (www.result == UnityWebRequest.Result.Success)
                Debug.Log($"[{GetType().Name}] OK. body='{www.downloadHandler.text}'");
            else
                Debug.LogError($"[{GetType().Name}] Failed: {www.responseCode} / {www.error}");
        }
    }

    /// <summary>
    /// api를 통해 서버에 아이템을 삭제하는 함수
    /// </summary>
    /// <param name="itemCode">아이템 코드</param>
    /// <param name="itemAmount">아이템 갯수</param>
    /// <returns></returns>
    public IEnumerator LostItem(string itemCode, int itemAmount = 1)
    {
        string url =
            $"{apiUrl}/api/inventory/items" +
            $"?username={UnityWebRequest.EscapeURL(username)}" +
            $"&itemCode={UnityWebRequest.EscapeURL(itemCode)}";

        using (var www = UnityWebRequest.Delete(url))
        {
            www.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            www.downloadHandler = new DownloadHandlerBuffer();
            www.timeout = 10;

            yield return www.SendWebRequest();

            Debug.Log($"[{GetType().Name}] code={www.responseCode}, result={www.result}");
            if (www.result == UnityWebRequest.Result.Success)
                Debug.Log($"[{GetType().Name}] OK. body='{www.downloadHandler.text}'");
            else
                Debug.LogError($"[{GetType().Name}] Failed: {www.responseCode} / {www.error}");
        }
    }
}
