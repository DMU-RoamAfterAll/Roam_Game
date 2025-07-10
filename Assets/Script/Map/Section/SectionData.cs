using UnityEngine;
using System.Collections.Generic;

public class SectionData : MonoBehaviour {
    public GameObject sightObjects;
    public GameObject sightSectionPrefab;

    public string id;
    public char rate;
    public string eventType;
    public bool isVisited;
    public bool isCleared;
    public bool isPlayerOn;
    public bool isCanMove;
    public Vector2 sectionPosition;

    public List<GameObject> linkSections;

    public int stepCost;

    void Start() {
        sightObjects = GameDataManager.Data.sightObjects;
        stepCost = 100;
    }
}