using UnityEngine;
using KoreanTyper;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class EventDisplayManager : MonoBehaviour
{
    //스트립트
    private SectionEventManager sectionEventManager;
    private EnemyDataManager enemyDataManager;

    //경로
    private string imageFolderPath =
    "StoryGameData/SectionData/SectionImage"; //게임 삽화가 담긴 파일의 경로

    //컨텐츠 오브젝트
    public Transform viewport; //스토리 컨텐츠 부분
    List<EnemyDataNode> currentEnemyList = new List<EnemyDataNode>(); //전투 씬 진입 시 전투할 대상 적 리스트
    public GameObject buttonPrefab; //버튼 프리팹 (인스펙터 접속)
    public Transform buttonPanel; //버튼 부모 오브젝트
    public Image sceneImage; //UI에 띄울 이미지 컴포넌트

    //타이핑 관련
    private Coroutine typingCoroutine; //현재 실행 중인 타이핑 코루틴
    public TextMeshProUGUI dialogueText; //출력될 텍스트 컴포넌트
    public float delayPerChar = 0.01f; //문장 타이핑 딜레이
    public float delayPerSentence = 0.25f; //문장간 딜레이

    private void Awake()
    {
        //참조 캐싱
        sectionEventManager = GetComponent<SectionEventManager>();
        enemyDataManager = GetComponent<EnemyDataManager>();
        viewport = GameObject.Find("Viewport").GetComponent<Transform>();
        sceneImage = viewport.Find("Content/UI_Image/Image").GetComponent<Image>();
        dialogueText = viewport.Find("Content/value").GetComponent<TextMeshProUGUI>();
        buttonPanel = viewport.Find("Content/Panel_Button").GetComponent<Transform>();
    }
    /// <summary>
    /// 본문 출력 메소드
    /// </summary>
    /// <param name="node">출력할 본문 노드</param>
    public void DisplayTextNode(TextNode node)
    {
        StopAllCoroutines();
        ClearButtons(); //버튼 제거

        sectionEventManager.HandleNodeActions(node.action); //액션 실행

        if (!string.IsNullOrEmpty(node.next)) //본문 출력
        {
            StartCoroutine(TypeTextCoroutine(string.Join("\n", node.value), () =>
            {
                GameObject buttonObj = Instantiate(buttonPrefab, buttonPanel);
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                buttonText.text = "다음으로";
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => sectionEventManager.StartDialogue(node.next));
            }));
        }
        else
        {
            StartCoroutine(TypeTextCoroutine(string.Join("\n", node.value)));
        }
    }

    /// <summary>
    /// 선택지 출력 메소드
    /// </summary>
    /// <param name="node">출력할 선택지 노드</param>
    public void DisplayMenuNode(MenuNode node)
    {
        //텍스트 비우기
        dialogueText.text = "";

        //기존 버튼 제거
        ClearButtons();

        foreach (MenuOption option in node.menuOption)
        {
            //각 선택지에 대해 버튼 생성
            GameObject buttonObj = Instantiate(buttonPrefab, buttonPanel);

            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            buttonText.text = option.label;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                Debug.Log($"선택됨: {option.id}");

                //선택지 클릭 후 기존 버튼 제거
                ClearButtons();

                //액션 처리
                if (option.action != null)
                {
                    Debug.Log($"[Action 실행]");
                    sectionEventManager.HandleNodeActions(option.action);
                }

                //선택지 텍스트 출력
                if (!string.IsNullOrEmpty(option.next))
                {
                    StopAllCoroutines();
                    sectionEventManager.StartDialogue(option.next);
                }
                else
                {
                    Debug.Log($"[{GetType().Name}] MenuNode의 next 값이 없습니다. 종료 또는 대기 처리 필요.");
                }
            });
        }
    }

    /// <summary>
    /// 전투 씬 출력 메소드
    /// </summary>
    /// <param name="node">출력할 전투 노드</param>
    public void DisplayBattleNode(BattleNode node)
    {
        List<string> battleOrder = node.battleOrder;

        if (battleOrder.Count > 1 && battleOrder.Count(item => item == "player") == 1)
        {
            Debug.Log($"배틀 실행 : {string.Join("→", battleOrder)}");

            //현재 적 정보 리스트 불러오기
            string BattleImage = "";
            foreach (string enemyCode in battleOrder)
            {
                if (enemyCode == "player")
                {
                    currentEnemyList.Add(null); //플레이어 턴에선 null값을 삽입
                }
                else
                {
                    EnemyDataNode enemyDict = enemyDataManager.GetEnemyByCode(enemyCode);
                    if (enemyDict != null)
                    {
                        currentEnemyList.Add(enemyDict);
                        BattleImage = enemyDict.image;
                    }
                    else
                    {
                        Debug.LogError($"[{GetType().Name}] 잘못된 적 코드값 : {enemyCode}");
                    }
                }
            }
            //텍스트 비우기
            dialogueText.text = "";

            //기존 버튼 제거
            ClearButtons();

            LoadSceneSprite(BattleImage); //전투 이미지 출력
            dialogueText.text = node.battleIntro[0];

            //전투 인트로 출력
            StartCoroutine(TypeTextCoroutine(string.Join("\n", node.battleIntro)));

            //턴제 전투 돌입
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] 잘못된 battleOrder : {string.Join("→", battleOrder)}");
        }
    }

    //-------------------------------------------------------------------------------
    // ** 출력 유틸용 함수 **
    //-------------------------------------------------------------------------------

    /// <summary>
    /// 버튼 UI 제거 메소드
    /// </summary>
    public void ClearButtons()
    {
        for (int i = buttonPanel.childCount - 1; i >= 0; i--)
        {
            var t = buttonPanel.GetChild(i);
            var btn = t.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveAllListeners(); //리스너 해제
            Destroy(t.gameObject); //버튼 삭제
        }
    }

    /// <summary>
    /// 삽화 이미지 로더 메소드
    /// </summary>
    /// <param name="imageName">확장자를 제외한 삽화 이미지 파일명 (SectionImage이후 경로 포함)</param>
    public void LoadSceneSprite(string imageName)
    {
        string imagePath = $"{imageFolderPath}/{imageName}"; //이미지 주소 생성

        Texture2D texture = Resources.Load<Texture2D>(imagePath);
        if (texture == null) return;

        //삽화 이미지 랜더링
        Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

        if (newSprite != null)
        {
            sceneImage.sprite = newSprite; //게임 내 이미지 적용
            Debug.Log($"이미지 변경 {imageName}");
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name}] 이미지 로드 실패 {imageName}");
        }
    }
    
    /// <summary>
    /// 스크립트 타이핑 코루틴 시작 메소드
    /// </summary>
    /// <param name="fullText">타이핑 효과를 넣고 싶은 텍스트 전문</param>
    /// <param name="onComplete">타이핑 완료시 실행할 이벤트</param>
    public void StartTyping(string fullText, System.Action onComplete = null)
    {
        //이전 코루틴이 실행 중이면 중단
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        //새 코루틴 실행
        typingCoroutine = StartCoroutine(TypeTextCoroutine(fullText, () =>
        {
            typingCoroutine = null; //타이핑이 끝나면 코루틴 정리
            onComplete?.Invoke(); //후처리 함수 실행
        }));
    }

    /// <summary>
    /// 스크립트 타이핑 코루틴 중지 메소드
    /// </summary>
    public void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
    }

    /// <summary>
    /// 스크립트 타이핑 효과 코루틴
    /// </summary>
    /// <param name="fullText">타이핑 효과를 넣고 싶은 텍스트 전문</param>
    /// <param name="onComplete">타이핑 완료시 실행할 이벤트</param>
    public IEnumerator TypeTextCoroutine(string fullText, System.Action onComplete = null)
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
}
