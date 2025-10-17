using System.Collections.Generic;
using UnityEngine;

//-------------------------------------------------------------------------------
// ** enemy Json 데이터 클래스 구조 **
//-------------------------------------------------------------------------------
[System.Serializable]
//아이템 데이터 노드
public class EnemyScriptNode
{
    public string code; //적 코드
    public string name; //적 이름
    public List<string> atkHit; //플레이어 맨손 공격 적중 (적 회피 실패) 스크립트
    public List<string> atkHit2001; //플레이어 삽 공격 적중 (적 회피 실패) 스크립트
    public List<string> atkHit2002; //플레이어 식칼 공격 적중 (적 회피 실패) 스크립트
    public List<string> atkHit2003; //플레이어 녹슨 파이프 공격 적중 (적 회피 실패) 스크립트
    public List<string> atkMiss; //플레이어 공격 실패 (적 회피 성공) 스크립트
    public List<string> ctrPHit; //플레이어 반격 성공 (적 공격 실패) 스크립트
    public List<string> evdSuccess; //플레이어 회피 성공 (적 공격 실패) 스크립트
    public List<string> evdMiss; //플레이어 회피 실패 (적 공격 성공) 스크립트
    public List<string> ctrEHit; //플레이어 회피 실패 (적 반격 성공) 스크립트
    public List<string> battleEnd; //전투 완료 스크립트
    public List<string> battleDefeat; //전투 패배 스크립트
}
//-------------------------------------------------------------------------------

public class EnemyScriptManager : MonoBehaviour
{
    //게임 적 스크립트 정보가 담긴 파일
    private string enemyScriptFolderPath = "StoryGameData/SectionData/BattleEvent/BattleSection/enemyScript";
    public List<EnemyScriptNode> enemyScriptList;
    private Dictionary<string, EnemyScriptNode> enemyScriptDict;

    private void Awake()
    {
        LoadEnemyJson();
    }

    /// <summary>
    /// 적 스크립트 데이터 파일 로드
    /// </summary>
    public void LoadEnemyJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(enemyScriptFolderPath); //Json 파일 로드
        if (jsonFile == null)
        {
            Debug.LogError($"[{GetType().Name}] 파일을 찾을 수 없음: enemyScript.json");
            return;
        }

        //JSON 텍스트를 리스트로 변환
        enemyScriptList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<EnemyScriptNode>>(jsonFile.text);

        if (enemyScriptList == null || enemyScriptList.Count == 0)
        {
            Debug.LogError($"[{GetType().Name}] enemyScriptList가 비어있거나 파싱에 실패했습니다.");
            return;
        }

        //리스트에서 Dictionary로 변환
        enemyScriptDict = new Dictionary<string, EnemyScriptNode>();
        foreach (var script in enemyScriptList)
        {
            enemyScriptDict[script.code] = script;
        }

        Debug.Log("Reading File : enemyScript.json"); //파일 로드 확인 로그
    }

    /// <summary>
    /// 적 코드를 사용하여 적 스크립트를 가져오는 함수
    /// </summary>
    /// <param name="code">적 코드</param>
    /// <returns>적 스크립트</returns>
    public EnemyScriptNode GetEnemyScriptByCode(string code)
    {
        if (enemyScriptDict.TryGetValue(code, out var data))
            return data;
        Debug.LogWarning($"[{GetType().Name}] 적 코드 {code}의 스크립트 내용을 찾을 수 없습니다.");
        return null;
    }
}
