using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class UserDataManager : MonoBehaviour
{
    protected string apiUrl = "http://125.176.246.14:8081"; //api 주소
    public string username = "admin"; //테스트용 유저 이름
    public string accessToken = ""; //로그인 토큰

    /// <summary>
    /// api 송신을 도와주는 헬퍼 함수
    /// </summary>
    /// <param name="req">전송할 리퀘스트</param>
    /// <returns></returns>
    private IEnumerator SendApi(UnityWebRequest req)
    {
        req.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 10;

        yield return req.SendWebRequest();

        Debug.Log($"[{GetType().Name}] code={req.responseCode}, result={req.result}");
        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log($"[{GetType().Name}] OK body='{req.downloadHandler.text}'");
        else
            Debug.LogError($"[{GetType().Name}] FAIL {req.responseCode} / {req.error}");
    }

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

        using (var req = UnityWebRequest.PostWwwForm(url, "")) //body부분 비우고 전송
            yield return SendApi(req);
    }

    /// <summary>
    /// api를 통해 서버에 아이템을 삭제하는 함수
    /// </summary>
    /// <param name="itemCode">아이템 코드</param>
    /// <param name="itemAmount">아이템 갯수</param>
    /// <returns></returns>
    public IEnumerator LostItem(string itemCode, int itemAmount)
    {
        string url =
            $"{apiUrl}/api/inventory/items" +
            $"?username={UnityWebRequest.EscapeURL(username)}" +
            $"&itemCode={UnityWebRequest.EscapeURL(itemCode)}";

        using (var req = UnityWebRequest.Delete(url))
            yield return SendApi(req);
    }

    /// <summary>
    /// api를 통해 서버의 플래그를 설정하는 함수
    /// </summary>
    /// <param name="flagCode">플래그 코드</param>
    /// <param name="flagState">플래그 상태(true or false)</param>
    /// <returns></returns>
    public IEnumerator FlagSet(string flagCode, bool flagState)
    {
        string url =
            $"{apiUrl}/api/choices" +
            $"?username={UnityWebRequest.EscapeURL(username)}" +
            $"&choiceCode={UnityWebRequest.EscapeURL(flagCode)}" +
            $"&condition={flagState}";
        using (var req = UnityWebRequest.PostWwwForm(url, ""))
            yield return SendApi(req);
    }
}
