using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

[RequireComponent(typeof(LayoutElement))]
public class ExpandablePanel : MonoBehaviour
{
    [Header("Refs")]
    public Button toggleButton;           // 검은 십자 버튼
    public RectTransform bodyMask;        // 클리핑용
    public RectTransform bodyContent;     // VerticalLayout+ContentSizeFitter가 붙은 Body

    [Header("Anim")]
    public float duration = 0.25f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool startOpen = false;

    public bool IsOpen { get; private set; }
    public event Action<ExpandablePanel, bool> OnExpandedChanged;

    LayoutElement _layout;
    float _headerHeight;
    Coroutine _co;

    void Awake()
    {
        _layout = GetComponent<LayoutElement>();
        if (toggleButton == null) Debug.LogWarning($"{name}: toggleButton not set.");
        if (bodyMask == null) Debug.LogWarning($"{name}: bodyMask not set.");
        if (bodyContent == null) Debug.LogWarning($"{name}: bodyContent not set.");
    }

    void Start()
    {
        // 헤더 높이는 버튼 부모(=Header)의 높이를 사용
        var header = (RectTransform)toggleButton.transform.parent;
        _headerHeight = header.rect.height;

        // 초기 상태
        IsOpen = startOpen;
        LayoutRebuilder.ForceRebuildLayoutImmediate(bodyContent);
        float contentH = bodyContent.sizeDelta.y;
        float bodyH = IsOpen ? contentH : 0f;

        bodyMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bodyH);
        _layout.preferredHeight = _headerHeight + bodyH;

        toggleButton.onClick.AddListener(() => SetOpen(!IsOpen));
    }

    public void SetOpen(bool open)
    {
        if (IsOpen == open) return;
        IsOpen = open;

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(AnimateTo(IsOpen));

        OnExpandedChanged?.Invoke(this, IsOpen);
    }

    IEnumerator AnimateTo(bool open)
    {
        // 최신 컨텐츠 높이 계산(동적 아이템 반영)
        LayoutRebuilder.ForceRebuildLayoutImmediate(bodyContent);
        float contentH = Mathf.Max(0f, bodyContent.sizeDelta.y);

        float startBody = bodyMask.rect.height;
        float targetBody = open ? contentH : 0f;

        float startTotal = _layout.preferredHeight;
        float targetTotal = _headerHeight + targetBody;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.001f, duration);
            float k = ease.Evaluate(Mathf.Clamp01(t));

            float b = Mathf.Lerp(startBody, targetBody, k);
            bodyMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, b);
            _layout.preferredHeight = Mathf.Lerp(startTotal, targetTotal, k);

            yield return null;
        }

        bodyMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetBody);
        _layout.preferredHeight = targetTotal;
        _co = null;
    }
}
