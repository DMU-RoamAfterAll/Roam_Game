using UnityEngine;
using System;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour {
    [Header("Objects")]
    public GameObject contentObj;

    public List<GameObject> areaMaskList;
    public List<GameObject> headerMaskList;
    public List<GameObject> mainSectionMaskList;

    public List<GameObject> areaObjects;
    public List<GameObject> sections;
    public List<GameObject> mainSections;

    [Header("Prefabs")]
    public GameObject listPrefab;
    public GameObject headerPrefab;
    public GameObject bodyPrefab;

    void OnEnable() {
        UpdateSectionInfo();
    }

    void UpdateSectionInfo() {
        areaObjects = MapSceneDataManager.Instance.areaObjects;
        sections = MapSceneDataManager.Instance.sections;
        mainSections = MapSceneDataManager.Instance.mainSections;
        
        foreach(var area in areaObjects) {
            GameObject areaObj = Instantiate(listPrefab, contentObj.transform);
            GameObject headerObj = Instantiate(headerPrefab, areaObj.transform);

            areaObj.name = area.name;
            areaMaskList.Add(areaObj);
            headerMaskList.Add(headerObj);

            foreach(var section in sections) {
                if(section.transform.parent.name != areaObj.name || !section.GetComponent<SectionData>().isVisited) 
                    continue;
                GameObject sectionObj = Instantiate(bodyPrefab, areaObj.transform);
                sectionObj.GetComponent<BodyMaskInfo>().sd = section.GetComponent<SectionData>();
            }

            foreach(var mSection in mainSections) {
                if(mSection.transform.parent.name != areaObj.name || !mSection.GetComponent<SectionData>().isVisited) 
                    continue;
                GameObject mSectionObj = Instantiate(bodyPrefab, areaObj.transform);
                mSectionObj.GetComponent<BodyMaskInfo>().sd = mSection.GetComponent<SectionData>();
            }
        }
    }

    void OnDisable() {
        foreach (Transform child in contentObj.transform) {
            Destroy(child.gameObject);
        }
    }
}