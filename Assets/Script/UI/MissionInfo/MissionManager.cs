using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
    public GameObject hiddenBodyPrefab;

    void OnEnable() {
        UpdateSectionInfo();
    }

    void UpdateSectionInfo() {
        areaObjects = MapSceneDataManager.Instance.areaObjects;
        sections = MapSceneDataManager.Instance.sections;
        mainSections = MapSceneDataManager.Instance.mainSections;

        Debug.Log(AddHiddenSection());
        if(AddHiddenSection() != null) {
            GameObject hiddenAreaObj = Instantiate(listPrefab, contentObj.transform);
            GameObject hiddenHeaderObj = Instantiate(headerPrefab, hiddenAreaObj.transform);
            GameObject hiddenBodyObj = Instantiate(hiddenBodyPrefab, hiddenAreaObj.transform);
        }
        
        foreach(var area in areaObjects) {
            GameObject areaObj = Instantiate(listPrefab, contentObj.transform);
            GameObject headerObj = Instantiate(headerPrefab, areaObj.transform);

            areaObj.name = area.name;
            areaMaskList.Add(areaObj);
            headerMaskList.Add(headerObj);

            foreach(var section in sections) {
                if(section.transform.parent.name != areaObj.name || !section.GetComponent<SectionData>().isCleared) 
                    continue;
                GameObject sectionObj = Instantiate(bodyPrefab, areaObj.transform);
                sectionObj.GetComponent<BodyMaskInfo>().sd = section.GetComponent<SectionData>();
            }

            foreach(var mSection in mainSections) {
                if(mSection.transform.parent.name != areaObj.name || !mSection.GetComponent<SectionData>().isCleared) 
                    continue;
                GameObject mSectionObj = Instantiate(bodyPrefab, areaObj.transform);
                mSectionObj.GetComponent<BodyMaskInfo>().sd = mSection.GetComponent<SectionData>();
            }
        }
    }

    const string hiddenFolderPath = "StoryGameData/SectionData/SectionEvent/HiddenSection";

    public string AddHiddenSection() {
        var wm = WeatherManager.Instance;
        if(wm == null || !wm.isHidden) return null;

        
        var assets = Resources.LoadAll<TextAsset>(hiddenFolderPath);
        if(assets == null || assets.Length == 0) return null;

        foreach(var ta in assets) {
            string prefix = GetnNamePrefix(ta.name);
            if(!string.Equals(prefix, wm.weatherCur, System.StringComparison.OrdinalIgnoreCase)) continue;

            #if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(ta);
            return assetPath;
            #else
            string resourcePath = $"{hiddenFolderPath}/{ta.name}";
            return resourcePath;
            #endif
        }

        return null;
    }

    string GetnNamePrefix(string stem) {
        int idx = stem.IndexOf('_');
        if (idx > 0) return stem.Substring(0, idx);
        if (idx == 0) return string.Empty;
        return stem;
    }

    void OnDisable() {
        foreach (Transform child in contentObj.transform) {
            Destroy(child.gameObject);
        }
    }
}