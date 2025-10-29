using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine.UI;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
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
    public List<ItemData> checkI;
    public List<ItemData> getI;
    public List<ItemData> lostI;
    public List<WeaponData> checkW;
    public List<WeaponData> getW;
    public List<WeaponData> lostW;
    public List<SkillData> checkS;
    public List<SkillData> getS;
    public List<FlagData> flagSet;
    public List<FlagData> flagCheck;
    public List<ProbData> prob;
    public string reset; //유저 데이터를 초기화하는 노드
}

[System.Serializable]
public class ItemData { public string itemCode; public int amount; }

[System.Serializable]
public class WeaponData { public string weaponCode; public int amount; }

[System.Serializable]
public class SkillData { public string skillCode; public int skillLevel; }

[System.Serializable]
public class FlagData { public string flagCode; public bool flagState; }

[System.Serializable]
public class ProbData { public string next; public int probability; }

//-------------------------------------------------------------------------------

[System.Serializable]
public class TextNode : CommonNode
{
    public List<string> value;
}

[System.Serializable]
public class MenuNode
{
    public List<MenuOption> menuOption;
}

public class MenuOption : CommonNode
{
    public string id;
    public string label;
}

[System.Serializable]
public class BattleNode
{
    public List<string> battleIntro;
    public List<string> battleOrder;
    public List<string> battleTriggers;
    public string battleWin;
    public string battleLose;
}

// ---- 액션 평가 결과 컨테이너 ----
public class ActionEval
{
    public Dictionary<string, object> result = new Dictionary<string, object>(); // 각 체크 키 -> bool, 기타(확률 next 등)
    public HashSet<string> requiredKeys = new HashSet<string>();                  // 완료를 기다릴 키 목록
}

//-------------------------------------------------------------------------------

public class SectionEventManager : MonoBehaviour
{
    private Dictionary<string, object> sectionData = new Dictionary<string, object>();
    public BattleEventManager battleEventManager;
    public EventDisplayManager eventDisplayManager;
    public SectionEventParser sectionEventParser;
    public UserDataManager userDataManager;
    public DataService dataService;

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
        if (testjson != null && testjson.value != null && testjson.value.Count > 0)
            Debug.Log(testjson.value[0]);
        StartCoroutine(StartDialogue("Text1"));
    }

    // ---------------- JSON 로드 ----------------
    public void LoadJson()
    {
        string filePath = Path.ChangeExtension(GameDataManager.Instance.sectionPath, null);
        Debug.Log("Section filePath = " + filePath);

        TextAsset jsonFile = Resources.Load<TextAsset>(filePath);
        if (jsonFile == null)
        {
            Debug.LogError($"[{GetType().Name}] 파일을 찾을 수 없음: {filePath}.json");
            return;
        }
        string jsonText = jsonFile.text;
        JObject root = JObject.Parse(jsonText);

        foreach (var pair in root)
        {
            string key = pair.Key;
            JObject nodeObj = (JObject)pair.Value;

            if (key == "SectionInfo") continue;

            if (key.StartsWith("Text") || key.StartsWith("Result") || key.StartsWith("Post"))
            {
                JObject nodeClone = (JObject)nodeObj.DeepClone();
                nodeClone.Remove("action");

                TextNode text = nodeClone.ToObject<TextNode>();

                if (nodeObj.TryGetValue("action", out var actionToken) && actionToken is JObject actionObj)
                    text.action = sectionEventParser.ParseActionNode(actionObj);

                sectionData[key] = text;
            }
            else if (key.StartsWith("Menu"))
            {
                var optionsToken = nodeObj["menuOption"] as JArray;

                JObject nodeClone = (JObject)nodeObj.DeepClone();
                if (nodeClone["menuOption"] is JArray optArrayClone)
                {
                    foreach (var opt in optArrayClone)
                    {
                        if (opt is JObject optObj) optObj.Remove("action");
                    }
                }

                MenuNode menu = nodeClone.ToObject<MenuNode>();

                if (optionsToken != null && menu?.menuOption != null)
                {
                    for (int i = 0; i < menu.menuOption.Count && i < optionsToken.Count; i++)
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
                sectionData[key] = menu;
            }
            else if (key.StartsWith("Battle"))
            {
                BattleNode battle = nodeObj.ToObject<BattleNode>();
                sectionData[key] = battle;
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] 인식할 수 없는 노드: {key}");
            }
        }
        Debug.Log("Reading File : " + filePath);
    }

    // ---------------- 노드 Getter ----------------
    public TextNode GetTextNode(string key)
    {
        if (sectionData.TryGetValue(key, out object node) && node is TextNode text)
            return text;
        return null;
    }

    public MenuNode GetMenuNode(string key)
    {
        if (sectionData.TryGetValue(key, out object node) && node is MenuNode menu)
            return menu;
        return null;
    }

    public BattleNode GetBattleNode(string key)
    {
        if (sectionData.TryGetValue(key, out object node) && node is BattleNode battle)
            return battle;
        return null;
    }

    // ---------------- 액션 처리 ----------------

#if UNITY_EDITOR
    [SerializeField, TextArea(5,20)]
    private string resultPreview;

    private void OnValidate()
    {
        // 이 프리뷰는 마지막으로 계산한 결과만 간단히 표시
        // (선택지마다 별도의 ActionEval을 쓰므로 디버그 용도로만)
        // Null-safe
    }
#endif

    private ActionEval HandleNodeActions(ActionNode actions)
    {
        if (actions == null) return new ActionEval();

        var eval = new ActionEval();

        // ---- 삽화 변경 ----
        if (!string.IsNullOrEmpty(actions.image))
        {
            eventDisplayManager.LoadSceneSprite(actions.image);
        }

        // ---- 아이템 체크 ----
        if (actions.checkI != null && actions.checkI.Count > 0)
        {
            foreach (var a in actions.checkI)
            {
                if (a == null || string.IsNullOrEmpty(a.itemCode)) continue;

                string key = $"checkI:{a.itemCode}";
                eval.requiredKeys.Add(key);

                bool done = false;
                bool ok = false;

                StartCoroutine(userDataManager.ItemCheck(
                    onResult: list =>
                    {
                        var it = list.FirstOrDefault(x => x.itemCode == a.itemCode);
                        ok = (it != null && it.amount >= a.amount);
                        done = true;
                    },
                    onError: (code, msg) => { ok = false; done = true; }
                ));

                StartCoroutine(When(() => done, () =>
                {
                    eval.result[key] = ok;
                    Debug.Log($"Item Check {a.itemCode} >= {a.amount} => {ok}");
                }));
            }
        }

        // ---- 아이템 획득/유실 ----
        if (actions.getI != null)
        {
            foreach (var a in actions.getI)
            {
                if (a == null || string.IsNullOrEmpty(a.itemCode) || a.amount == 0) continue;
                var itemData = dataService.Item.GetItemByCode(a.itemCode);
                Debug.Log($"'{itemData.code}' 아이템 {a.amount}개 획득");
                StartCoroutine(userDataManager.GetItem(itemData.code, a.amount));
            }
        }
        if (actions.lostI != null)
        {
            foreach (var a in actions.lostI)
            {
                if (a == null || string.IsNullOrEmpty(a.itemCode) || a.amount == 0) continue;
                var itemData = dataService.Item.GetItemByCode(a.itemCode);
                Debug.Log($"'{itemData.name}' 아이템 {a.amount}개 유실");
                StartCoroutine(userDataManager.LostItem(itemData.code, a.amount));
            }
        }

        // ---- 무기 체크 ----
        if (actions.checkW != null && actions.checkW.Count > 0)
        {
            foreach (var a in actions.checkW)
            {
                if (a == null || string.IsNullOrEmpty(a.weaponCode)) continue;

                string key = $"checkW:{a.weaponCode}";
                eval.requiredKeys.Add(key);

                bool done = false;
                bool ok = false;

                StartCoroutine(userDataManager.WeaponCheck(
                    onResult: list =>
                    {
                        var it = list.FirstOrDefault(x => x.weaponCode == a.weaponCode);
                        ok = (it != null && it.amount >= a.amount);
                        done = true;
                    },
                    onError: (code, msg) => { ok = false; done = true; }
                ));

                StartCoroutine(When(() => done, () =>
                {
                    eval.result[key] = ok;
                    Debug.Log($"Weapon Check {a.weaponCode} >= {a.amount} => {ok}");
                }));
            }
        }

        // ---- 무기 획득/유실 ----
        if (actions.getW != null)
        {
            foreach (var a in actions.getW)
            {
                if (a == null || string.IsNullOrEmpty(a.weaponCode) || a.amount == 0) continue;
                var weaponData = dataService.Weapon.GetWeaponByCode(a.weaponCode);
                Debug.Log($"'{weaponData.code}' 무기 {a.amount}개 획득");
                StartCoroutine(userDataManager.GetWeapon(weaponData.code, a.amount));
            }
        }
        if (actions.lostW != null)
        {
            foreach (var a in actions.lostW)
            {
                if (a == null || string.IsNullOrEmpty(a.weaponCode) || a.amount == 0) continue;
                var weaponData = dataService.Weapon.GetWeaponByCode(a.weaponCode);
                Debug.Log($"'{weaponData.name}' 무기 {a.amount}개 유실");
                StartCoroutine(userDataManager.LostWeapon(weaponData.code, a.amount));
            }
        }

        // ---- 스킬 체크 ----
        if (actions.checkS != null && actions.checkS.Count > 0)
        {
            foreach (var a in actions.checkS)
            {
                if (a == null || string.IsNullOrEmpty(a.skillCode)) continue;

                string key = $"checkS:{a.skillCode}";
                eval.requiredKeys.Add(key);

                bool done = false;
                bool ok = false;

                StartCoroutine(userDataManager.SkillCheck(
                    onResult: list =>
                    {
                        var it = list.FirstOrDefault(x => x.skillCode == a.skillCode);
                        ok = (it != null && it.skillLevel >= a.skillLevel);
                        done = true;

                        Debug.Log($"[checkS] need {a.skillCode}:{a.skillLevel}, " +
                                  $"have {(it==null ? "none" : it.skillLevel.ToString())} -> {ok}");
                    },
                    onError: (code, msg) => { ok = false; done = true; }
                ));

                StartCoroutine(When(() => done, () =>
                {
                    eval.result[key] = ok;
                    Debug.Log($"Skill Check {a.skillCode} >= {a.skillLevel} => {ok}");
                }));
            }
        }

        // ---- 스킬 획득 ----
        if (actions.getS != null)
        {
            foreach (var a in actions.getS)
            {
                if (a == null || string.IsNullOrEmpty(a.skillCode) || a.skillLevel == 0) continue;
                var skillData = dataService.skill.GetSkillByCode(a.skillCode);
                Debug.Log($"'{skillData.code}' 스킬 레벨 +{a.skillLevel}");
                StartCoroutine(userDataManager.GetSkill(skillData.code, a.skillLevel));
            }
        }

        // ---- 플래그 설정 ----
        if (actions.flagSet != null)
        {
            foreach (var a in actions.flagSet)
            {
                if (a == null || string.IsNullOrEmpty(a.flagCode)) continue;
                var f = dataService.StoryFlag.GetFlagByCode(a.flagCode);
                Debug.Log($"'{f.name}' 플래그 = {a.flagState}");
                StartCoroutine(userDataManager.FlagSet(a.flagCode, a.flagState));
            }
        }

        // ---- 플래그 체크 ----
        if (actions.flagCheck != null && actions.flagCheck.Count > 0)
        {
            foreach (var a in actions.flagCheck)
            {
                if (a == null || string.IsNullOrEmpty(a.flagCode)) continue;

                string key = $"flagCheck:{a.flagCode}";
                eval.requiredKeys.Add(key);

                bool done = false;
                bool ok = false;

                StartCoroutine(userDataManager.FlagCheck(
                    onResult: list =>
                    {
                        var it = list.FirstOrDefault(x => x.flagCode == a.flagCode);
                        ok = (it != null && it.flagState == a.flagState);
                        done = true;
                    },
                    onError: (code, msg) => { ok = false; done = true; }
                ));

                StartCoroutine(When(() => done, () =>
                {
                    eval.result[key] = ok;
                    Debug.Log($"Flag Check {a.flagCode} == {a.flagState} => {ok}");
                }));
            }
        }

        // ---- 확률 이동 ----
        if (actions.prob != null && actions.prob.Count > 0)
        {
            var randomNext = new List<(string, float)>();
            foreach (var p in actions.prob)
            {
                if (p == null || string.IsNullOrEmpty(p.next)) continue;
                randomNext.Add((p.next, (float)p.probability));
            }
            if (randomNext.Count > 0)
            {
                eval.result["prob"] = SecureRng.Weighted(randomNext);
                Debug.Log("Prob -> " + eval.result["prob"]);
            }
        }

        if (actions.reset == "reset")
        {
            StartCoroutine(userDataManager.GameReset());
            Debug.Log("유저 데이터 리셋");
        }

        return eval;
    }

    private bool CheckValidation(Dictionary<string, object> actionResult)
    {
        if (actionResult == null || actionResult.Count == 0) return true;

        var bools = actionResult.Values.Where(v => v is bool).Cast<bool>();
        bool ok = bools.Any()
            ? bools.Aggregate(true, (a, b) => a && b)
            : true;

        Debug.Log($"action check 완료, 선택지 {(!ok ? "비활성화" : "활성화")}");
        return ok;
    }

    private IEnumerator When(System.Func<bool> predicate, System.Action action)
    {
        yield return new WaitUntil(predicate);
        action?.Invoke();
    }

    /// <summary>
    /// 모든 필요한 키가 result에 채워질 때까지 기다렸다가, 유효하면 버튼 켠다.
    /// </summary>
    private IEnumerator WaitAndEnableWhenReady(Button btn, HashSet<string> requiredKeys, Dictionary<string, object> result)
    {
        // 대기: 모든 키가 존재할 때까지
        yield return new WaitUntil(() => requiredKeys.All(k => result.ContainsKey(k)));
        // 최종 AND 판정
        bool enable = CheckValidation(result);
        btn.interactable = enable;
    }

    //-------------------------------------------------------------------------------
    // ** 게임 내 오브젝트 출력 부분 **
    //-------------------------------------------------------------------------------
    public IEnumerator StartDialogue(string nodeKey)
    {
        Debug.Log($"{nodeKey} 노드 출력 실행");
        if (sectionData.TryGetValue(nodeKey, out object node))
        {
            //---------------본문 출력---------------
            if (node is TextNode textNode)
            {
                string nextNode = textNode.next;

                var eval = HandleNodeActions(textNode.action); // (텍스트는 버튼 gating 없음)
                if (eval != null &&
                    eval.result.TryGetValue("prob", out var objNext) &&
                    objNext is string next &&
                    !string.IsNullOrEmpty(next))
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

                        if(!WeatherManager.Instance.isHiddenSectionClear) {
                            if(nextNode.Equals("EndS")) {
                                if (MapSceneDataManager.Instance?.Player?.TryGetComponent<PlayerControl>(out var pc) == true &&
                                    pc.sectionData != null)
                                {
                                    pc.sectionData.isCleared = true;
                                    SaveLoadManager.Instance?.AddClearedSectionIds(pc.sectionData.id);
                                }
                            }
                            else if (nextNode.Equals("EndF")) {
                                _ = MapSceneDataManager.Instance?.Player?.TryGetComponent<PlayerControl>(out var pc) == true
                                && pc.sectionData != null
                                && (pc.sectionData.isCleared = false);
                            }
                        }
                        else {
                            WeatherManager.Instance.isHidden = false;
                            WeatherManager.Instance.isHiddenSectionClear = false;
                        }      

                        SwitchSceneManager.GoToMapScene();
                    }
                    else
                    {
                        yield return StartCoroutine(
                            eventDisplayManager.DisplayScript(
                                textNode.value,
                                eventDisplayManager.nextText,
                                null)
                        );
                        StartCoroutine(StartDialogue(nextNode));
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
                    var eval = HandleNodeActions(option.action); // 액션 실행(비동기 시작됨)

                    // 초기는 잠가두되, 체크가 하나도 없으면 즉시 켬
                    bool initialInteract = (eval.requiredKeys.Count == 0) && CheckValidation(eval.result);

                    Button btn = eventDisplayManager.DisplayMenuButton(
                        option,
                        initialInteract,
                        () =>
                        {
                            //선택지 선택 후 진행
                            Debug.Log($"선택됨: {option.id}");
                            string nextNode = option.next;

                            if (option.action != null &&
                                eval.result.TryGetValue("prob", out var objNext) &&
                                objNext is string next &&
                                !string.IsNullOrEmpty(next))
                            {
                                nextNode = next;
                            }

                            if (!string.IsNullOrEmpty(nextNode))
                            {
                                StartCoroutine(StartDialogue(nextNode));
                            }
                            else
                            {
                                Debug.Log($"[{GetType().Name}] MenuNode의 next 값이 없습니다. 종료 또는 대기 처리 필요.");
                            }
                        });

                    // 필요한 키가 있으면 다 끝날 때까지 대기 후 버튼 켜기
                    if (eval.requiredKeys.Count > 0)
                        StartCoroutine(WaitAndEnableWhenReady(btn, eval.requiredKeys, eval.result));
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