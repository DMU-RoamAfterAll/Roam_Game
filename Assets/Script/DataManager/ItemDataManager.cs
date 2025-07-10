using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

//-------------------------------------------------------------------------------
// ** Item Json 데이터 클래스 구조 **
//-------------------------------------------------------------------------------
[System.Serializable]
//아이템 데이터 노드
public class ItemDataNode
{
    public string code;
    public string name;
    public string description;
    public List<string> category;
}
//-------------------------------------------------------------------------------

public class ItemDataManager : MonoBehaviour
{
    private string itemFolderPath = "StoryGameData/CommonData/item"; //게임 아이템 정보가 담긴 파일
    public List<ItemDataNode> itemList;
    private Dictionary<string, ItemDataNode> itemDict;

    private void Awake()
    {
        LoadItemJson();
    }

    /// <summary>
    /// Itme 데이터 파일 로드
    /// </summary>
    public void LoadItemJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(itemFolderPath); //Json 파일 로드
        if (jsonFile == null)
        {
            Debug.LogError($"[ItemDataManager] 파일을 찾을 수 없음: item.json");
            return;
        }

        //JSON 텍스트를 리스트로 변환
        itemList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ItemDataNode>>(jsonFile.text);

        if (itemList == null || itemList.Count == 0)
        {
            Debug.LogError("[ItemDataManager] itemList가 비어있거나 파싱에 실패했습니다.");
            return;
        }

        //리스트에서 Dictionary로 변환
        itemDict = new Dictionary<string, ItemDataNode>();
        foreach (var item in itemList)
        {
            itemDict[item.code] = item;
        }

        Debug.Log("Reading File : item.json"); //파일 로드 확인 로그
    }
    
    /// <summary>
    /// 아이템 코드를 사용하여 아이템 정보를 가져오는 함수
    /// </summary>
    /// <param name="code">아이템 코드</param>
    /// <returns>아이템 정보</returns>
    public ItemDataNode GetItemByCode(string code)
    {
        if (itemDict.TryGetValue(code, out var data))
            return data;
        Debug.LogWarning($"[ItemDataManager] 아이템 코드 {code}를 찾을 수 없습니다.");
        return null;
    }
}
