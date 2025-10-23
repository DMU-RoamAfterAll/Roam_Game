using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollbarFader : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup scrollbarGroup;
    public ScrollRect scrollRect;

    [Header("Settings")]
    public float visibleAlpha = 0.5f;
    public float hideDelay = 1f; 

    private Coroutine hideCoroutine;
    private float previousPos;

    void Start()
    {
        if (scrollbarGroup != null)
            scrollbarGroup.alpha = 0f;

        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        previousPos = scrollRect.verticalNormalizedPosition;

        scrollRect.onValueChanged.AddListener(OnScroll);
    }

    void OnScroll(Vector2 _)
    {
        float currentPos = scrollRect.verticalNormalizedPosition;

        if (currentPos < previousPos)
        {
            ShowScrollbar();
        }

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideScrollbarAfterDelay());

        previousPos = currentPos;
    }

    private void ShowScrollbar()
    {
        if (scrollbarGroup != null)
            scrollbarGroup.alpha = visibleAlpha;
    }

    private IEnumerator HideScrollbarAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);
        if (scrollbarGroup != null)
            scrollbarGroup.alpha = 0f;
    }
}
