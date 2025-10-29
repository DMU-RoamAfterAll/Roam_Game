using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class LinkSectionSpawner : MonoBehaviour {
    [Header("GameData")]
    public List<GameObject> areaObjects;

    [Header("Section Data")]
    public List<(Transform mainObj, Transform subObj)> closestPairs;

    // 아주 가까운(사실상 같은 점) 판정용
    const float EPS = 1e-4f;

    void Start() {
        areaObjects = MapSceneDataManager.Instance.areaObjects;
        closestPairs = new();

        // --- 서로 다른 에어리어 사이 연결(필요 시 인덱스 유지/수정) ---
        FindCrossAreaLinks(areaObjects[5], areaObjects[0], areaObjects[1]);
        FindCrossAreaLinks(areaObjects[4], areaObjects[2], areaObjects[3]);
        FindCrossAreaLinks(areaObjects[0], areaObjects[1]);
        FindCrossAreaLinks(areaObjects[2], areaObjects[3]);

        // --- 같은 에어리어 내부(Main rate) 연결 ---
        foreach (var area in areaObjects.Where(a => a != null)) {
            FindSameAreaMainLinks(area);
        }

        // --- 실제 LinkSection 컴포넌트 부착 ---
        AttachLinks();
    }

    // ================= 교차 에어리어 =================
    void FindCrossAreaLinks(params object[] objs) {
        GameObject mainArea = objs[0] as GameObject;
        if (mainArea == null) return;

        var mainSections = GetChildSections(mainArea);
        if (mainSections.Count == 0) return;

        for (int i = 1; i < objs.Length; i++) {
            GameObject subArea = objs[i] as GameObject;
            if (subArea == null) continue;
            if (ReferenceEquals(subArea, mainArea)) continue; // 내부는 별도 처리

            var subSections = GetChildSections(subArea);
            if (subSections.Count == 0) continue;

            float minDist = float.MaxValue;
            SectionData closestMain = null;
            SectionData closestSub  = null;

            foreach (var m in mainSections) {
                foreach (var s in subSections) {
                    if (m == null || s == null) continue;
                    if (Same(m.transform, s.transform)) continue;

                    float dist = Vector2.Distance(m.transform.position, s.transform.position);
                    if (dist < minDist) {
                        minDist = dist;
                        closestMain = m;
                        closestSub  = s;
                    }
                }
            }

            // 충분히 멀 때만 링크 후보
            if (closestMain != null && closestSub != null &&
                minDist > MapSceneDataManager.mapData.initialMaxDistance + EPS) {
                AddPairIfNeeded(closestMain.transform, closestSub.transform, minDist);
            }
        }
    }

    // =============== 같은 에어리어(Main by rate) ===============
    void FindSameAreaMainLinks(GameObject area) {
        var all = GetChildSections(area);
        if (all.Count == 0) return;

        var mains = all.Where(sd => sd != null && IsMainByRate(sd)).ToList();
        if (mains.Count == 0) return;

        foreach (var main in mains) {
            SectionData nearest = null;
            float minDist = float.MaxValue;

            foreach (var other in all) {
                if (other == null) continue;
                if (Same(other.transform, main.transform)) continue;

                float d = Vector2.Distance(main.transform.position, other.transform.position);
                // 거리 0(동일 좌표) 후보는 제외
                if (d <= EPS) continue;

                if (d < minDist) {
                    minDist = d;
                    nearest = other;
                }
            }

            // 충분히 멀 때만 링크 후보
            if (nearest != null &&
                minDist > MapSceneDataManager.mapData.initialMaxDistance + EPS) {
                AddPairIfNeeded(main.transform, nearest.transform, minDist);
            }
        }
    }

    bool IsMainByRate(SectionData sd) {
        // char, string 모두 안전. 문자열이면 그대로, char면 글자 1개, null이면 빈 문자열.
        string r = sd != null ? $"{sd.rate}".Trim() : string.Empty;

        return r.Equals("Main", StringComparison.OrdinalIgnoreCase)
            || r.Equals("M", StringComparison.OrdinalIgnoreCase);
    }

    // =============== 유틸: 에어리어 자식 SectionData 수집 ===============
    List<SectionData> GetChildSections(GameObject area) {
        var list = new List<SectionData>();
        if (area == null) return list;

        // 필요하면 GetComponentsInChildren<SectionData>(true)로 바꿔도 됨
        foreach (Transform child in area.transform) {
            var sd = child.GetComponent<SectionData>();
            if (sd != null) list.Add(sd);
        }

        // 혹시 같은 Transform이 중복으로 들어오면 제거
        return list
            .GroupBy(sd => sd.transform)
            .Select(g => g.First())
            .ToList();
    }

    // =============== 페어 추가(중복/기존 링크/자기자신 방지) ===============
    void AddPairIfNeeded(Transform a, Transform b, float dist) {
        if (a == null || b == null) return;
        if (Same(a, b)) return; // 자기 자신 차단
        if (dist <= MapSceneDataManager.mapData.initialMaxDistance + EPS) return; // 가까우면 불필요

        bool alreadyQueued = closestPairs.Any(p =>
            (Same(p.mainObj, a) && Same(p.subObj, b)) ||
            (Same(p.mainObj, b) && Same(p.subObj, a))
        );
        if (alreadyQueued) return;

        if (IsAlreadyLinked(a, b)) return;

        closestPairs.Add((a, b));
    }

    bool IsAlreadyLinked(Transform a, Transform b) {
        if (Same(a, b)) return true; // 자기자신이면 '이미 연결'로 간주해 차단
        var al = a.GetComponents<LinkSection>();
        var bl = b.GetComponents<LinkSection>();
        return (al != null && al.Any(ls => ls && Same(ls.linkedSection?.transform, b))) ||
               (bl != null && bl.Any(ls => ls && Same(ls.linkedSection?.transform, a)));
    }

    // =============== 실제 컴포넌트 부착 ===============
    void AttachLinks() {
        foreach (var pair in closestPairs) {
            AttachLinkIfMissing(pair.mainObj, pair.subObj);
            AttachLinkIfMissing(pair.subObj, pair.mainObj);
        }
    }

    void AttachLinkIfMissing(Transform from, Transform to) {
        if (from == null || to == null) return;
        if (Same(from, to)) {
            Debug.LogWarning($"[LinkSectionSpawner] self-link prevented on '{from.name}'");
            return;
        }

        // 이미 같은 대상에 대한 링크가 있으면 스킵
        bool exists = from.GetComponents<LinkSection>()
                         .Any(ls => ls && Same(ls.linkedSection?.transform, to));
        if (exists) return;

        var link = from.gameObject.AddComponent<LinkSection>();
        link.linkedSection = to.gameObject;
    }

    // =============== 동등성 헬퍼(Null 안전 + Transform 동일성) ===============
    static bool Same(Transform a, Transform b) {
        if (a == null || b == null) return false;
        return a == b;
    }
}