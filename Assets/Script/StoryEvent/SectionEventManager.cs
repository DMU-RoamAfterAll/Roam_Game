using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using KoreanTyper;
using System;
using System.Security.Cryptography.X509Certificates;

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
//모든 노드 안에 작성 가능한 부가 기능 노드, 키값을 주어 다양한 기능을 제어 가능
public class ActionNode
{
    public string image; //삽화를 변경하기 위한 삽화 명을 작성하는 노드, 본문을 출력하기 전 삽화 변경이 이루어짐
    public ItemData get; //아이템 획득 기능을 위한 노드, <"아이템 코드", 갯수>형식으로 작성
    public ItemData lost; //아이템 유실 기능을 위한 노드, <"아이템 코드", 갯수>형식으로 작성
    public FlagData flagSet; //플래그 설정을 위한 플래그 명을 작성하는 노드, <"플래그명", boolean>형식으로 작성
    public List<FlagData> flagCheck; //본문을 내보내기 위해 플래그를 확인하는 노드, <"플래그명", boolean>형식으로 작성
                                      //(리스트 형식으로 복수 체크 가능)
}

[System.Serializable]
//아이템 제어를 위한 데이터 모델
public class ItemData
{
    public string ItemCode; //아이템 코드명
    public int ItemAmount; //아이템 갯수
}

[System.Serializable]
//플래그 제어를 위한 데이터 모델
public class FlagData
{
    public string flagName; //플래그
    public bool flagState; //플래그 상태
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

public class SectionEventManager : MonoBehaviour
{
    public string jsonFileName = ""; //불러올 Json파일 이름, 외부에서 받아옴
    private string jsonFolderPath = "StoryGameData/SectionData/SectionEvent/TutorialSection"; //Json폴더가 담긴 파일의 경로
    private string imageFolderPath = "StoryGameData/SectionData/SectionImage/TSectionImage"; //게임 삽화가 담긴 파일의 경로
    private Dictionary<string, object> sectionData = new Dictionary<string, object>(); //파싱된 Json데이터
    private ItemDataManager itemDataManager;

    public GameObject choiceButtonPrefab; //버튼 프리팹
    public Transform choiceButtonContainer; //버튼 부모 오브젝트
    private Image sceneImage; //UI에 띄울 이미지 컴포넌트

    //다음으로 버튼튼 y값 수정
    public float customY = -200f;
    //선택지 버튼 변수
    float yOffset = -40f; //버튼 간 세로 간격
    public float startY = -10f; //시작 위치 기준값
    int index = 1;

    //타이핑 변수
    private Text dialogueText; //출력될 텍스트 컴포넌트
    public float delayPerChar = 0.01f; //문장 타이핑 딜레이
    public float delayPerSentence = 0.25f; //문장간 딜레이

    //디버깅용 변수
    TextNode testjson = null;
    public ItemDataNode testItem = null;

    private void Awake()
    {
        //참조 캐싱
        dialogueText = GameObject.Find("ScriptText").GetComponent<Text>();
        sceneImage = GameObject.Find("SceneImage").GetComponent<Image>();
        itemDataManager = GetComponent<ItemDataManager>();

        LoadJson(jsonFileName); //Json파일 로드
    }

    private void Start()
    {
        //Json테스트 출력
        testjson = GetTextNode("Text1");
        Debug.Log(testjson.value[0]);
        StartDialogue("Text1");
    }

    /// <summary>
    /// Json Event 데이터 파일 로드
    /// </summary>
    /// <param name="jsonFileName">Json 파일명(확장자 미포함)</param>
    public void LoadJson(string jsonFileName)
    {
        string filePath = $"{jsonFolderPath}/{jsonFileName}";

        TextAsset jsonFile = Resources.Load<TextAsset>(filePath); //Json 파일 로드
        if (jsonFile == null)
        {
            Debug.LogError($"[SectionEventManager] 파일을 찾을 수 없음: {jsonFileName}.json");
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

            //본문과 선택지 여부에 따라 저장
            if (key.StartsWith("Menu"))
            {
                MenuNode menu = nodeObj.ToObject<MenuNode>();
                sectionData[key] = menu; //키가 존재하지 않으면 동적 추가
            }
            else if (key.StartsWith("Text") || key.StartsWith("Result"))
            {
                // action 빼고 복제본 만들기
                JObject nodeClone = (JObject)nodeObj.DeepClone();
                nodeClone.Remove("action");

                TextNode text = nodeClone.ToObject<TextNode>();

                //action 수동 파싱
                if (nodeObj.TryGetValue("action", out var actionToken))
                    text.action = ParseActionNode((JObject)actionToken);
                
                sectionData[key] = text; //키가 존재하지 않으면 동적 추가
            }
            else
            {
                Debug.LogWarning($"[SectionEventManager] 인식할 수 없는 노드: {key}");
            }
        }
        Debug.Log("Reading File : " + jsonFileName + ".json"); //파일 로드 확인 로그
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
    /// Action태그의 사용 편의를 위한 수동 parser 틀
    /// 아이템 처리와 flag처리를 간편하게 쓰기 위해 사용됨
    /// </summary>
    /// <param name="actionObj">처리가 필요한 action노드</param>
    /// <returns>parsing 완료된 action노드</returns>
    private ActionNode ParseActionNode(JObject actionObj)
    {
        ActionNode action = new ActionNode();

        // image 처리
        if (actionObj.TryGetValue("image", out var imgToken))
            action.image = imgToken.ToString();

        // get 아이템 처리
        if (actionObj.TryGetValue("get", out var getToken))
            action.get = ParseItemData(getToken);

        // lost 아이템 처리
        if (actionObj.TryGetValue("lost", out var lostToken))
            action.lost = ParseItemData(lostToken);

        // flagSet 처리
        if (actionObj.TryGetValue("flagSet", out var flagSetToken))
            action.flagSet = flagSetToken.ToObject<FlagData>();

        // flagCheck 처리
        if (actionObj.TryGetValue("flagCheck", out var flagCheckToken))
            action.flagCheck = flagCheckToken.ToObject<List<FlagData>>();

        return action;
    }

    /// <summary>
    /// 아이템 처리 부분의 parser (ParseActionNode함수에 사용)
    /// </summary>
    /// <param name="token">아이템 처리 action 정보</param>
    private ItemData ParseItemData(JToken token)
    {
        if (token == null)
            return null;

        //배열일 경우 객체로 바꿔 return
        if (token is JArray arr)
        {
            return new ItemData
            {
                ItemCode = arr[0].ToString(),
                ItemAmount = arr[1].ToObject<int>()
            };
        }
        else
        {
            //객체일 경우 그대로 return
            return token.ToObject<ItemData>();
        }
    }

    /// <summary>
    /// 스크립트 타이핑 효과 코루틴
    /// </summary>
    /// <param name="fullText">타이핑 효과를 넣고 싶은 텍스트 전문</param>
    /// <param name="onComplete">타이핑 완료시 실행할 이벤트</param>
    IEnumerator TypeTextCoroutine(string fullText,
    System.Action onComplete = null)
    {
        dialogueText.text = ""; //타이핑 첫 시작시 내용 초기화
        int typingLength = fullText.GetTypingLength(); //문장 길이 측정

        for (int j = 0; j <= typingLength; j++) //타이핑 효과
        {
            dialogueText.text = fullText.Typing(j);
            if (!string.IsNullOrEmpty(dialogueText.text))
            {
                if (dialogueText.text[dialogueText.text.Length - 1] == '\n')
                {
                    yield return new WaitForSeconds(delayPerSentence); //문장 끝일때 딜레이 추가
                }
            }
            yield return new WaitForSeconds(delayPerChar); //타이핑 딜레이
        }
        onComplete?.Invoke();
    }

    /// <summary>
    /// 액션 노드 처리 메소드
    /// </summary>
    /// <param name="actions">대상 액션 노드</param>
    void HandleNodeActions(ActionNode actions)
    {
        if (actions == null) return;

        //삽화 변경
        if (actions.image != null && actions.image != "" && actions.image is string imageName)
        {
            Sprite newSprite = LoadSceneSprite(imageName);
            if (newSprite != null)
            {
                sceneImage.sprite = newSprite;
                Debug.Log($"이미지 변경 {imageName}");
            }
            else
            {
                Debug.LogWarning($"[SectionEventManager] 이미지 로드 실패 {imageName}");
            }
        }

        //아이템 획득
        if (actions.get != null && actions.get.ItemCode!= "" && actions.get.ItemAmount != 0 && 
            actions.get.ItemCode is string ItemCode && actions.get.ItemAmount is int ItemAmount)
        {
            testItem = itemDataManager.GetItemByCode(ItemCode);

            //테스트 출력
            Debug.Log($"\'{testItem.name}\'아이템을 {ItemAmount}개 획득했습니다.");
        }

        //아이템 유실

        //플래그 설정

        //플래그 체크
    }

    /// <summary>
    /// 삽화 이미지 로더 메소드
    /// </summary>
    /// <param name="imageName">확장자를 제외한 삽화 이미지 파일명</param>
    /// <returns>삽화 이미지 렌더링 실행</returns>
    Sprite LoadSceneSprite(string imageName)
    {
        string imagePath = $"{imageFolderPath}/{imageName}";
        Debug.Log(imagePath);

        Texture2D texture = Resources.Load<Texture2D>(imagePath);
        if (texture == null) return null;

        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    //-------------------------------------------------------------------------------
    // ** 테스트 출력 부분 **
    void StartDialogue(string nodeKey)
    {
        if (sectionData.TryGetValue(nodeKey, out object node))
        {
            if (node is TextNode textNode)
            {
                DisplayTextNode(textNode);
            }
            else if (node is MenuNode menuNode)
            {
                DisplayMenuNode(menuNode);
            }
            else
            {
                Debug.LogError($"[SectionEventManager] 알 수 없는 노드 타입: {node.GetType()}");
            }
        }
        else
        {
            Debug.LogError($"[SectionEventManager] {nodeKey}노드를 찾을 수 없습니다.");
        }
    }

    void DisplayTextNode(TextNode node)
    {
        StopAllCoroutines();

        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }

        HandleNodeActions(node.action); //이미지 로드

        if (!string.IsNullOrEmpty(node.next))
        {
            StartCoroutine(TypeTextCoroutine(string.Join("\n", node.value), () =>
            {
                GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                Button button = buttonObj.GetComponent<Button>();
                Text buttonText = buttonObj.GetComponentInChildren<Text>();
                buttonText.text = "다음으로";
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => StartDialogue(node.next));

                RectTransform rect = buttonObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, customY);
            }));
        }
        else
        {
            StartCoroutine(TypeTextCoroutine(string.Join("\n", node.value)));
        }
    }
    void DisplayMenuNode(MenuNode node)
    {
        //텍스트 비우기
        dialogueText.text = "";

        //기존 버튼 제거
        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (MenuOption option in node.menuOption)
        {
            //각 선택지에 대해 버튼 생성
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            buttonObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, startY + index * yOffset);

            Button button = buttonObj.GetComponent<Button>();
            Text buttonText = buttonObj.GetComponentInChildren<Text>();

            buttonText.text = option.label;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                Debug.Log($"선택됨: {option.id}");

                //선택지 클릭 후 기존 버튼 제거
                foreach (Transform child in choiceButtonContainer)
                {
                    Destroy(child.gameObject);
                }

                //액션 처리
                if (option.action != null)
                {
                    Debug.Log($"[Action 실행]");
                    HandleNodeActions(option.action);
                }

                //선택지 텍스트 출력
                if (!string.IsNullOrEmpty(option.next))
                {
                    StopAllCoroutines();
                    StartDialogue(option.next);
                }
                else
                {
                    Debug.Log("MenuNode의 next 값이 없습니다. 종료 또는 대기 처리 필요.");
                }
            });
            index++;
        }
    }
    //-------------------------------------------------------------------------------
}
