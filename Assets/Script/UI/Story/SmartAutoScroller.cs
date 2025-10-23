using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[DisallowMultipleComponent]
public class SmartAutoScroller : MonoBehaviour
{
    [Header("Refs")]
    public ScrollRect scrollRect;          // Scroll View
    public RectTransform content;          // scrollRect.content

    [Header("Behavior")]
    [Tooltip("이 값 이하(=거의 바닥)일 때만 자동 스크롤")]
    [Range(0f, 0.5f)] public float autoScrollThreshold = 0.05f;
    [Tooltip("부드럽게 스크롤 시간(초)")]
    public float smoothDuration = 0.15f;

    float _prevContentHeight;
    bool _autoScroll = true;              // 유저가 위로 올리면 false, 바닥 근처면 true
    Coroutine _scrollCo;

    void Reset()
    {
        if (!scrollRect) scrollRect = GetComponent<ScrollRect>();
        if (!content && scrollRect) content = scrollRect.content;
    }

    void Awake()
    {
        if (!scrollRect) scrollRect = GetComponent<ScrollRect>();
        if (!content && scrollRect) content = scrollRect.content;
    }

    void OnEnable()
    {
        if (!scrollRect || !content) return;

        // 유저 스크롤 감지해서 바닥 근처면 자동스크롤 켬/끔
        scrollRect.onValueChanged.AddListener(_ =>
        {
            _autoScroll = (scrollRect.verticalNormalizedPosition <= autoScrollThreshold);
        });

        // 첫 프레임 정렬 후 바닥으로
        StartCoroutine(ScrollBottomAfterLayout());
        _prevContentHeight = content.rect.height;
    }

    void Update()
    {
        if (!scrollRect || !content) return;

        // 콘텐츠 높이가 늘었는지 체크(메시지 추가 감지)
        var curH = content.rect.height;
        if (curH > _prevContentHeight)     // 내용이 늘어났음
        {
            _prevContentHeight = curH;
            if (_autoScroll)               // 바닥 근처 보고 있을 때만 자동 이동
            {
                if (_scrollCo != null) StopCoroutine(_scrollCo);
                _scrollCo = StartCoroutine(SmoothScrollToBottom());
            }
        }
    }

    IEnumerator ScrollBottomAfterLayout()
    {
        // 레이아웃 완전히 반영 후
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame ();
        Canvas.ForceUpdateCanvases();

        scrollRect.verticalNormalizedPosition = 0f;  // 맨 아래
        scrollRect.velocity = Vector2.zero;
    }

    IEnumerator SmoothScrollToBottom()
    {
        // 레이아웃 반영 대기
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame ();
        Canvas.ForceUpdateCanvases();

        float start = scrollRect.verticalNormalizedPosition;
        float target = 0f; // 아래
        float t = 0f, dur = Mathf.Max(0.01f, smoothDuration);

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            // 부드러운 이징
            float e = (t < 0.5f) ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(start, target, e);
            scrollRect.velocity = Vector2.zero;
            yield return null;
        }

        scrollRect.verticalNormalizedPosition = target;
        scrollRect.velocity = Vector2.zero;
        _scrollCo = null;
    }
}
