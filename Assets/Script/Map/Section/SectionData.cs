using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class SectionData : MonoBehaviour {
    public List<LinkSection> linkSections = new List<LinkSection>();
    public string id; //Section의 고유 id값
    public char rate; //Section의 등급
    public string eventType; //Section의 이벤트 종류
    public string content;
    public bool isVisited; //Player가 와 본 Section인지
    public bool isCleared; //Player가 이미 통과한 Section인지
    public bool isPlayerOn; //Player가 현재 이 Section에 위치하고 있는지
    public bool isCanMove; //Player가 이 Section으로 이동할 수 있는지
    public bool isNotSightOn;
    public Vector2 sectionPosition; //이 Section의 위치

    #region Section 시각화

    public GameObject completeObj;
    private Color originalColor;  // 원래 색상
    private SpriteRenderer spriteRenderer; // SpriteRenderer 참조

    #endregion

    void Start() {
        #region Section 시각화

        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        SetPinImage();
        #endregion
    }

    void OnEnable() {
        UpdateSectionColor();
        BlinkSection();
    }

    ///플레이어가 Section에 있는지 없는지를 판별하는 함수
    public void SetPlayerOn(bool on) {
        if (isPlayerOn == on) return;
        isPlayerOn = on;
    }

    public void LinkSectionCreate() {
        Debug.Log("1");
        if (linkSections != null) {
            Debug.Log("2");
            foreach (var link in linkSections) {
                link.CreateVirtualSection(); // on일 땐 생성, off일 땐 파괴(내부에서 처리)
            }
        }
    }

    // (원래 메서드는 호환용으로 남겨도 OK)
    public void SetPlayerOnSection() {
        SetPlayerOn(!isPlayerOn);
    }

    public void SetOption() {
        if(!isVisited) {
            isVisited = true;
            LightObj();
            SaveLoadManager.Instance.AddVisitedSectionIds(id);
        }
        else {
            return;
        }
    }

    public void LightObj() {
        var parent = this.transform;
        var prefab = MapSceneDataManager.mapData.lightHolePrefab;

        var go = Instantiate(prefab, parent, false);

        var t = go.transform;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
    }


    #region Section 시각화
    /// isCanMove 상태에 따라 색상을 업데이트
    public void UpdateSectionColor() {
        if (spriteRenderer == null) return;

        if (isCleared)
            spriteRenderer.color = Color.green;
        else
            spriteRenderer.color = Color.white;
    }

    private string resourcePath;
    public void SetPinImage() {
        resourcePath = "ImgObj/Pin/";
        switch(eventType) {
            case "검은 숲" : 
                resourcePath += "ForestPin";
                break;
            case "폐허" : 
                resourcePath += "HollowPin";
                break;
            case "튜토리얼" : 
                resourcePath += "TutorialPin";
                break;

            default : 
                resourcePath += "EndPin";
                break;
        }

        spriteRenderer.sprite = Resources.Load<Sprite>(resourcePath);

    }
    #endregion

    public void BlinkSection() => StartCoroutine(BlinkSection_N());

    public IEnumerator BlinkSection_N()
    {
        float duration = 0.6f;
        float timer = 0f;
        bool fadeOut = true;

        while (!isVisited)
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

    void Update() {
        if(isCleared || isVisited) isCanMove = true;

        UpdateSectionColor();
        UpdateSightByVisitedLightHoles();
    }

    private void UpdateSightByVisitedLightHoles()
    {
        var msdm = MapSceneDataManager.Instance;
        var pc   = msdm != null ? msdm.pc : null;
        if (msdm == null || pc == null) return;

        float r  = pc.maxDistance;
        float r2 = r * r;

        // 이 섹션(나)의 위치
        Vector2 me = transform.position;

        bool visible = false;

        // 1) 일반 섹션들 중 isVisited == true인 섹션(= LightHole가 있는 섹션)과의 거리 체크
        if (msdm.sections != null)
        {
            foreach (var go in msdm.sections)
            {
                if (!go) continue;
                var sd = go.GetComponent<SectionData>();
                if (sd != null && sd.isVisited)
                {
                    Vector2 lightCenter = sd.transform.position; // LightHole은 (0,0,0)에 생성되므로 섹션 중심 == LightHole
                    if ((me - lightCenter).sqrMagnitude <= r2) { visible = true; break; }
                }
            }
        }

        // 2) 메인 섹션들도 동일하게 확인
        if (!visible && msdm.mainSections != null)
        {
            foreach (var go in msdm.mainSections)
            {
                if (!go) continue;
                var sd = go.GetComponent<SectionData>();
                if (sd != null && sd.isVisited)
                {
                    Vector2 lightCenter = sd.transform.position;
                    if ((me - lightCenter).sqrMagnitude <= r2) { visible = true; break; }
                }
            }
        }

        // ✅ 방문(빛) 섹션들과 '직접 거리'가 maxDistance 이내일 때만 보이게
        isNotSightOn = !visible;
    }
}