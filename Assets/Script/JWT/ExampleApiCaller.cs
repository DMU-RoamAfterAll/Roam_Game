using System.Collections;
using UnityEngine;

public class ExampleApiCaller : MonoBehaviour
{
    public void GetInventory()
    {
        StartCoroutine(Call());
        IEnumerator Call()
        {
            var url = $"{GameDataManager.Data.baseUrl}/api/inventory/weapons?username=cnwvid";
            yield return AuthService.SendAuthorized("GET", url, null,
                onSuccess: json => {
                    Debug.Log("[API OK] " + json);
                },
                onError: err => {
                    Debug.LogWarning("[API ERR] " + err);
                }
            );
        }
    }
}
