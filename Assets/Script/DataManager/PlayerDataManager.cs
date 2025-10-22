using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

//-------------------------------------------------------------------------------
// ** enemy Json 데이터 클래스 구조 **
//-------------------------------------------------------------------------------
[System.Serializable]
//적 데이터 노드
public class PlayerDataNode
{
    public int hp; //체력
    public int atk; //공격력
    public int spd; //민첩
    public int hitRate; //공격 적중 확률
    public int evasionRate; //회피 확률
    public int CounterRate; //반격 확률
}
//-------------------------------------------------------------------------------
public class PlayerDataManager : MonoBehaviour
{
    private PlayerDataNode playerDataNode;
    private UserDataManager userDataManager;

    private void Awake()
    {
        userDataManager = GetComponent<UserDataManager>();
        LoadPlayerData();
    }

    /// <summary>
    /// 적 데이터 파일 로드
    /// </summary>
    public void LoadPlayerData()
    {
        StartCoroutine(userDataManager.PlayerDataLoad(
            onResult: userStats =>
            {
                playerDataNode = userStats;
            },
            onError: (code, msg) =>
            {
                Debug.LogError($"[{GetType().Name}] 데이터 로드 실패({code}) : {msg}");
            }
            )
        );

        Debug.Log("Loading Data : Player Data"); //파일 로드 확인 로그
    }
    
    /// <summary>
    /// 플레이어 정보를 가져오는 함수
    /// </summary>
    /// <returns>플레이어 정보</returns>
    public PlayerDataNode GetPlayerData()
    {
        if (playerDataNode != null)
            return playerDataNode;
        Debug.Log($"[{GetType().Name}] 플레이어 정보를 찾을 수 없습니다.");
        return null;
    }
}
