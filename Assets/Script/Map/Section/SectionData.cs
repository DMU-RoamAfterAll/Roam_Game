using UnityEngine;
using System.Collections.Generic;
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
    public Vector2 sectionPosition; //이 Section의 위치

    #region Section 시각화

    private Color originalColor;  // 원래 색상
    private SpriteRenderer spriteRenderer; // SpriteRenderer 참조

    #endregion

    void Start() {
        #region Section 시각화

        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        #endregion
    }

    ///플레이어가 Section에 있는지 없는지를 판별하는 함수
    public void SetPlayerOn(bool on) {
        if (isPlayerOn == on) return;
        isPlayerOn = on;

        if (linkSections != null) {
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

        if (isCanMove || isVisited) spriteRenderer.color = Color.blue;
        else spriteRenderer.color = originalColor;
    }
    #endregion
}