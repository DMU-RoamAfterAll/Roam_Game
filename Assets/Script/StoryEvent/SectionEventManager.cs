using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine.UI;
using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;

//-------------------------------------------------------------------------------
// ** Section Event Json 데이터 클래스 구조 **
//-------------------------------------------------------------------------------

[System.Serializable]
//공통 보조 노드
public class CommonNode
{
    public string next; //다음 출력을 위한 노드 키값, 본문이 출력된 후 해당 노드로 이동
    public ActionNode action; //키값을 주어 다양한 기능을 제어 가능
}

[System.Serializable]
//Text와 Menu 노드 안에 작성 가능한 부가 기능 노드, 키값을 주어 다양한 기능을 제어 가능
public class ActionNode
{
    public string image; //삽화를 변경하기 위한 삽화 명을 작성하는 노드, 본문을 출력하기 전 삽화 변경이 이루어짐
    public List<ItemData> checkI; //아이템 수량 확인을 위한 노드, <"아이템 코드", 갯수>형식으로 작성
    public List<ItemData> getI; //아이템 획득 기능을 위한 노드, <"아이템 코드", 갯수>형식으로 작성
    public List<ItemData> lostI; //아이템 유실 기능을 위한 노드, <"아이템 코드", 갯수>형식으로 작성
    public List<WeaponData> checkW; //무기 수량 확인을 위한 노드, <"무기 코드", 갯수> 형식으로 작성
    public List<WeaponData> getW; //무기 획득 기능을 위한 노드, <"무기 코드", 갯수> 형식으로 작성
    public List<WeaponData> lostW; //무기 유실 기능을 위한 노드, <"무기 코드", 갯수> 형식으로 작성
    public List<SkillData> checkS; //스킬 보유 레벨 확인을 위한 노드, <"스킬 코드", 레벨> 형식으로 작성
    public List<SkillData> getS; //스킬 획득 기능을 위한 노드, <"스킬 코드", 레벨> 형식으로 작성
    public List<FlagData> flagSet; //플래그 설정을 위한 플래그 명을 작성을 위한 노드, <"플래그명", boolean>형식으로 작성
    public List<FlagData> flagCheck; //본문을 내보내기 위해 플래그를 확인을 위한 노드, <"플래그명", boolean>형식으로 작성
    public List<ProbData> prob; //확률에 따라 다른 노드로 이동하기 위한 노드, <"노드 키값", 확률>형식으로 작성
}

[System.Serializable]
//아이템 제어를 위한 데이터 모델
public class ItemData
{
    public string itemCode; //아이템 코드명
    public int amount; //아이템 갯수
}

[System.Serializable]
//무기 제어를 위한 데이터 모델
public class WeaponData
{
    public string weaponCode; //무기 코드명
    public int amount; //무기 갯수
}

[System.Serializable]
//무기 제어를 위한 데이터 모델
public class SkillData
{
    public string skillCode; //스킬 코드명
    public int skillLevel; //스킬 레벨
}

[System.Serializable]
//플래그 제어를 위한 데이터 모델
public class FlagData
{
    public string flagCode; //플래그 코드명
    public bool flagState; //플래그 상태
}

[System.Serializable]
//확률 이동 제어를 위한 데이터 모델
public class ProbData
{
    public string next; //다음 출력을 위한 노드 키값, 선택지 확률값에 따라 해당 노드로 이동
    public int probability; //선택지 확률
}

//-------------------------------------------------------------------------------

[System.Serializable]
//텍스트 본문을 출력하는 노드, Text1 Text2 ... 혹은 Result1 Result2 ... 등으로 작성
public class TextNode : CommonNode
{
    public List<string> value; //본문 내용을 적는 노드, 리스트를 사용하여 여러 문장을 나누어 작성
}

//-------------------------------------------------------------------------------

[System.Serializable]
//선택지를 생성하는 노드, Menu1 Menu2 ... 등으로 작성
public class MenuNode
{
    public List<MenuOption> menuOption;
}

public class MenuOption : CommonNode
{
    public string id; //선택지를 구분하기 위한 키값
    public string label; //선택지 내용
}

//-------------------------------------------------------------------------------

[System.Serializable]
//전투씬을 출력하는 노드, Battle1 Battle2 ... 등으로 작성
public class BattleNode
{
    public List<string> battleIntro; //전투 시작 전 인트로 내용을 적는 노드, 리스트를 사용하여 여러 문장을 나누어 작성
    public List<string> battleOrder; //전투 진행 순서를 결정하는 노드, 리스트 앞쪽 순서부터 순서대로 진행
    public List<string> battleTriggers; //전투시 적용할 특성
    public string battleWin; //전투 승리 시 다음 출력을 위한 노드 키값, 본문이 출력된 후 해당 노드로 이동
    public string battleLose; //전투 패배 시 다음 출력을 위한 노드 키값, 본문이 출력된 후 해당 노드로 이동
}

//-------------------------------------------------------------------------------

public class SectionEventManager : MonoBehaviour
{
    private Dictionary<string, object> sectionData = new Dictionary<string, object>(); //파싱된 Json데이터
    private BattleEventManager battleEventManager;
    private EventDisplayManager eventDisplayManager;
    private SectionEventParser sectionEventParser;
    private UserDataManager userDataManager;
    private DataService dataService;

    //디버깅용 변수
    TextNode testjson = null;

    private void Awake()
    {
        //참조 캐싱
        battleEventManager = GetComponent<BattleEventManager>();
        eventDisplayManager = GetComponent<EventDisplayManager>();
        sectionEventParser = GetComponent<SectionEventParser>();
        userDataManager = GetComponent<UserDataManager>();
        dataService = GetComponent<DataService>();

        LoadJson(); //Json파일 로드
    }

    private void Start()
    {
        eventDisplayManager.dialogueText.text = string.Empty; //시작시 텍스트 비우기

        //Json테스트 출력
        testjson = GetTextNode("Text1");
        Debug.Log(testjson.value[0]);
        StartCoroutine(StartDialogue("Text1"));
    }

    /// <summary>
    /// Json Event 데이터 파일 로드 및 파싱
    /// </summary>
    public void LoadJson()
    {
        string filePath = Path.ChangeExtension(GameDataManager.Instance.sectionPath, null);

        TextAsset jsonFile = Resources.Load<TextAsset>(filePath); //Json 파일 로드
        if (jsonFile == null)
        {
            Debug.LogError($"[{GetType().Name}] 파일을 찾을 수 없음: {filePath}.json");
            return;
        }
        //Json파일에서 텍스트 데이터를 가져와 Json객체 구조로 변경
        string jsonText = jsonFile.text;
        JObject root = JObject.Parse(jsonText);

        // 각 노드를 순회하며 타입에 따라 파싱
        foreach (var pair in root)
        {
            string key = pair.Key;
            JObject nodeObj = (JObject)pair.Value;

            //세션 정보 무시
            if (key == "SectionInfo")
            {
                continue;
            }
            //본문과 선택지 여부에 따라 저장
            if (key.StartsWith("Text") || key.StartsWith("Result") || key.StartsWith("Post"))
            {
                // action 빼고 복제본 만들기
                JObject nodeClone = (JObject)nodeObj.DeepClone();
                nodeClone.Remove("action");

                TextNode text = nodeClone.ToObject<TextNode>();

                //action 수동 파싱
                if (nodeObj.TryGetValue("action", out var actionToken) && actionToken is JObject actionObj)
                    text.action = sectionEventParser.ParseActionNode(actionObj);

                sectionData[key] = text; //키가 존재하지 않으면 동적 추가
            }
            else if (key.StartsWith("Menu"))
            {
                var optionsToken = nodeObj["menuOption"] as JArray; //메뉴 옵션 저장

                // action 빼고 복제본 만들기
                JObject nodeClone = (JObject)nodeObj.DeepClone();
                if (nodeClone["menuOption"] is JArray optArrayClone)
                {
                    foreach (var opt in optArrayClone)
                    {
                        if (opt is JObject optObj)
                            optObj.Remove("action"); //옵션 안의 action 제거
                    }
                }

                MenuNode menu = nodeClone.ToObject<MenuNode>();

                //action 수동 파싱
                if (optionsToken != null && menu?.menuOption != null)
                {
                    for (int i = 0; i < menu.menuOption.Count && i < optionsToken.Count; i++) //메뉴 옵션을 순회하며 action 파싱
                    {
                        var optSrc = optionsToken[i] as JObject;
                        if (optSrc == null) continue;

                        if (optSrc.TryGetValue("action", out var actionToken) &&
                            actionToken is JObject actionObj)
                        {
                            menu.menuOption[i].action = sectionEventParser.ParseActionNode(actionObj);
                        }
                    }
                }
                sectionData[key] = menu; //키가 존재하지 않으면 동적 추가
            }
            else if (key.StartsWith("Battle"))
            {
                BattleNode battle = nodeObj.ToObject<BattleNode>();
                sectionData[key] = battle; //키가 존재하지 않으면 동적 추가
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] 인식할 수 없는 노드: {key}");
            }
        }
        Debug.Log("Reading File : " + filePath); //파일 로드 확인 로그
    }

    /// <summary>
    /// 텍스트 노드를 꺼내는 메소드
    /// ex) TextNode node = sectionEventManager.GetTextNode("Text2");
    ///     Debug.Log(node.value[0]);
    /// </summary>
    /// <param name="key">꺼낼 텍스트 노드의 key값</param>
    /// <returns>해당 텍스트 노드</returns>
    public TextNode GetTextNode(string key)
    {
        if (sectionData.TryGetValue(key, out object node) && node is TextNode text)
            return text;
        return null;
    }

    /// <summary>
    /// 메뉴 노드를 꺼내는 메소드
    /// </summary>
    /// <param name="key">꺼낼 메뉴 노드의 key값</param>
    /// <returns>해당 메뉴 노드</returns>
    public MenuNode GetMenuNode(string key)
    {
        if (sectionData.TryGetValue(key, out object node) && node is MenuNode menu)
            return menu;
        return null;
    }

    /// <summary>
    /// 전투씬 노드를 꺼내는 메소드
    /// </summary>
    /// <param name="key">꺼낼 전투씬 노드의 key값</param>
    /// <returns>해당 전투씬 노드</returns>
    public BattleNode GetBattleNode(string key)
    {
        if (sectionData.TryGetValue(key, out object node) && node is BattleNode battle)
            return battle;
        return null;
    }

    /// <summary>
    /// 액션 노드 처리 메소드
    /// </summary>
    /// <param name="actions">대상 액션 노드</param>
    
    public Dictionary<string, object> result;

#if UNITY_EDITOR
    [SerializeField, TextArea(5,20)]
    private string resultPreview; 

    private void OnValidate()
    {
        resultPreview = result == null
            ? "(null)"
            : string.Join("\n", result.Select(kv =>
                $"{kv.Key}: {kv.Value} ({kv.Value?.GetType().Name})"));
    }
#endif

    public Dictionary<string, object> HandleNodeActions(ActionNode actions)
    {
        if (actions == null)
        {
            return null;
        }

        result = new Dictionary<string, object>();

        //삽화 변경
        if (actions.image != null && actions.image != "" && actions.image is string imageName)
        {
            eventDisplayManager.LoadSceneSprite(imageName);
        }

        //아이템 체크
        if (actions.checkI != null && actions.checkI.Count > 0 && actions.checkI is List<ItemData> checkIData)
        {
            bool checkIResult = true;
            bool checkIFlag = true;
            foreach (ItemData actionItem in checkIData)
            {
                if (actionItem != null && actionItem.itemCode != "" && actionItem.amount >= 0 &&
                actionItem.itemCode is string itemCode && actionItem.amount is int amount)
                {
                    ItemDataNode itemData = dataService.Item.GetItemByCode(itemCode);

                    StartCoroutine(userDataManager.ItemCheck( //api 메소드
                        onResult: list =>
                        {
                            foreach (var it in list)
                            {
                                if (itemCode == it.itemCode)
                                {
                                    if (amount <= it.amount)
                                    {
                                        Debug.Log($"{itemData.name}의 수량이 필요 수량을 만족합니다.");
                                        checkIFlag = true;
                                        break;
                                    }
                                    else
                                    {
                                        Debug.Log($"{itemData.name}의 수량이 필요 수량을 만족하지 않습니다. (추가 필요 수량 : {amount - it.amount})");
                                        checkIResult = false; //하나라도 수량을 만족하지 못했을 시 false
                                        checkIFlag = true;
                                        break;
                                    }
                                }
                            }
                            if (checkIFlag)
                            {
                                checkIResult = false; //아이템 미보유 시 false
                            }
                            result["checkI"] = checkIResult;
                            Debug.Log("Item Check = " + result["checkI"]);
                        },
                        onError: (code, msg) => Debug.LogError($"[{GetType().Name}] 아이템 불러오기 실패: {code}/{msg}")
                        )
                    );
                }
            }
        }

        //아이템 획득
        if (actions.getI != null && actions.getI.Count > 0 && actions.getI is List<ItemData> getIData)
        {
            foreach (ItemData actionItem in getIData)
            {
                if (actionItem != null && actionItem.itemCode != "" && actionItem.amount != 0 &&
                actionItem.itemCode is string itemCode && actionItem.amount is int amount)
                {
                    ItemDataNode itemData = dataService.Item.GetItemByCode(itemCode);

                    //테스트 출력
                    Debug.Log($"\'{itemData.code}\'아이템을 {amount}개 획득했습니다.");
                    StartCoroutine(userDataManager.GetItem(itemData.code, amount)); //api 메소드
                }
            }
        }

        //아이템 유실
        if (actions.lostI != null && actions.lostI.Count > 0 && actions.lostI is List<ItemData> lostIData)
        {
            foreach (ItemData actionItem in lostIData)
            {
                if (actionItem != null && actionItem.itemCode != "" && actionItem.amount != 0 &&
                actionItem.itemCode is string itemCode && actionItem.amount is int amount)
                {
                    ItemDataNode itemData = dataService.Item.GetItemByCode(itemCode);

                    //테스트 출력
                    Debug.Log($"\'{itemData.name}\'아이템을 {amount}개 잃었습니다.");
                    StartCoroutine(userDataManager.LostItem(itemData.code, amount)); //api 메소드
                }
            }
        }

        //무기 체크
        if (actions.checkW != null && actions.checkW.Count > 0 && actions.checkW is List<WeaponData> checkWData)
        {
            bool checkWResult = true;
            bool checkWFlag = true;
            foreach (WeaponData actionWeapon in checkWData)
            {
                if (actionWeapon != null && actionWeapon.weaponCode != "" && actionWeapon.amount >= 0 &&
                actionWeapon.weaponCode is string weaponCode && actionWeapon.amount is int amount)
                {
                    WeaponDataNode weaponData = dataService.Weapon.GetWeaponByCode(weaponCode);

                    StartCoroutine(userDataManager.WeaponCheck( //api 메소드
                        onResult: list =>
                        {
                            Debug.Log("Code1" + list);
                            foreach (var it in list)
                            {
                                Debug.Log("Code2" + it.weaponCode);
                                if (weaponCode == it.weaponCode)
                                {
                                    if (amount <= it.amount)
                                    {
                                        Debug.Log($"{weaponData.name}의 수량이 필요 수량을 만족합니다.");
                                        checkWFlag = false;
                                        break;
                                    }
                                    else
                                    {
                                        Debug.Log($"{weaponData.name}의 수량이 필요 수량을 만족하지 않습니다. (추가 필요 수량 : {amount - it.amount})");
                                        checkWResult = false; //하나라도 수량을 만족하지 못했을 시 false
                                        checkWFlag = false;
                                        break;
                                    }
                                }
                            }
                            if (checkWFlag)
                            {   
                                Debug.Log("Flag = true if");
                                checkWResult = false; //무기 미보유 시 false
                            }
                            result["checkW"] = checkWResult;
                            Debug.Log("Weapon Check" + result["checkW"]);
                        },
                        onError: (code, msg) => Debug.LogError($"[{GetType().Name}] 무기 불러오기 실패: {code}/{msg}")
                        )
                    );
                }
            }
        }

        //무기 획득
        if (actions.getW != null && actions.getW.Count > 0 && actions.getW is List<WeaponData> getWData)
        {
            foreach (WeaponData actionWeapon in getWData)
            {
                if (actionWeapon != null && actionWeapon.weaponCode != "" && actionWeapon.amount != 0 &&
                actionWeapon.weaponCode is string weaponCode && actionWeapon.amount is int amount)
                {
                    WeaponDataNode weaponData = dataService.Weapon.GetWeaponByCode(weaponCode);

                    //테스트 출력
                    Debug.Log($"\'{weaponData.code}\'무기를 {amount}개 획득했습니다.");
                    StartCoroutine(userDataManager.GetWeapon(weaponData.code, amount)); //api 메소드
                }
            }
        }

        //무기 유실
        if (actions.lostW != null && actions.lostW.Count > 0 && actions.lostW is List<WeaponData> lostWData)
        {
            foreach (WeaponData actionWeapon in lostWData)
            {
                if (actionWeapon != null && actionWeapon.weaponCode != "" && actionWeapon.amount != 0 &&
                actionWeapon.weaponCode is string weaponCode && actionWeapon.amount is int amount)
                {
                    WeaponDataNode weaponData = dataService.Weapon.GetWeaponByCode(weaponCode);

                    //테스트 출력
                    Debug.Log($"\'{weaponData.name}\'무기를 {amount}개 잃었습니다.");
                    StartCoroutine(userDataManager.LostWeapon(weaponData.code, amount)); //api 메소드
                }
            }
        }

        //스킬 체크
        if (actions.checkS != null && actions.checkS.Count > 0 && actions.checkS is List<SkillData> checkSData)
        {
            bool checkSResult = true;
            bool checkSFlag = true;
            foreach (SkillData actionSkill in checkSData)
            {
                if (actionSkill != null && actionSkill.skillCode != "" && actionSkill.skillLevel >= 0 &&
                actionSkill.skillCode is string skillCode && actionSkill.skillLevel is int level)
                {
                    SkillDataNode skillData = dataService.skill.GetSkillByCode(skillCode);

                    StartCoroutine(userDataManager.SkillCheck( //api 메소드
                        onResult: list =>
                        {
                            foreach (var it in list)
                            {
                                Debug.Log("skillCode = " + it.skillCode + "skillLevel" + it.skillLevel);
                                if (skillCode == it.skillCode)
                                {
                                    if (level <= it.skillLevel)
                                    {
                                        Debug.Log($"{skillData.name}의 레벨이 필요 레벨을 만족합니다.");
                                        checkSFlag = false;
                                        break;
                                    }
                                    else
                                    {
                                        Debug.Log($"{skillData.name}의 레벨이 필요 레벨을 만족하지 않습니다. (추가 필요 레벨 : {level - it.skillLevel}), level = {level}, it.level = {it.skillLevel}");
                                        checkSResult = false; //하나라도 레벨을 만족하지 못했을 시 false
                                        checkSFlag = false;
                                        break;
                                    }
                                }
                            }
                            if (checkSFlag)
                            {
                                checkSResult = false; //보유하지 않았을 시 false
                            }
                            result["checkS"] = checkSResult;
                            Debug.Log("Skill Check" + result["checkS"]);
                        },
                        onError: (code, msg) => Debug.LogError($"[{GetType().Name}] 스킬 불러오기 실패: {code}/{msg}")
                        )
                    );
                }
            }
        }

        //스킬 획득
        if (actions.getS != null && actions.getS.Count > 0 && actions.getS is List<SkillData> getSData)
        {
            foreach (SkillData actionSkill in getSData)
            {
                if (actionSkill != null && actionSkill.skillCode != "" && actionSkill.skillLevel != 0 &&
                actionSkill.skillCode is string skillCode && actionSkill.skillLevel is int level)
                {
                    SkillDataNode skillData = dataService.skill.GetSkillByCode(skillCode);

                    //테스트 출력
                    Debug.Log($"\'{skillData.code}\'스킬의 레벨이 {level}만큼 올랐습니다.");
                    StartCoroutine(userDataManager.GetSkill(skillData.code, level)); //api 메소드
                }
            }
        }

        //플래그 설정
        if (actions.flagSet != null && actions.flagSet.Count > 0 && actions.flagSet is List<FlagData> fSetData)
        {
            foreach (FlagData actionFlag in fSetData)
            {
                if (actionFlag != null && actionFlag.flagCode != "" &&
                actionFlag.flagCode is string flagCode && actionFlag.flagState is bool flagState)
                {
                    storyFlagNode FlagData = dataService.StoryFlag.GetFlagByCode(flagCode);

                    //테스트 출력
                    Debug.Log($"\'{FlagData.name}\'플래그 상태를 {flagState}로 변경했습니다.");
                    StartCoroutine(userDataManager.FlagSet(flagCode, flagState)); //api 메소드
                }
            }
        }

        //플래그 체크
        if (actions.flagCheck != null && actions.flagCheck.Count > 0 && actions.flagCheck is List<FlagData> fCheckData)
        {
            bool flagCheckResult = true;
            bool flagcheckFlag = true;
            foreach (FlagData actionFlag in fCheckData)
            {
                if (actionFlag != null && actionFlag.flagCode != "" &&
                actionFlag.flagCode is string flagCode && actionFlag.flagState is bool flagState)
                {
                    storyFlagNode FlagData = dataService.StoryFlag.GetFlagByCode(flagCode);

                    StartCoroutine(userDataManager.FlagCheck( //api 메소드
                        onResult: list =>
                        {
                            foreach (var it in list)
                            {
                                if (flagCode == it.flagCode)
                                {
                                    if (flagState == it.flagState)
                                    {
                                        Debug.Log($"{FlagData.name}는 {flagState}를 만족합니다."); //테스트 출력
                                        flagcheckFlag = false;
                                        break;
                                    }
                                    else
                                    {
                                        Debug.Log($"{FlagData.name}는 {flagState}를 만족하지 않습니다."); //테스트 출력
                                        flagCheckResult = false; //하나라도 상태를 만족하지 못했을 시 false
                                        flagcheckFlag = false;
                                        break;
                                    }
                                }
                            }
                            if (flagcheckFlag)
                            {
                                flagCheckResult = false; //플래그 미보유 시 false
                            }
                            result["flagCheck"] = flagCheckResult;
                            Debug.Log("Flag Check" + result["flagCheck"]);
                        },
                        onError: (code, msg) => Debug.LogError($"[{GetType().Name}] 플래그 불러오기 실패: {code}/{msg}")
                        )
                    );
                }
            }
        }

        //확률 이동
        if (actions.prob != null && actions.prob.Count > 0 && actions.prob is List<ProbData> probData)
        {
            List<(string, float)> randomNext = new List<(string, float)>();

            foreach (ProbData actionProb in probData)
            {
                if (actionProb != null && actionProb.next != "" &&
                actionProb.next is string next && actionProb.probability is int probability)
                {
                    randomNext.Add((next, (float)probability));
                }
            }
            result["prob"] = SecureRng.Weighted(randomNext);
            Debug.Log("Prob" + result["prob"]);
        }

        return result;
    }

    public bool checkValidation(Dictionary<string, object> actionResult)
    {
        Debug.Log("actionResult == " + actionResult);
        OnValidate();
        if (actionResult == null || actionResult.Count == 0)
        return true; // 기본 true로 반환
        
        var checkResults = actionResult.Values.Where(v => v is bool).Cast<bool>(); //check 확인

        bool checkResult = checkResults.Any() //요류 존재 확인 
        ? checkResults.Aggregate(true, (a, b) => a && b) //AND연산, check중 하나라도 false면 false
        : true; //비어있으면 기본 true

        Debug.Log($"action check 완료, 선택지 {(!checkResult ? "비활성화" : "활성화")}");
        return checkResult;
    }

    //-------------------------------------------------------------------------------
    // ** 게임 내 오브젝트 출력 부분 **
    //-------------------------------------------------------------------------------
    /// <summary>
    /// 노드의 출력을 위한 메소드, 노드의 키값에 따라 알맞은 출력 메소드를 실행
    /// </summary>
    /// <param name="nodeKey">출력을 시작할 노드의 키값</param>
    public IEnumerator StartDialogue(string nodeKey)
    {
        Debug.Log($"{nodeKey} 노드 출력 실행");
        if (sectionData.TryGetValue(nodeKey, out object node))
        {
            //---------------본문 출력---------------
            if (node is TextNode textNode)
            {
                string nextNode = textNode.next;

                var actionResult = HandleNodeActions(textNode.action); //액션 실행
                if (actionResult != null &&
                    actionResult.TryGetValue("prob", out var objNext) &&
                    objNext is string next &&
                    !string.IsNullOrEmpty(next)) //prob값이 존재한다면 적용
                {
                    nextNode = next;
                }

                if (!string.IsNullOrEmpty(nextNode))
                {
                    if (nextNode.Equals("EndS") || nextNode.Equals("EndF"))
                    {
                        yield return StartCoroutine(
                            eventDisplayManager.DisplayScript(
                                textNode.value,
                                "조사 종료",
                                null)
                        );

                        if(!SceneManager.GetSceneByName(SceneList.Map).isLoaded) {
                            SwitchSceneManager.Instance.EnterBaseFromBoot();
                        }

                        if(nextNode.Equals("EndS")) {
                            SwitchSceneManager.Instance.sectionCleared = true;
                        }
                        else if (nextNode.Equals("EndF")) {
                            SwitchSceneManager.Instance.sectionCleared = false;
                        }

                        SwitchSceneManager.GoToMapScene(); //다음 노드가 없다면 조사를 종료하고 씬 이동
                    }
                    else
                    {
                        yield return StartCoroutine(
                            eventDisplayManager.DisplayScript(
                                textNode.value,
                                eventDisplayManager.nextText,
                                null)
                        );
                        StartCoroutine(StartDialogue(nextNode)); //출력이 끝나면 다음 노드로
                    }
                }
                else
                {
                    Debug.Log($"[{GetType().Name}] TextNode의 next 값이 없습니다. 종료 또는 대기 처리 필요.");
                }
            }
            //---------------선택지 출력---------------
            else if (node is MenuNode menuNode)
            {
                foreach (MenuOption option in menuNode.menuOption)
                {
                    var actionResult = HandleNodeActions(option.action); //액션 실행
                    eventDisplayManager.DisplayMenuButton(option, checkValidation(actionResult), () =>
                    {
                        //선택지 선택 후 진행
                        Debug.Log($"선택됨: {option.id}");
                        string nextNode = option.next;

                        if (option.action != null)
                        {
                            Debug.Log($"[Action 실행]");
                            
                            if (actionResult.TryGetValue("prob", out var objNext) &&
                                objNext is string next &&
                                !string.IsNullOrEmpty(next)) //prob값이 존재한다면 적용
                            {
                                nextNode = next;
                            }
                        }

                        if (!string.IsNullOrEmpty(nextNode))
                        {
                            StartCoroutine(StartDialogue(nextNode)); //선택 완료시 결과 노드로 이동
                        }
                        else
                        {
                            Debug.Log($"[{GetType().Name}] MenuNode의 next 값이 없습니다. 종료 또는 대기 처리 필요.");
                        }
                    });
                }
            }
            //---------------전투씬 출력---------------
            else if (node is BattleNode battleNode)
            {
                battleEventManager.EnterBattleTurn(battleNode);
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] 알 수 없는 노드 타입: {node.GetType()}");
            }
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] {nodeKey}노드를 찾을 수 없습니다.");
        }
    }
    //-------------------------------------------------------------------------------
}
