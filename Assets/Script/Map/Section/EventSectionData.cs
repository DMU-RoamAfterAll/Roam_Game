using UnityEngine;
using System.Collections.Generic;

public class EventSectionData : MonoBehaviour {
    public string id; //Section의 고유 id값
    public char rate; //Section의 등급 
    public string eventType; //Section의 이벤트 종류
    public bool isVisited; //Player가 와 본 Section인지
    public bool isPlayerOn; //Player가 현재 이 Section에 위치하고 있는지
    public bool isCanMove; //Player가 이 Section으로 이동할 수 있는지
    public Vector2 sectionPosition; //이 Section의 위치
}