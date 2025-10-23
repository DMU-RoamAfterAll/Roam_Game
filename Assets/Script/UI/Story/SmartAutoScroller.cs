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
    [Tooltip("�� �� ����(=���� �ٴ�)�� ���� �ڵ� ��ũ��")]
    [Range(0f, 0.5f)] public float autoScrollThreshold = 0.05f;
    [Tooltip("�ε巴�� ��ũ�� �ð�(��)")]
    public float smoothDuration = 0.15f;

    float _prevContentHeight;
    bool _autoScroll = true;              // ������ ���� �ø��� false, �ٴ� ��ó�� true
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

        // ���� ��ũ�� �����ؼ� �ٴ� ��ó�� �ڵ���ũ�� ��/��
        scrollRect.onValueChanged.AddListener(_ =>
        {
            _autoScroll = (scrollRect.verticalNormalizedPosition <= autoScrollThreshold);
        });

        // ù ������ ���� �� �ٴ�����
        StartCoroutine(ScrollBottomAfterLayout());
        _prevContentHeight = content.rect.height;
    }

    void Update()
    {
        if (!scrollRect || !content) return;

        // ������ ���̰� �þ����� üũ(�޽��� �߰� ����)
        var curH = content.rect.height;
        if (curH > _prevContentHeight)     // ������ �þ��
        {
            _prevContentHeight = curH;
            if (_autoScroll)               // �ٴ� ��ó ���� ���� ���� �ڵ� �̵�
            {
                if (_scrollCo != null) StopCoroutine(_scrollCo);
                _scrollCo = StartCoroutine(SmoothScrollToBottom());
            }
        }
    }

    IEnumerator ScrollBottomAfterLayout()
    {
        // ���̾ƿ� ������ �ݿ� ��
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame ();
        Canvas.ForceUpdateCanvases();

        scrollRect.verticalNormalizedPosition = 0f;  // �� �Ʒ�
        scrollRect.velocity = Vector2.zero;
    }

    IEnumerator SmoothScrollToBottom()
    {
        // ���̾ƿ� �ݿ� ���
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame ();
        Canvas.ForceUpdateCanvases();

        float start = scrollRect.verticalNormalizedPosition;
        float target = 0f; // �Ʒ�
        float t = 0f, dur = Mathf.Max(0.01f, smoothDuration);

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            // �ε巯�� ��¡
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
