using UnityEngine;
using System.Collections.Generic;

public class LinkSectionSpawner : MonoBehaviour {
    [Header("GameData")]
    public List<GameObject> areaObjects;

    [Header("Section Data")]
    public List<(Transform mainObj, Transform subObj)> closestPairs;
    public float minDist;

    void Start() {
        areaObjects = GameDataManager.Instance.areaObjects;
        closestPairs = new();

        FindLinkSection(areaObjects[5], areaObjects[0], areaObjects[1]);
        FindLinkSection(areaObjects[4], areaObjects[2], areaObjects[3]);
        FindLinkSection(areaObjects[0], areaObjects[1]);
        FindLinkSection(areaObjects[2], areaObjects[3]);

        foreach (var pair in closestPairs) {
            LinkSection mainLinkSection = pair.mainObj.gameObject.AddComponent<LinkSection>();
            mainLinkSection.linkedSection = pair.subObj.gameObject;

            LinkSection subLinkSection = pair.subObj.gameObject.AddComponent<LinkSection>();
            subLinkSection.linkedSection = pair.mainObj.gameObject;
        }
    }

    ///각 구역들끼리의 최대거리를 벗어나는 Section을 찾음
    void FindLinkSection(params object[] objs) {
        GameObject mainArea = objs[0] as GameObject;
        if (mainArea == null) return;

        List<Transform> mainChildren = GetChildTransforms(mainArea);

        for (int i = 1; i < objs.Length; i++) {
            GameObject subArea = objs[i] as GameObject;
            if (subArea == null) continue;

            List<Transform> subChildren = GetChildTransforms(subArea);

            float minDist = float.MaxValue;
            Transform closestMain = null;
            Transform closestSub = null;

            foreach (var m in mainChildren) {
                foreach (var s in subChildren) {
                    float dist = Vector2.Distance(
                        new Vector2(m.position.x, m.position.y),
                        new Vector2(s.position.x, s.position.y)
                    );
                    if (dist < minDist) {
                        minDist = dist;
                        closestMain = m;
                        closestSub = s;
                    }
                }
            }

            if (closestMain != null && closestSub != null && minDist > GameDataManager.Data.initialMaxDistance) {
                closestPairs.Add((closestMain, closestSub));
            }
        }
    }

    List<Transform> GetChildTransforms(GameObject area) {
        List<Transform> children = new();
        foreach (Transform child in area.transform) {
            children.Add(child);
        }
        return children;
    }
}