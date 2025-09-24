using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScrollToReveal : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float scrollDuration = 0.2f;

    public void ScrollTo(RectTransform target)
    {
        StartCoroutine(ScrollIntoView(target));
    }

    private IEnumerator ScrollIntoView(RectTransform target)
    {
        yield return null; // Layout 갱신 대기

        var viewport = scrollRect.viewport;
        var content = scrollRect.content;

        Vector3[] vpCorners = new Vector3[4];
        Vector3[] tgCorners = new Vector3[4];
        viewport.GetWorldCorners(vpCorners);
        target.GetWorldCorners(tgCorners);

        float vpBot = vpCorners[0].y;
        float tgBot = tgCorners[0].y;

        float delta = tgBot - vpBot;

        // 콘텐츠 전체 높이 - 뷰포트 높이
        float contentHeight = content.rect.height - viewport.rect.height;
        if (contentHeight <= 1f) yield break;

        float ratio = Mathf.Clamp01(Mathf.Abs(delta) / viewport.rect.height);
        float start = scrollRect.verticalNormalizedPosition;
        float targetPos = Mathf.Clamp01(start - ratio);

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
