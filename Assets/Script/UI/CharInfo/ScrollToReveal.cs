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
        yield return null; // �� ������ ���(���̾ƿ� ���� ��)

        var viewport = scrollRect.viewport;
        var content = scrollRect.content;

        // Ÿ���� ��ǥ�� Viewport ���� �������� ��ȯ
        Vector3[] vpCorners = new Vector3[4];
        Vector3[] tgCorners = new Vector3[4];
        viewport.GetWorldCorners(vpCorners);
        target.GetWorldCorners(tgCorners);

        float vpTop = vpCorners[2].y;   // ���
        float vpBot = vpCorners[0].y;   // �ϴ�
        float tgTop = tgCorners[2].y;
        float tgBot = tgCorners[0].y;

        float delta = 0f;
        if (tgTop > vpTop) delta = tgTop - vpTop;      // ���� ���� �� ���� ��ũ��
        else if (tgBot < vpBot) delta = tgBot - vpBot;      // �Ʒ��� ���� �� �Ʒ��� ��ũ��
        else yield break; // �̹� ����

        // delta(����Y)�� content local�� ȯ�� �� ���� ������ ����
        float contentHeight = content.rect.height - viewport.rect.height;
        if (contentHeight <= 1f) yield break;

        // ���� ��ũ�� ���� ��ȭ�� ����(���� ���)
        float ratio = Mathf.Clamp01(Mathf.Abs(delta) / viewport.rect.height);
        float dir = Mathf.Sign(delta);
        float start = scrollRect.verticalNormalizedPosition;
        float targetPos = Mathf.Clamp01(start + (-dir) * ratio); // Unity ����: ���� �ø����� �� ����

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
