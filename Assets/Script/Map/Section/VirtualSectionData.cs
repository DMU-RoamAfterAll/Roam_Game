using UnityEngine;
using System.Collections;


public class VirtualSectionData : MonoBehaviour {
    public GameObject truthSection; // 원래 섹션

    SpriteRenderer spriteRenderer;

    void Start() {
        if (truthSection == null) {
            Debug.LogWarning("[VirtualSectionData] truthSection이 설정되지 않았습니다.");
            return;
        }

        SpriteRenderer original = truthSection.GetComponent<SpriteRenderer>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (original != null && spriteRenderer != null) {
            // ✅ 같은 이미지 복사
            spriteRenderer.sprite = original.sprite;

            // ✅ 같은 색상 및 투명도 반영
            spriteRenderer.color = original.color;

            // ✅ 같은 Flip 옵션 적용 (좌우/상하 뒤집힘 포함)
            spriteRenderer.flipX = original.flipX;
            spriteRenderer.flipY = original.flipY;

            // ✅ 같은 Sorting Layer & Order 적용 (겹칠 때 표시 순서 맞추기)
            spriteRenderer.sortingLayerID = original.sortingLayerID;
            spriteRenderer.sortingOrder = original.sortingOrder;
        }
        else {
            Debug.LogWarning("[VirtualSectionData] SpriteRenderer가 누락되었습니다.");
        }

        StartCoroutine(BlinkSection_N());
    }

    void Update() {
        if(truthSection.GetComponent<SectionData>().isCanMove) Destroy(this.gameObject);
    }

    public IEnumerator BlinkSection_N()
    {
        float duration = 0.6f;
        float timer = 0f;
        bool fadeOut = true;

        while (true)
        {
            timer += Time.deltaTime / duration;

            float alpha;
            if (fadeOut)
            {
                alpha = Mathf.Lerp(1f, 0f, timer);
            }
            else
            {
                alpha = Mathf.Lerp(0f, 1f, timer);
            }
            
            if(spriteRenderer) {
                Color c = spriteRenderer.color;
                c.a = alpha;
                spriteRenderer.color = c;
            }

            if (timer >= 1f)
            {
                fadeOut = !fadeOut;
                timer = 0f;
            }

            yield return null;
        };
    }
}