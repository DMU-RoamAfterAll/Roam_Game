using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutElement))]
public class BodyMaskAutoSizer : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform bodyMask;      // ����ũ �ڽ� (RectMask2D ����, Pivot Y=1, Top Stretch)
    public RectTransform bodyContent;   // ���� ���� �����̳� (VLG + CSF, Pivot Y=1, Top Stretch)
    public ExpandablePanel panel;       // ���� �� ExpandablePanel (���� X)

    LayoutElement _layout;
    RectTransform _header;              // ��� ��ư�� �θ� = ���
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
        // ��� ���� ���̾ƿ��� �ٲ�Ƿ� ���� �����ӿ� ����
        if (panel && panel.toggleButton)
            panel.toggleButton.onClick.AddListener(ReflowNextFrame);

        // �ؽ�Ʈ�� ���� ���� �ڵ� �ݿ�
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnAnyTextChanged);

        // ���� 1ȸ ���� �ݿ�
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
        // ���� �� �ؽ�Ʈ�� �ٲ�� ���� �����ӿ� �ݿ�
        ReflowNextFrame();
    }

    void ReflowNextFrame()
    {
        StopAllCoroutines();
        StartCoroutine(Co_Reflow());
    }

    IEnumerator Co_Reflow()
    {
        // ���̾ƿ� ����� ���� ���� �����ӱ��� ���
        yield return null;

        if (!bodyMask || !bodyContent) yield break;

        // ������ ��Ȱ��ȭ�� ���� ���·� ����
        if (!bodyContent.gameObject.activeInHierarchy)
        {
            SetBody(0f);
            yield break;
        }

        // �ֽ� Preferred ���� ���� ����
        LayoutRebuilder.ForceRebuildLayoutImmediate(bodyContent);

        float contentH = Mathf.Max(0f, bodyContent.rect.height);
        SetBody(contentH);
    }

    void SetBody(float bodyHeight)
    {
        bodyMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bodyHeight);

        // �г� ��ü ����(��� + �ٵ�)�� LayoutElement�� �ݿ� �� �θ� ���̾ƿ��� �з���
        float total = _headerHeight + bodyHeight;
        _layout.preferredHeight = total;
    }
}
