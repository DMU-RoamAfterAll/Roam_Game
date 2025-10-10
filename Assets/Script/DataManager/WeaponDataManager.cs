using UnityEngine;
using System.Collections.Generic;
using System.Linq;

//-------------------------------------------------------------------------------
// ** Weapon Json 데이터 클래스 구조 **
//-------------------------------------------------------------------------------
[System.Serializable]
//아이템 데이터 노드
public class WeaponDataNode
{
    public string code; //무기 코드
    public string name; //무기 이름
    public string description; //무기 설명
    public List<string> category; //무기 분류
    public int damage; //무기 데미지 값
    public int durability; //무기 내구도 값
    public List<string> breakWarningMessages; //무기 파괴 경고 스크립트
    public List<string> breakMessages; //무기 파괴 스크립트
}
//-------------------------------------------------------------------------------


public class WeaponDataManager : MonoBehaviour
{
    private string weaponFolderPath = "StoryGameData/CommonData/weapon"; //게임 무기 정보가 담긴 파일
    public List<WeaponDataNode> weaponList;
    private Dictionary<string, WeaponDataNode> weaponDict;

    private void Awake()
    {
        LoadWeaponJson();
    }

    /// <summary>
    /// Weapon 데이터 파일 로드
    /// </summary>
    public void LoadWeaponJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(weaponFolderPath); //Json 파일 로드
        if (jsonFile == null)
        {
            Debug.LogError($"[{GetType().Name}] 파일을 찾을 수 없음: weapon.json");
            return;
        }

        //JSON 텍스트를 리스트로 변환
        weaponList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<WeaponDataNode>>(jsonFile.text);

        if (weaponList == null || weaponList.Count == 0)
        {
            Debug.LogError($"[{GetType().Name}] weaponList가 비어있거나 파싱에 실패했습니다.");
            return;
        }

        //리스트에서 Dictionary로 변환
        weaponDict = new Dictionary<string, WeaponDataNode>();
        foreach (var item in weaponList)
        {
            weaponDict[item.code] = item;
        }

        Debug.Log("Reading File : weapon.json"); //파일 로드 확인 로그
    }

    /// <summary>
    /// 무기 코드를 사용하여 무기 정보를 가져오는 함수
    /// </summary>
    /// <param name="code">무기 코드</param>
    /// <returns>무기 정보</returns>
    public WeaponDataNode GetWeaponByCode(string code)
    {
        if (weaponDict.TryGetValue(code, out var data))
            return data;
        Debug.LogWarning($"[{GetType().Name}] 무기 코드 {code}을(를) 찾을 수 없습니다.");
        return null;
    }
    
    /// <summary>
    /// 무기 코드들을 사용하여 무기 정보를 가져오는 함수
    /// </summary>
    /// <param name="codes">무기 코드 리스트</param>
    /// <returns>무기 정보([코드:정보]인 딕셔너리 형태)</returns>
    public Dictionary<string, WeaponDataNode> GetWeaponsByCodes(List<string> codes)
    {
        var dict = new Dictionary<string, WeaponDataNode>();
        foreach (var code in codes.Distinct())
        {
            var w = GetWeaponByCode(code);
            if (w != null) dict[code] = w;
        }
        return dict;
    }
}
