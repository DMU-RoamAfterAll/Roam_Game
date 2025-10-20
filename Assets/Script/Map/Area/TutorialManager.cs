using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour {
    public GameObject nextGo;
    public GameObject prevGo;
    public SectionData sd;
    public CircleCollider2D col2D;

    public Transform p;

    // (선택) 상한 거리 참고용. 굳이 안 써도 되지만,
    // MapData의 initialMaxDistance를 기본값으로 가져와 로그에 참고 가능.
    [SerializeField] private float postTutorialMaxDistance = -1f;

    void Start() {
        sd = GetComponent<SectionData>();
        col2D = GetComponent<CircleCollider2D>();

        p = transform.parent;
        int idx = transform.GetSiblingIndex();
        nextGo = (idx < p.childCount - 1) ? p.GetChild(idx + 1).gameObject : null;
        prevGo = (idx > 0) ? p.GetChild(idx - 1).gameObject : null;

        if (prevGo != null) col2D.enabled = false;

        if (postTutorialMaxDistance <= 0f)
            postTutorialMaxDistance = MapSceneDataManager.mapData.initialMaxDistance;
    }

    public void CompleteSection() {
        if (nextGo == null) {
            // 튜토리얼 종료 처리
            GameDataManager.Data.tutorialClear = true;

            var msdm = MapSceneDataManager.Instance;
            var player = msdm?.Player;
            var pc = msdm?.pc;
            if (msdm == null || player == null || pc == null) {
                Debug.LogWarning("[Tutorial] MapSceneDataManager/Player/PC가 없음");
                return;
            }

            // 튜토리얼 에어리어(마지막) 참조
            int tutIndex = msdm.areaObjects.Count - 1;
            var tutorialArea = (tutIndex >= 0 && tutIndex < msdm.areaObjects.Count)
                ? msdm.areaObjects[tutIndex]
                : null;

            // 1) 튜토리얼이 아닌 모든 섹션들 중에서 가장 가까운 섹션 찾기
            var nearest = FindNearestNonTutorialSection(tutorialArea);
            if (nearest == null) {
                Debug.LogWarning("[Tutorial] 이동할 대상 섹션을 찾지 못했음");
                // 기존 처리대로만 종료
                MapSceneDataManager.Instance?.Player.transform.SetParent(null);
                SaveLoadManager.Instance.AfterTutorialClear();
                foreach (var n in msdm.areaObjects) n.SetActive(true);
                return;
            }

            // 2) 다른 에어리어들을 먼저 활성화 (비활성 부모로 들어가며 플레이어가 함께 꺼지는 것 방지)
            for (int i = 0; i < msdm.areaObjects.Count; i++) {
                if (i == tutIndex) continue; // 튜토리얼 제외
                var a = msdm.areaObjects[i];
                if (a) a.SetActive(true);
            }

            // 3) 플레이어를 가장 가까운 섹션으로 이동(부모/좌표/플래그 세팅)
            if (pc.sectionData != null) pc.sectionData.isPlayerOn = false;

            pc.preSection     = pc.currentSection;
            pc.currentSection = nearest.gameObject;
            pc.sectionData    = nearest;
            nearest.isPlayerOn = true;

            player.transform.SetParent(nearest.transform, true);
            player.transform.position = nearest.transform.position;

            // (참고 로그) 상한 거리 대비 실제 거리
            float d = Vector2.Distance((Vector2)transform.position, (Vector2)nearest.transform.position);
            Debug.Log($"[Tutorial] 플레이어를 가장 가까운 섹션 '{nearest.name}'로 이동 (거리 {d:F1}, 상한 {postTutorialMaxDistance:F1})");

            // 4) 저장 상태 정리
            // 튜토리얼 종료 시점 저장 구조 초기화(visited 초기화 등) → 플레이어 새 위치 반영
            SaveLoadManager.Instance.AfterTutorialClear();
            SaveLoadManager.Instance.SaveNow();

            // 6) 주변 감지/색상 갱신
            pc.DetectSection();
        }
        else {
            // 다음 튜토리얼 섹션 열기
            nextGo.GetComponent<CircleCollider2D>().enabled = true;
        }
    }

    /// <summary>
    /// 튜토리얼 에어리어를 제외한 모든 섹션 중 플레이어(혹은 현재 튜토리얼 섹션)에서 가장 가까운 섹션 탐색
    /// </summary>
    private SectionData FindNearestNonTutorialSection(GameObject tutorialArea) {
        var msdm = MapSceneDataManager.Instance;
        if (msdm == null) return null;

        Vector3 origin = transform.position; // 현재(튜토리얼) 섹션 위치 기준

        SectionData best = null;
        float bestDist = float.MaxValue;

        // msdm.sections + msdm.mainSections 둘 다 후보에 포함
        // (튜토리얼 에어리어의 자식은 제외)
        void ConsiderList(List<GameObject> list) {
            if (list == null) return;
            foreach (var go in list) {
                if (!go) continue;
                if (tutorialArea && go.transform.IsChildOf(tutorialArea.transform)) continue; // 튜토리얼 제외
                if (go.TryGetComponent<SectionData>(out var sd)) {
                    float d = Vector2.Distance(origin, sd.transform.position);
                    if (d < bestDist) { bestDist = d; best = sd; }
                }
            }
        }

        ConsiderList(msdm.sections);
        ConsiderList(msdm.mainSections);

        return best;
    }
}