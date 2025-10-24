using UnityEngine;
using KoreanTyper;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System;

public class EventDisplayManager : MonoBehaviour
{
    //경로
    private string imageFolderPath =
    "StoryGameData/SectionData/SectionImage"; //게임 삽화가 담긴 파일의 경로

    //컨텐츠 오브젝트
    public Transform viewport; //스토리 컨텐츠 부분
    public GameObject buttonPrefab; //버튼 프리팹 (인스펙터 접속)
    public Transform buttonPanel; //버튼 부모 오브젝트
    public Image sceneImage; //UI에 띄울 이미지 컴포넌트
    public string nextText = "다음으로"; //다음 노드로 넘어가는 버튼의 Text값

    //타이핑 관련
    private Coroutine typingCoroutine; //현재 실행 중인 타이핑 코루틴
    public TextMeshProUGUI dialogueText; //출력될 텍스트 컴포넌트
    public float delayPerChar = 0.01f; //문장 타이핑 딜레이
    private float delayPerSentence = 0.5f;  //문장 당 딜레이
    private bool isTyping = false; //타이핑 진행 상황 확인
    private bool skipRequested = false; //타이핑 스킵 상태 확인

    private void Awake()
    {
        //참조 캐싱
        viewport = GameObject.Find("Viewport").GetComponent<Transform>();
        sceneImage = viewport.Find("Content/UI_Image/Image").GetComponent<Image>();
        dialogueText = viewport.Find("Content/value").GetComponent<TextMeshProUGUI>();
        buttonPanel = viewport.Find("Content/Panel_Button").GetComponent<Transform>();
    }

    private void Update()
    {
        if (isTyping && Input.GetMouseButtonDown(0)) //화면 터치시 스킵 요청
        {
            skipRequested = true;
        }
    }

    /// <summary>
    /// 스크립트 출력 메소드
    /// </summary>
    public IEnumerator DisplayScript(List<string> textScript, string buttonText, UnityAction HandleNextNode)
    {
        ClearButtons(); //기존 버튼 제거
        bool clicked = false; //버튼 클릭 트리거

        StartTyping(string.Join("\n", textScript), () => //스크립트 출력
        {
            CreateButtons(buttonText, () => { clicked = true; }); //버튼 생성
        }, true);

        yield return new WaitUntil(() => clicked); //버튼 선택 대기
        
        HandleNextNode?.Invoke();
    }

    /// <summary>
    /// 선택지 버튼 1개 생성 (초기 활성/비활성 지정 가능). 생성된 Button 반환.
    /// </summary>
    public Button DisplayMenuButton(MenuOption option, bool isInteract, UnityAction HandleMenuSelect)
    {
        return CreateButtons(option.label, isInteract, HandleMenuSelect);
    }

    /// <summary>
    /// 전투 인트로 출력 및 전투 삽화 변경 메소드
    /// </summary>
    public void DisplayBattleIntro(List<string> battleIntro, string battleImage, UnityAction onBattleStart)
    {
        ClearButtons(); //기존 버튼 제거
        dialogueText.text = string.Empty; //텍스트 비우기

        LoadSceneSprite("BattleImage/" + battleImage); //전투 이미지 출력
        StartTyping(string.Join("\n", battleIntro), () =>
        {
            //전투 인트로 출력 후 메인 전투 루프 실행
            CreateButtons("전투 시작", onBattleStart);
        }, true);
    }

    /// <summary>
    /// 전투 루프 시, 선택지 메뉴 출력 메소드
    /// </summary>
    public IEnumerator DisplaySelectMenu<T>(
    List<T> options,
    Func<T, string> labelSelector,
    Action<T> onSelected)
    {
        bool picked = false;
        T result = default;

        foreach (var option in options)
        {
            var label = option; //클로저 방지
            CreateButtons(labelSelector(label), () =>
            {
                if (!picked) //중복 선택 방지
                {
                    result = label;
                    picked = true;
                }
            });
        }

        yield return new WaitUntil(() => picked); //선택 대기
        onSelected?.Invoke(result);
    }

    //-------------------------------------------------------------------------------
    // ** 출력 유틸용 함수 **
    //-------------------------------------------------------------------------------

    /// <summary>
    /// 삽화 이미지 로더 메소드
    /// </summary>
    public void LoadSceneSprite(string imageName)
    {
        string imagePath = $"{imageFolderPath}/{imageName}"; //이미지 주소 생성

        Texture2D texture = Resources.Load<Texture2D>(imagePath);
        if (texture == null)
        {
            Debug.LogWarning($"[{GetType().Name}] 이미지 경로 오류 {imagePath}");
            return;
        }

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

        dialogueText.text = string.Empty; //삽화가 바뀌면 내용 리셋
    }

    // --- 버튼 생성 유틸 ---

    private Button CreateButtons(string text, UnityAction onClick)
    {
        Button tempBtn = ActivateButton(); //버튼 활성화
        SetupButton(tempBtn, text); //버튼 세팅
        tempBtn.interactable = true;

        tempBtn.onClick.RemoveAllListeners();
        tempBtn.onClick.AddListener(() =>
        {
            ClearButtons(); //클릭 시 버튼 비활
            onClick?.Invoke(); //onClick 실행
        });

        return tempBtn;
    }

    private Button CreateButtons(string text, bool isInteract, UnityAction onClick)
    {
        Button tempBtn = ActivateButton(); //버튼 활성화
        SetupButton(tempBtn, text); //버튼 세팅

        tempBtn.onClick.RemoveAllListeners();
        if (isInteract)
        {
            tempBtn.interactable = true;
            tempBtn.onClick.AddListener(() =>
            {
                ClearButtons(); //클릭 시 버튼 비활
                onClick?.Invoke(); //onClick 실행
            });
        }
        else
        {
            tempBtn.interactable = false; //일단 잠가두기
            // 콜백은 나중에 외부에서 interactable이 true가 된 뒤에도 정상 동작하도록 등록해 둔다.
            tempBtn.onClick.AddListener(() =>
            {
                if (tempBtn.interactable)
                {
                    ClearButtons();
                    onClick?.Invoke();
                }
            });
        }

        return tempBtn;
    }

    private Button ActivateButton()
    {
        Button tempBtn = null;
        for (int i = 0; i < buttonPanel.childCount; i++) //비활성화인 버튼 찾기
        {
            var child = buttonPanel.GetChild(i);
            if (!child.gameObject.activeSelf)
            {
                tempBtn = child.GetComponent<Button>();
                break; //비활성화 버튼이 존재한다면 저장
            }
        }
        if (tempBtn == null) //비활성화 버튼이 없으면 생성
            tempBtn = Instantiate(buttonPrefab, buttonPanel).GetComponent<Button>();

        tempBtn.gameObject.SetActive(true); //버튼 활성화
        return tempBtn;
    }

    private void SetupButton(Button btn, string text)
    {
        var buttonText = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null) buttonText.text = text; //버튼 라벨 설정
    }

    private void ClearButtons()
    {
        for (int i = buttonPanel.childCount - 1; i >= 0; i--)
        {
            var t = buttonPanel.GetChild(i);
            var btn = t.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveAllListeners(); //리스너 해제
            if (t.gameObject.activeSelf)
                t.gameObject.SetActive(false); //버튼 비활성화
        }
    }
    
    // --- 타이핑 유틸 ---

    private void StartTyping(string fullText, System.Action onComplete = null, bool append = false)
    {
        StopTyping();

        string baseText = string.Empty;
        string appendText = fullText;

        if (append && dialogueText.text != string.Empty) { baseText = dialogueText.text + '\n' + '\n'; }

        typingCoroutine = StartCoroutine(TypeTextCoroutine(baseText, appendText, () =>
        {
            typingCoroutine = null;
            isTyping = false;
            onComplete?.Invoke();
        }));
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isTyping = false;
        skipRequested = false;
    }

    private IEnumerator TypeTextCoroutine(string baseText, string appendText, System.Action onComplete = null)
    {
        isTyping = true;
        skipRequested = false;

        int typingLength = appendText.GetTypingLength();

        for (int i = 0; i <= typingLength; i++)
        {
            if (skipRequested)
            {
                dialogueText.text = baseText + appendText;
                break;
            }

            dialogueText.text = baseText + appendText.Typing(i);
            if (!string.IsNullOrEmpty(dialogueText.text)
                && dialogueText.text[dialogueText.text.Length - 1] == '\n')
            {
                yield return new WaitForSeconds(delayPerSentence);
            }
            yield return new WaitForSeconds(delayPerChar);
        }
        dialogueText.text = baseText + appendText;
        onComplete?.Invoke();
    }
}