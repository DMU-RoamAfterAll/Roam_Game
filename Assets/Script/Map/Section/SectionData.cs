using UnityEngine;
using System.Collections.Generic;

public class SectionData : MonoBehaviour {
    public GameObject sightObjects;
    public GameObject sightSectionPrefab;

    public List<LinkSection> linkSections;

    public string id;
    public char rate;
    public string eventType;
    public bool isVisited;
    public bool isCleared;
    public bool isPlayerOn;
    public bool isCanMove;
    public Vector2 sectionPosition;

    public int stepCost;

    void Start() {
        sightObjects = GameDataManager.Data.sightObjects;
        sightSectionPrefab = GameDataManager.Data.sightSectionPrefab;
        stepCost = 100;
    }

    public void SetPlayerOnSection() {
        isPlayerOn = !isPlayerOn;

        if(linkSections != null) {
            foreach(var link in linkSections) {
                Debug.Log("Is It LinkSections");
                link.AdjustPosition();
            }
        }
    }

    public void SetSight() {
        if(!isVisited) {
            isVisited = true;
        }
        else {
            return;
        }

        if(isVisited) {
            GameObject go = Instantiate(sightSectionPrefab, this.transform.position, Quaternion.identity);
            go.transform.SetParent(sightObjects.transform);
        }
    }
}