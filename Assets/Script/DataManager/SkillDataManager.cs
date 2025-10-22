using System.Collections.Generic;
using UnityEngine;

//-------------------------------------------------------------------------------
// ** skill Json 데이터 클래스 구조 **
//-------------------------------------------------------------------------------
[System.Serializable]
//스킬 데이터 노드
public class SkillDataNode
{
    public string code; //스킬 코드
    public string name; //스킬 이름
    public string description; //스킬 설명
    public string category; //스킬 분류
}
//-------------------------------------------------------------------------------


public class SkillDataManager : MonoBehaviour
{
    private string skillFolderPath = "StoryGameData/CommonData/skill"; //게임 스킬 정보가 담긴 파일
    public List<SkillDataNode> skillList;
    private Dictionary<string, SkillDataNode> skillDict;

    private void Awake()
    {
        LoadSkillJson();
    }

    /// <summary>
    /// Skill 데이터 파일 로드
    /// </summary>
    public void LoadSkillJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(skillFolderPath); //Json 파일 로드
        if (jsonFile == null)
        {
            Debug.LogError($"[{GetType().Name}] 파일을 찾을 수 없음: skill.json");
            return;
        }

        //JSON 텍스트를 리스트로 변환
        skillList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SkillDataNode>>(jsonFile.text);

        if (skillList == null || skillList.Count == 0)
        {
            Debug.LogError($"[{GetType().Name}] skillList가 비어있거나 파싱에 실패했습니다.");
            return;
        }

        //리스트에서 Dictionary로 변환
        skillDict = new Dictionary<string, SkillDataNode>();
        foreach (var item in skillList)
        {
            skillDict[item.code] = item;
        }

        Debug.Log("Reading File : skill.json"); //파일 로드 확인 로그
    }

    /// <summary>
    /// 스킬 코드를 사용하여 스킬 정보를 가져오는 함수
    /// </summary>
    /// <param name="code">스킬 코드</param>
    /// <returns>스킬 정보</returns>
    public SkillDataNode GetSkillByCode(string code)
    {
        if (skillDict.TryGetValue(code, out var data))
            return data;
        Debug.LogWarning($"[{GetType().Name}] 스킬 코드 {code}을(를) 찾을 수 없습니다.");
        return null;
    }
}
