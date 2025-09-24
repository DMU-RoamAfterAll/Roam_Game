using UnityEngine;
using UnityEngine.UI;

public class TogglePanel : MonoBehaviour
{
    public GameObject body;          // 열고 닫을 본문
    public Image buttonImage;        // 버튼에 달린 Image 컴포넌트
    public Sprite addSprite;         // 접힌 상태( + 아이콘 )
    public Sprite minusSprite;       // 펼친 상태( – 아이콘 )

    bool isOpen;

    void Start()
    {
        isOpen = body != null && body.activeSelf;
        UpdateButtonImage();
    }

    public void Toggle()
    {
        if (body == null) return;

        isOpen = !isOpen;
        body.SetActive(isOpen);
        UpdateButtonImage();
    }

    void UpdateButtonImage()
    {
        if (buttonImage != null)
            buttonImage.sprite = isOpen ? addSprite : minusSprite;
    }
}

