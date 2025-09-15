using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class ScrollToReveal : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float scrollDuration = 0.2f;

    ExpandablePanel[] _panels;

    void Awake()
    {
        if (scrollRect == null) Debug.LogWarning($"{name}: ScrollRect not set.");
        _panels = GetComponentsInChildren<ExpandablePanel>(includeInactive: true);
        foreach (var p in _panels)
            p.OnExpandedChanged += OnExpanded;
    }

    void OnDestroy()
    {
        if (_panels == null) return;
        foreach (var p in _panels)
            p.OnExpandedChanged -= OnExpanded;
    }

    void OnExpanded(ExpandablePanel panel, bool open)
    {
        if (!open || scrollRect == null) return;
        StartCoroutine(ScrollIntoView(panel.GetComponent<RectTransform>()));
    }

    IEnumerator ScrollIntoView(RectTransform target)
    {
        yield return null; // 한 프레임 대기(레이아웃 갱신 후)

        var viewport = scrollRect.viewport;
        var content = scrollRect.content;

        // 타겟의 좌표를 Viewport 공간 기준으로 변환
        Vector3[] vpCorners = new Vector3[4];
        Vector3[] tgCorners = new Vector3[4];
        viewport.GetWorldCorners(vpCorners);
        target.GetWorldCorners(tgCorners);

        float vpTop = vpCorners[2].y;   // 상단
        float vpBot = vpCorners[0].y;   // 하단
        float tgTop = tgCorners[2].y;
        float tgBot = tgCorners[0].y;

        float delta = 0f;
        if (tgTop > vpTop) delta = tgTop - vpTop;      // 위로 나감 → 위로 스크롤
        else if (tgBot < vpBot) delta = tgBot - vpBot;      // 아래로 나감 → 아래로 스크롤
        else yield break; // 이미 보임

        // delta(월드Y)를 content local로 환산 → 대충 비율로 보정
        float contentHeight = content.rect.height - viewport.rect.height;
        if (contentHeight <= 1f) yield break;

        // 대충 스크롤 비율 변화값 추정(간단 계산)
        float ratio = Mathf.Clamp01(Mathf.Abs(delta) / viewport.rect.height);
        float dir = Mathf.Sign(delta);
        float start = scrollRect.verticalNormalizedPosition;
        float targetPos = Mathf.Clamp01(start + (-dir) * ratio); // Unity 기준: 위로 올릴수록 값 증가

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.001f, scrollDuration);
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(start, targetPos, t);
            yield return null;
        }
        scrollRect.verticalNormalizedPosition = targetPos;
    }
}
