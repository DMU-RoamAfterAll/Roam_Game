using UnityEngine;
using System.Collections.Generic;
using TMPro;
public class SectionData : MonoBehaviour {
    public List<LinkSection> linkSections;
    public string id; //Section의 고유 id값
    public char rate; //Section의 등급 
    public string eventType; //Section의 이벤트 종류
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
    public void SetPlayerOnSection() {
        isPlayerOn = !isPlayerOn;

        if(linkSections != null) {
            foreach(var link in linkSections) {
                Debug.Log("Is It LinkSections");
                link.CreateVirtualSection(); //가상의 점 생성
            }
        }
    }

    ///플레이어가 처음 Section에 도달했을 때 시야 프리팹 생성
    public void SetOption() {
        if(!isVisited) {
            isVisited = true;
        }
        else {
            return;
        }
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