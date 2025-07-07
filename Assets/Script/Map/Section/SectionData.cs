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
    public Vector2 sectionPosition;

    public List<GameObject> linkSections;

    public int stepCost;

    void Start() {
        sightObjects = GameDataManager.Data.sightObjects;
    }

    public void ActiveSightSection() {
        if(!isVisited) {
            sightSectionPrefab = GameDataManager.Data.sightSectionPrefab;
            GameObject go = Instantiate(sightSectionPrefab, this.transform.position, Quaternion.identity);
            go.transform.SetParent(sightObjects.transform);
            isVisited = true;
        }
    }

    public void ActiveLinkSection() {
        if(linkSections != null) {
            foreach(var link in linkSections) {
                link.SetActive(isPlayerOn);
            }
            foreach(var link in this.gameObject.GetComponents<LinkSection>()) {
                link.linkObj.SetActive(!isPlayerOn);
            }
        }
    }
}