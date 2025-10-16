using UnityEngine;
using System;
using System.Collections.Generic;

public class AchieveManager : MonoBehaviour {
    public GameObject contentObj;

    public List<string> achieveList;

    public GameObject bodyPrefab;

    void OnEnable() {
        achieveList.Add("집에 가고싶어요");

        foreach(var achieve in achieveList) {
            GameObject go = Instantiate(bodyPrefab, contentObj.transform);
            go.GetComponent<AchieveTextInfo>().content = achieve;
        }
    }
}