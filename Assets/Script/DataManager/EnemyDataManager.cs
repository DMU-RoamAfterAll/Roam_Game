using UnityEngine;
using System.Collections.Generic;

//-------------------------------------------------------------------------------
// ** enemy Json 데이터 클래스 구조 **
//-------------------------------------------------------------------------------
[System.Serializable]
//아이템 데이터 노드
public class EnemyDataNode
{
    public string code; //적 코드
    public string name; //적 이름
    public string image; //삽화 파일명 (경로 생략, 확장자 미포함)
    public int hp; //체력
    public int atk; //공격력
    public int spd; //민첩
    public int hitRate; //공격 확률
    public int evasionRate; //회피 확률
    public int CounterRate; //반격 확률
    public string description; //적 설명
}
//-------------------------------------------------------------------------------

public class EnemyDataManager : MonoBehaviour
{
    private string enemyFolderPath = "StoryGameData/CommonData/enemy"; //게임 적 정보가 담긴 파일
    public List<EnemyDataNode> enemyList;
    private Dictionary<string, EnemyDataNode> enemyDict;

    private void Awake()
    {
        LoadEnemyJson();
    }

    /// <summary>
    /// Item 데이터 파일 로드
    /// </summary>
    public void LoadEnemyJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(enemyFolderPath); //Json 파일 로드
        if (jsonFile == null)
        {
            Debug.LogError($"[{GetType().Name}] 파일을 찾을 수 없음: enemy.json");
            return;
        }

        //JSON 텍스트를 리스트로 변환
        enemyList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<EnemyDataNode>>(jsonFile.text);

        if (enemyList == null || enemyList.Count == 0)
        {
            Debug.LogError($"[{GetType().Name}] enemyList가 비어있거나 파싱에 실패했습니다.");
            return;
        }

        //리스트에서 Dictionary로 변환
        enemyDict = new Dictionary<string, EnemyDataNode>();
        foreach (var item in enemyList)
        {
            enemyDict[item.code] = item;
        }

        Debug.Log("Reading File : enemy.json"); //파일 로드 확인 로그
    }
    
    /// <summary>
    /// 적 코드를 사용하여 적 정보를 가져오는 함수
    /// </summary>
    /// <param name="code">적 코드</param>
    /// <returns>적 정보</returns>
    public EnemyDataNode GetEnemyByCode(string code)
    {
        if (enemyDict.TryGetValue(code, out var data))
            return data;
        Debug.LogWarning($"[{GetType().Name}] 적 코드 {code}을(를) 찾을 수 없습니다.");
        return null;
    }
}
