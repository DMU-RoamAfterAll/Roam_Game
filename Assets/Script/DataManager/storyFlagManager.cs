using System.Collections.Generic;
using UnityEngine;

//-------------------------------------------------------------------------------
// ** storyFlag Json 데이터 클래스 구조 **
//-------------------------------------------------------------------------------
[System.Serializable]
//flag 데이터 노드
public class storyFlagNode
{
    public string code;
    public string name;
    public string description;
}
//-------------------------------------------------------------------------------

public class StoryFlagManager : MonoBehaviour
{
private string flagFolderPath = "StoryGameData/CommonData/storyFlag"; //게임 스토리 분기 정보가 담긴 파일
    public List<storyFlagNode> flagList;
    private Dictionary<string, storyFlagNode> flagDict;

    private void Awake()
    {
        LoadflagJson();
    }

    /// <summary>
    /// storyFlag 데이터 파일 로드
    /// </summary>
    public void LoadflagJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(flagFolderPath); //Json 파일 로드
        if (jsonFile == null)
        {
            Debug.LogError($"[storyFlagManager] 파일을 찾을 수 없음: storyFlag.json");
            return;
        }

        //JSON 텍스트를 리스트로 변환
        flagList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<storyFlagNode>>(jsonFile.text);

        if (flagList == null || flagList.Count == 0)
        {
            Debug.LogError("[storyFlagManager] flagList가 비어있거나 파싱에 실패했습니다.");
            return;
        }

        //리스트에서 Dictionary로 변환
        flagDict = new Dictionary<string, storyFlagNode>();
        foreach (var item in flagList)
        {
            flagDict[item.code] = item;
        }

        Debug.Log("Reading File : storyFlag.json"); //파일 로드 확인 로그
    }

    /// <summary>
    /// 플래그 코드를 사용하여 플래그 정보를 가져오는 함수
    /// </summary>
    /// <param name="code">플래그 코드</param>
    /// <returns>플래그 정보</returns>
    public storyFlagNode GetFlagByCode(string code)
    {
        if (flagDict.TryGetValue(code, out var data))
            return data;
            
        Debug.LogWarning($"[storyFlagManager] 플래그 코드 {code}를 찾을 수 없습니다.");
        return null;
    }
}
