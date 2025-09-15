using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

[RequireComponent(typeof(LayoutElement))]
public class ExpandablePanel : MonoBehaviour
{
    [Header("Refs")]
    public Button toggleButton;           // ���� ���� ��ư
    public RectTransform bodyMask;        // Ŭ���ο�
    public RectTransform bodyContent;     // VerticalLayout+ContentSizeFitter�� ���� Body

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
        // ��� ���̴� ��ư �θ�(=Header)�� ���̸� ���
        var header = (RectTransform)toggleButton.transform.parent;
        _headerHeight = header.rect.height;

        // �ʱ� ����
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
        // �ֽ� ������ ���� ���(���� ������ �ݿ�)
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
