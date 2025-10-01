using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutElement))]
public class BodyMaskAutoSizer : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform bodyMask;      // 마스크 박스 (RectMask2D 권장, Pivot Y=1, Top Stretch)
    public RectTransform bodyContent;   // 실제 내용 컨테이너 (VLG + CSF, Pivot Y=1, Top Stretch)
    public ExpandablePanel panel;       // 기존 네 ExpandablePanel (수정 X)

    LayoutElement _layout;
    RectTransform _header;              // 토글 버튼의 부모 = 헤더
    float _headerHeight;

    void Awake()
    {
        _layout = GetComponent<LayoutElement>();
        if (!panel) panel = GetComponent<ExpandablePanel>();
        if (!panel) Debug.LogWarning($"{name}: ExpandablePanel not found.");

        if (!bodyContent && panel && panel.content)
            bodyContent = panel.content.GetComponent<RectTransform>();

        if (panel && panel.toggleButton)
            _header = panel.toggleButton.transform.parent as RectTransform;

        if (_header) _headerHeight = _header.rect.height;
    }

    void OnEnable()
    {
        // 토글 직후 레이아웃이 바뀌므로 다음 프레임에 재계산
        if (panel && panel.toggleButton)
            panel.toggleButton.onClick.AddListener(ReflowNextFrame);

        // 텍스트가 변할 때도 자동 반영
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnAnyTextChanged);

        // 최초 1회 강제 반영
        ReflowNextFrame();
    }

    void OnDisable()
    {
        if (panel && panel.toggleButton)
            panel.toggleButton.onClick.RemoveListener(ReflowNextFrame);

        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnAnyTextChanged);
    }

    void OnAnyTextChanged(Object _)
    {
        // 본문 안 텍스트가 바뀌면 다음 프레임에 반영
        ReflowNextFrame();
    }

    void ReflowNextFrame()
    {
        StopAllCoroutines();
        StartCoroutine(Co_Reflow());
    }

    IEnumerator Co_Reflow()
    {
        // 레이아웃 계산이 끝난 다음 프레임까지 대기
        yield return null;

        if (!bodyMask || !bodyContent) yield break;

        // 본문이 비활성화면 접힘 상태로 간주
        if (!bodyContent.gameObject.activeInHierarchy)
        {
            SetBody(0f);
            yield break;
        }

        // 최신 Preferred 높이 강제 갱신
        LayoutRebuilder.ForceRebuildLayoutImmediate(bodyContent);

        float contentH = Mathf.Max(0f, bodyContent.rect.height);
        SetBody(contentH);
    }

    void SetBody(float bodyHeight)
    {
        bodyMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bodyHeight);

        // 패널 전체 높이(헤더 + 바디)를 LayoutElement로 반영 → 부모 레이아웃이 밀려남
        float total = _headerHeight + bodyHeight;
        _layout.preferredHeight = total;
    }
}
