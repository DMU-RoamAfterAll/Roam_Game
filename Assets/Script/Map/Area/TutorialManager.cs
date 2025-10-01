using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour {
    public GameObject nextGo;
    public GameObject prevGo;
    public SectionData sd;
    public CircleCollider2D col2D;

    public Transform p;

    void Start() {
        sd = GetComponent<SectionData>();
        col2D = GetComponent<CircleCollider2D>();

        p = transform.parent;
        int idx = transform.GetSiblingIndex();
        nextGo = (idx < p.childCount - 1) ? p.GetChild(idx + 1).gameObject : null;
        prevGo = (idx > 0) ? p.GetChild(idx - 1).gameObject : null;

        if(prevGo != null) col2D.enabled = false;
    }

    public void CompleteSection() {
        if(nextGo == null) { 
            GameDataManager.Data.tutorialClear = true;
            MapSceneDataManager.Instance?.Player.transform.SetParent(null);
            SaveLoadManager.Instance.AfterTutorialClear();
            GameObject tutorialArea = MapSceneDataManager.Instance?.areaObjects[MapSceneDataManager.Instance.areaObjects.Count - 1];
            Destroy(tutorialArea);
            MapSceneDataManager.Instance?.areaObjects.Remove(tutorialArea);
            foreach(var n in MapSceneDataManager.Instance?.areaObjects) {
                n.SetActive(true);
            }
        }
        else { nextGo.GetComponent<CircleCollider2D>().enabled = true; }
    }
}