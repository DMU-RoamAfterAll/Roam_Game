using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Resources;

public class UserDataManager : MonoBehaviour
{
    protected string apiUrl = "http://125.176.246.14:8081"; //api 주소
    public string username = "admin"; //테스트용 유저 이름

    /// <summary>
    /// api를 통해 서버에 아이템을 삽입하는 함수
    /// </summary>
    /// <param name="itemName">아이템 이름</param>
    /// <param name="itemAmount">아이템 갯수</param>
    /// <returns></returns>
    public IEnumerator InsertItem(string itemCode, int itemAmount)
    {
        string insertUrl = apiUrl + "/api/inventory/items"
        + $"?username={username}&itemCode={itemCode}&amount={itemAmount}"; //전용 api
        Debug.Log("url: "+insertUrl);

        UnityWebRequest www = UnityWebRequest.PostWwwForm(insertUrl, "");
        www.SetRequestHeader("Authorization", $"Bearer eyJhbGciOiJIUzUxMiJ9.eyJzdWIiOiJhZG1pbiIsImlhdCI6MTc1NDgxNjM0NSwiZXhwIjoxNzU0ODE3MjQ1fQ.ScFUtai6Ctcq-16z_EjUCVoMTKL9ZZGdRTYMPSW8EkhLUa9ZWMPIoutqBXpdNqnPHLrFsfUE4hMYiEUECo9DYw");
        www.timeout = 10;
        yield return www.SendWebRequest(); //네트워크 전송

        //데이터 전송 확인
        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Insert Success: " + www.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Insert Failed: " + www.error);
        }
    }
}
