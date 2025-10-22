using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

public class UserDataManager : MonoBehaviour
{
    protected string apiUrl = "http://125.176.246.14:8081"; //api 주소
    protected string username = "cnwvid"; //테스트용 유저 이름
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

        //디버깅용 로그
        Debug.Log($"[{GetType().Name}] code={req.responseCode}, result={req.result}");
        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log($"[{GetType().Name}] OK body='{req.downloadHandler.text}'");
        else
            Debug.LogError($"[{GetType().Name}] FAIL {req.responseCode} / {req.error}");
    }

    /// <summary>
    /// 조회 Json파일을 받아 Unity List로 변환해주는 함수
    /// </summary>
    /// <typeparam name="T">List 타입</typeparam>
    /// <param name="req">변환할 리퀘스트</param>
    /// <param name="onResult">리퀘스트 성공시 콜백</param>
    /// <param name="onError">리퀘스트 실패시 콜백</param>
    /// <returns></returns>
    private IEnumerator GetJsonList<T>(
        UnityWebRequest req,
        Action<List<T>> onResult,
        Action<long, string> onError
    )
    {
        yield return SendApi(req);

        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(req.responseCode, req.error);
            yield break;
        }

        var body = req.downloadHandler?.text ?? "";
        try
        {
            List<T> list;
            list = JsonConvert.DeserializeObject<List<T>>(body) ?? new List<T>();

            onResult?.Invoke(list);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{GetType().Name}] GetJsonList<{typeof(T).Name}> parse fail: {ex.Message} body='{body}'");
            onError?.Invoke(req.responseCode, "parse_error");
        }
    }

    /// <summary>
    /// api를 통해 유저의 스탯 정보를 불러오는 함수
    /// </summary>
    /// <param name="onResult">리퀘스트 성공시 콜백</param>
    /// <param name="onError">리퀘스트 실패시 콜백</param>
    /// <returns></returns>
    public IEnumerator PlayerDataLoad(Action<PlayerDataNode> onResult = null, Action<long, string> onError = null)
    {
        string url =
            $"{apiUrl}/api/player-stats/{UnityWebRequest.EscapeURL(username)}" +
            $"?username={UnityWebRequest.EscapeURL(username)}";

        using (var req = UnityWebRequest.Get(url))
        {
            yield return SendApi(req); //호출 완료 대기

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    PlayerDataNode playerData = JsonUtility.FromJson<PlayerDataNode>(req.downloadHandler.text);
                    onResult?.Invoke(playerData);
                }
                catch (Exception e)
                {
                    onError?.Invoke(req.responseCode, "JSON parsing returned null.");
                }
            }
        }
    }

    /// <summary>
    /// api를 통해 유저의 아이템 정보를 불러오는 함수
    /// </summary>
    /// <param name="onResult">리퀘스트 성공시 콜백</param>
    /// <param name="onError">리퀘스트 실패시 콜백</param>
    /// <returns></returns>
    public IEnumerator ItemCheck(Action<List<ItemData>> onResult = null, Action<long, string> onError = null)
    {
        string url =
            $"{apiUrl}/api/inventory/items" +
            $"?username={UnityWebRequest.EscapeURL(username)}";

        using (var req = UnityWebRequest.Get(url))
        {
            yield return GetJsonList<ItemData>(req, onResult, onError);
        }
    }

    /// <summary>
    /// api를 통해 서버에 아이템을 추가하는 함수
    /// </summary>
    /// <param name="itemCode">아이템 코드</param>
    /// <param name="amount">아이템 갯수</param>
    /// <returns></returns>
    public IEnumerator GetItem(string itemCode, int amount)
    {
        string url =
        $"{apiUrl}/api/inventory/items" +
        $"?username={UnityWebRequest.EscapeURL(username)}" +
        $"&itemCode={UnityWebRequest.EscapeURL(itemCode)}" +
        $"&amount={amount}";

        using (var req = UnityWebRequest.PostWwwForm(url, "")) //body부분 비우고 전송
            yield return SendApi(req);
    }

    /// <summary>
    /// api를 통해 서버에 아이템을 삭제하는 함수
    /// </summary>
    /// <param name="itemCode">아이템 코드</param>
    /// <param name="amount">아이템 갯수</param>
    /// <returns></returns>
    public IEnumerator LostItem(string itemCode, int amount)
    {
        string url =
            $"{apiUrl}/api/inventory/items" +
            $"?username={UnityWebRequest.EscapeURL(username)}" +
            $"&itemCode={UnityWebRequest.EscapeURL(itemCode)}" +
            $"&amount={amount}";

        using (var req = UnityWebRequest.Delete(url))
            yield return SendApi(req);
    }

    /// <summary>
    /// api를 통해 유저의 무기 정보를 불러오는 함수
    /// </summary>
    /// <param name="onResult">리퀘스트 성공시 콜백</param>
    /// <param name="onError">리퀘스트 실패시 콜백</param>
    /// <returns></returns>
    public IEnumerator WeaponCheck(Action<List<WeaponData>> onResult = null, Action<long, string> onError = null)
    {
        string url =
            $"{apiUrl}/api/inventory/weapons" +
            $"?username={UnityWebRequest.EscapeURL(username)}";

        using (var req = UnityWebRequest.Get(url))
        {
            yield return GetJsonList<WeaponData>(req, onResult, onError);
        }
    }

    /// <summary>
    /// api를 통해 서버에 무기를 추가하는 함수
    /// </summary>
    /// <param name="weaponCode">무기 코드</param>
    /// <param name="amount">무기 갯수</param>
    /// <returns></returns>
    public IEnumerator GetWeapon(string weaponCode, int amount)
    {
        string url =
        $"{apiUrl}/api/inventory/weapons" +
        $"?username={UnityWebRequest.EscapeURL(username)}" +
        $"&weaponCode={UnityWebRequest.EscapeURL(weaponCode)}" +
        $"&amount={amount}";

        using (var req = UnityWebRequest.PostWwwForm(url, "")) //body부분 비우고 전송
            yield return SendApi(req);
    }

    /// <summary>
    /// api를 통해 서버에 무기를 삭제하는 함수
    /// </summary>
    /// <param name="weaponCode">무기 코드</param>
    /// <param name="amount">무기 갯수</param>
    /// <returns></returns>
    public IEnumerator LostWeapon(string weaponCode, int amount)
    {
        string url =
            $"{apiUrl}/api/inventory/weapons" +
            $"?username={UnityWebRequest.EscapeURL(username)}" +
            $"&weaponCode={UnityWebRequest.EscapeURL(weaponCode)}" +
            $"&amount={amount}";

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

    /// <summary>
    /// api를 통해 유저의 플래그 정보를 불러오는 함수
    /// </summary>
    /// <param name="onResult">리퀘스트 성공시 콜백</param>
    /// <param name="onError">리퀘스트 실패시 콜백</param>
    /// <returns></returns>
    public IEnumerator FlagCheck(Action<List<FlagData>> onResult = null, Action<long, string> onError = null)
    {
        string url =
            $"{apiUrl}/api/flags" +
            $"?username={UnityWebRequest.EscapeURL(username)}";

        using (var req = UnityWebRequest.Get(url))
        {
            yield return GetJsonList<FlagData>(req, onResult, onError);
        }
    }
}
