using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections; 

//-------------------------------------------------------
// ** Json 데이터 클래스 구조 **

[System.Serializable]
public class BaseNode //공통 노드
{
    public List<Dictionary<string, object>> action; //아이템 습득 및 손실
    public List<string> text; //선택지 출력 결과
    public string next; //텍스트 이동을 위한 키값
    
}

[System.Serializable]
public class TextNode : BaseNode //본문 노드
{
    public List<string> value; //텍스트 본문
}

[System.Serializable]
public class MenuOption //조사 선택지 노드
{
    public string id; //선택지 키값
    public string label; //선택지에 보여질 텍스트
    public List<string> text; //선택지 클릭 시 출력될 텍스트
    public List<Dictionary<string, object>> action; //행동 정보 (아이템 획득 등)
    public string next; //다음 노드 키
}
[System.Serializable]
public class MenuNode : BaseNode //조사 노드
{
    public List<MenuOption> value; //조사 선택지 노드가 담긴 리스트
}

//-------------------------------------------------------

public class SectionEventManager : MonoBehaviour
{
    public string jsonFileName = ""; //불러올 Json파일 이름
    public string jsonFolderPath = "SectionData\\SectionEvent"; //Json폴더가 담긴 파일의 경로
    private Dictionary<string, object> sectionData = new Dictionary<string, object>();
    //파싱된 Json데이터

    public GameObject choiceButtonPrefab; //버튼 프리팹
    public Transform choiceButtonContainer; //버튼 부모 오브젝트
    public Image sceneImage; //UI에 띄울 이미지 컴포넌트

    //다음으로 버튼튼 y값 수정
    public float customY = -200f; 
    //선택지 버튼 변수
    float yOffset = -40f; //버튼 간 세로 간격
    public float startY = -10f; //시작 위치 기준값
    int index = 1;

    //임시 변수들
    public Text dialogueText;
    TextNode testjson = null;

    void Start()
    {
        LoadJson(jsonFileName); //Json파일 로드

        //Json테스트 출력
        testjson = GetTextNode("Text1");
        Debug.Log(testjson.value[0]);
        StartDialogue("Text1");
    }

    void Update()
    {

    }

    //Json Event 데이터 파일 로드
    public void LoadJson(string jsonFileName)
    {
        string filePath = Path.Combine(jsonFolderPath, jsonFileName);
        filePath = filePath.Replace("\\", "/"); // 경로 구분자 통일

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
            else if (key.StartsWith("Text"))
            {
                TextNode text = nodeObj.ToObject<TextNode>();
                sectionData[key] = text; //키가 존재하지 않으면 동적 추가
            }
            else
            {
                Debug.LogWarning($"[SectionEventManager] 인식할 수 없는 노드: {key}");
            }
        }
        Debug.Log("Reading File : " + jsonFileName + ".json"); //파일 로드 확인 로그
    }

    //텍스트 노드를 꺼내는 메소드
    //사용 예시 : 
    /*
    TextNode node = sectionEventManager.GetTextNode("Text2");
    Debug.Log(node.value[0]);
    */
    public TextNode GetTextNode(string key)
    {
        if (sectionData.TryGetValue(key, out object node) && node is TextNode text)
            return text;
        return null;
    }

    //메뉴 노드를 꺼내는 메소드
    public MenuNode GetMenuNode(string key)
    {
        if (sectionData.TryGetValue(key, out object node) && node is MenuNode menu)
            return menu;
        return null;
    }

    //ㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡ테스트 출력 부분ㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡ

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
                Debug.LogError($"알 수 없는 노드 타입: {node.GetType()}");
            }
        }
        else
        {
            Debug.LogError($"Node '{nodeKey}' not found in dialogue data");
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

                //ui임시 중앙정렬
                RectTransform rect = buttonObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);  // 앵커 중앙
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);       // 피벗 중앙
                rect.anchoredPosition = new Vector2(0f, customY); // X는 중앙, Y는 직접 지정
            }));
        }
        else
        {
            StartCoroutine(TypeTextCoroutine(string.Join("\n", node.value), null));
        }
    }
    void DisplayMenuNode(MenuNode node)
    {
        //공통 텍스트 출력
        dialogueText.text = node.text != null ? string.Join("\n", node.text) : "";

        //기존 버튼 제거
        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }

        HandleNodeActions(node.action); //이미지 로드

        //각 선택지에 대해 버튼 생성
        foreach (MenuOption option in node.value)
        {
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

                //선택지 텍스트 출력
                if (option.text != null && option.text.Count > 0)
                {
                    StopAllCoroutines();
                    StartCoroutine(TypeTextCoroutine(string.Join("\n", option.text), () =>
                    {
                        if (!string.IsNullOrEmpty(node.next))
                        {
                            GameObject nextButton = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                            Button nextBtn = nextButton.GetComponent<Button>();
                            Text nextBtnText = nextBtn.GetComponentInChildren<Text>();
                            nextBtnText.text = "다음으로";
                            nextBtn.onClick.RemoveAllListeners();
                            nextBtn.onClick.AddListener(() => StartDialogue(node.next));

                            //ui임시 중앙정렬
                            RectTransform rect = nextButton.GetComponent<RectTransform>();
                            rect.anchorMin = new Vector2(0.5f, 0.5f);  // 앵커 중앙
                            rect.anchorMax = new Vector2(0.5f, 0.5f);
                            rect.pivot = new Vector2(0.5f, 0.5f);       // 피벗 중앙
                            rect.anchoredPosition = new Vector2(0f, customY); // X는 중앙, Y는 직접 지정
                        }
                        else
                        {
                            Debug.Log("MenuNode의 next 값이 없습니다. 종료 또는 대기 처리 필요.");
                        }
                    }));
                }
                else
                {
                    dialogueText.text = "";
                }

                //액션 처리
                if (option.action != null)
                {
                    foreach (var act in option.action)
                    {
                        foreach (var key in act.Keys)
                        {
                            Debug.Log($"[Action 실행] {key} → {act[key]}");
                        }
                    }
                }
            });
            index++;
        }
    }
    IEnumerator TypeTextCoroutine(string fullText, System.Action onComplete = null, float delay = 0.03f)
    {
        dialogueText.text = "";
        foreach (char c in fullText)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(delay);
        }
        onComplete?.Invoke();
    }

    //action태그 처리 메소드 (현재 Image의 처리만 존재)
    void HandleNodeActions(List<Dictionary<string, object>> actions)
    {
        if (actions == null) return;

        foreach (var act in actions)
        {
            foreach (var key in act.Keys)
            {
                if (key == "Image" && act[key] is string imageName)
                {
                    Sprite newSprite = LoadSceneSprite(imageName);
                    if (newSprite != null)
                    {
                        sceneImage.sprite = newSprite;
                        Debug.Log($"[이미지 변경] {imageName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[이미지 로드 실패] {imageName}");
                    }
                }
            }
        }
    }

    //삽화 이미지 로더 메소드
    Sprite LoadSceneSprite(string imageName)
    {
        string path = $"SectionData/SectionImage/{imageName}";
        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null) return null;

        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

}
