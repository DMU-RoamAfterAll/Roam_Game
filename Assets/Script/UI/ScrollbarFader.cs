using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems; // For pointer events
using UnityEngine.UI;           // For ScrollRect and CanvasGroup

public class ScrollbarFader : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("References")]
    public CanvasGroup scrollbarGroup;       // The scrollbar to fade in/out
    public ScrollRect scrollRect;            // The ScrollRect component to track drag events

    [Header("Settings")]
    public float visibleAlpha = 0.5f;        // Opacity when visible
    public float hideDelay = 1f;             // Delay before hiding

    private Coroutine hideCoroutine;

    void Start()
    {
        if (scrollbarGroup != null)
            scrollbarGroup.alpha = 0f;

        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>(); // Try auto-assign
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ShowScrollbar();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideScrollbarAfterDelay());
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
