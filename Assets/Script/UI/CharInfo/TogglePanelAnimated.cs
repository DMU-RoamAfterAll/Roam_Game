using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutElement))]
public class TogglePanelAnimated : MonoBehaviour
{
    public Button toggleButton;
    public RectTransform header;         // 버튼 포함 헤더
    public RectTransform bodyMask;       // Pivot Y=1, Top-Stretch
    public RectTransform bodyContent;    // VLG + CSF, Pivot Y=1, Top-Stretch

    public Image buttonImage;
    public Sprite addSprite;
    public Sprite minusSprite;

    public bool startOpen = false;
    public float duration = 0.2f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // flicker 방지: 가능하면 false 유지 권장
    public bool deactivateOnClose = false;

    public bool IsOpen { get; private set; }

    LayoutElement _layout;
    float _headerHeight;
    Coroutine _co;

    void Awake()
    {
        _layout = GetComponent<LayoutElement>();
        if (!header && toggleButton) header = toggleButton.transform.parent as RectTransform;
        if (toggleButton) toggleButton.onClick.AddListener(() => SetOpen(!IsOpen));
    }

    void Start()
    {
        _headerHeight = header ? header.rect.height : 0f;

        // 시작 상태 적용 (startOpen=false일 때도 bodyContent는 활성 유지 권장)
        if (deactivateOnClose == false && bodyContent && !bodyContent.gameObject.activeSelf)
            bodyContent.gameObject.SetActive(true);

        StartCoroutine(InitCo());
    }

    IEnumerator InitCo()
    {
        yield return null; // 첫 프레임 레이아웃 계산 기다림
        float contentH = GetPreferredHeight(bodyContent);
        IsOpen = startOpen;
        float bodyH = IsOpen ? contentH : 0f;
        SetBodyHeight(bodyH);
        SetPanelHeight(_headerHeight + bodyH);
        UpdateIcon();

        if (!IsOpen && deactivateOnClose)
            bodyContent.gameObject.SetActive(false);
    }

    public void SetOpen(bool open)
    {
        if (IsOpen == open) return;
        IsOpen = open;
        UpdateIcon();

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(AnimateCo());
    }

    IEnumerator AnimateCo()
    {
        // 열기 직전: 비활성화 사용 시 먼저 켜고 1프레임 대기
        if (IsOpen && deactivateOnClose && bodyContent && !bodyContent.gameObject.activeSelf)
        {
            bodyContent.gameObject.SetActive(true);
            yield return null; // 켠 뒤 레이아웃 계산을 한 프레임 기다림
        }

        // 항상 한 프레임 대기 후 최신 PreferredHeight를 얻음
        yield return null;

        float contentH = GetPreferredHeight(bodyContent);
        float fromBody = bodyMask ? bodyMask.rect.height : 0f;
        float toBody = IsOpen ? contentH : 0f;

        float fromTotal = _layout.preferredHeight;
        float toTotal = _headerHeight + toBody;

        float t = 0f;
        float dur = Mathf.Max(0.001f, duration);

        while (t < 1f)
        {
            t += Time.deltaTime / dur; // 필요시 unscaledDeltaTime 사용
            float k = ease.Evaluate(Mathf.Clamp01(t));

            float h = Mathf.Lerp(fromBody, toBody, k);
            SetBodyHeight(h);
            SetPanelHeight(Mathf.Lerp(fromTotal, toTotal, k));

            yield return null;
        }

        SetBodyHeight(toBody);
        SetPanelHeight(toTotal);

        if (!IsOpen && deactivateOnClose && bodyContent)
            bodyContent.gameObject.SetActive(false);

        _co = null;
    }

    // ----- helpers -----
    static float GetPreferredHeight(RectTransform rt)
    {
        if (!rt) return 0f;
        // LayoutUtility는 VLG/CSF와 잘 맞음. rect.height가 0 나오는 타이밍 문제 해결.
        float h = LayoutUtility.GetPreferredHeight(rt);
        if (h <= 0.01f) h = Mathf.Max(rt.rect.height, rt.sizeDelta.y);
        return Mathf.Max(0f, h);
    }

    void SetBodyHeight(float h)
    {
        if (!bodyMask) return;
        bodyMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(0f, h));
    }

    void SetPanelHeight(float h)
    {
        _layout.preferredHeight = Mathf.Max(0f, h);
    }

    void UpdateIcon()
    {
        if (buttonImage)
            buttonImage.sprite = IsOpen ? minusSprite : addSprite;
    }
}
