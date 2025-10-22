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
    /// <param name="node">출력할 스크립트 노드</param>
    /// <param name="buttonText">버튼 라벨</param>
    /// <param name="HandleNextNode">버튼 클릭 콜백 함수</param>
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
    /// 선택지 출력 메소드
    /// </summary>
    /// <param name="node">출력할 선택지 노드</param>
    /// <param name="HandleMenuSelect">선택지를 고른 뒤 실행할 콜백함수</param>
    public void DisplayMenuButton(MenuOption option, bool isInteract, UnityAction HandleMenuSelect)
    {
        //각 선택지에 대해 버튼 생성
        CreateButtons(option.label, isInteract, HandleMenuSelect);
    }

    /// <summary>
    /// 전투 인트로 출력 및 전투 삽화 변경 메소드
    /// </summary>
    /// <param name="battleIntro">출력할 전투 인트로 리스트</param>
    /// <param name="battleImage">출력할 전투 삽화명</param>
    /// <param name="onBattleStart">인트로 출력이 끝난 후 실행할 전투 콜백함수</param>
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
    /// <typeparam name="T">선택지 요소의 자료형</typeparam>
    /// <param name="options">선택지로 출력할 옵션 목록(리스트 형식)</param>
    /// <param name="labelSelector">문자열 추출을 위한 함수</param>
    /// <param name="onSelected">선택 완료시 실행할 콜백함수</param>
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
    /// <param name="imageName">확장자를 제외한 삽화 이미지 파일명 (SectionImage이후 경로 포함)</param>
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

    /// <summary>
    /// 버튼 UI 생성 메소드
    /// </summary>
    /// <param name="text">버튼에 표시 글자</param>
    /// <param name="onClick">버튼 onClick 함수</param>
    /// <returns>생성한 버튼</returns>
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

    /// <summary>
    /// 버튼 UI 생성 메소드 : 오버로드 (메뉴)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="text">버튼에 표시 글자</param>
    /// <param name="actionResult"></param>
    /// <param name="onClick">버튼 onClick 함수</param>
    /// <returns>생성한 버튼</returns>
    private Button CreateButtons(string text, bool isInteract, UnityAction onClick)
    {
        Button tempBtn = ActivateButton(); //버튼 활성화
        SetupButton(tempBtn, text); //버튼 세팅
        if (isInteract) //버튼 활성화 체크
        {
            tempBtn.interactable = true;
            tempBtn.onClick.RemoveAllListeners();
            tempBtn.onClick.AddListener(() =>
            {
                ClearButtons(); //클릭 시 버튼 비활
                onClick?.Invoke(); //onClick 실행
            });

            return tempBtn;
        }
        tempBtn.interactable = isInteract;
        return tempBtn;
    }

    /// <summary>
    /// 버튼 활성화 메소드
    /// </summary>
    /// <returns>활성화된 버튼</returns>
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

    /// <summary>
    /// 버튼 오브젝트 설정 메소드
    /// </summary>
    /// <param name="btn">설정이 필요한 버튼</param>
    /// <param name="text">버튼 라벨값</param>
    private void SetupButton(Button btn, string text)
    {
        var buttonText = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null) buttonText.text = text; //버튼 라벨 설정
    }

    /// <summary>
    /// 전체 버튼 UI 정리 메소드
    /// </summary>
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
    
    /// <summary>
    /// 스크립트 타이핑 코루틴 시작 메소드
    /// </summary>
    /// <param name="fullText">타이핑 효과를 넣고 싶은 텍스트 전문</param>
    /// <param name="onComplete">타이핑 완료시 실행할 이벤트</param>
    private void StartTyping(string fullText, System.Action onComplete = null, bool append = false)
    {
        //이전 코루틴이 실행 중이면 중단
        StopTyping();

        string baseText = string.Empty;
        string appendText = fullText;

        if (append && dialogueText.text != string.Empty) { baseText = dialogueText.text + '\n'; }

        //새 코루틴 실행
        typingCoroutine = StartCoroutine(TypeTextCoroutine(baseText, appendText, () =>
        {
            typingCoroutine = null; //타이핑이 끝나면 코루틴 정리
            isTyping = false; //타이핑 끝남 처리
            onComplete?.Invoke(); //후처리 함수 실행
        }));
    }

    /// <summary>
    /// 스크립트 타이핑 코루틴 중지 메소드
    /// </summary>
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

    /// <summary>
    /// 스크립트 타이핑 효과 코루틴
    /// </summary>
    /// <param name="fullText">타이핑 효과를 넣고 싶은 텍스트 전문</param>
    /// <param name="onComplete">타이핑 완료시 실행할 이벤트</param>
    private IEnumerator TypeTextCoroutine(string baseText, string appendText, System.Action onComplete = null)
    {
        isTyping = true; //타이핑 시작
        skipRequested = false; //타이핑 스킵 요청 초기화

        int typingLength = appendText.GetTypingLength(); //문장 길이 측정

        for (int i = 0; i <= typingLength; i++) //타이핑 효과
        {
            if (skipRequested) //스킵 요청 시 즉시 전체 텍스트 출력 후 종료
            {
                dialogueText.text = baseText + appendText;
                break;
            }

            dialogueText.text = baseText + appendText.Typing(i); //타이핑 반복문 동안 기존 텍스트 내용은 유지
            if (!string.IsNullOrEmpty(dialogueText.text)
                && dialogueText.text[dialogueText.text.Length - 1] == '\n')
            {
                yield return new WaitForSeconds(delayPerSentence); //문장 끝일때 딜레이 추가
            }
            yield return new WaitForSeconds(delayPerChar); //타이핑 딜레이
        }
        dialogueText.text = baseText + appendText; //반복문을 빠져나올 시 텍스트 전체 표시
        onComplete?.Invoke();
    }
}
