using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour {
    public GameObject nextGo;
    public GameObject prevGo;
    public SectionData sd;

    public Transform p;
    void Start() {
        sd = GetComponent<SectionData>();

        p = transform.parent;
        int idx = transform.GetSiblingIndex();
        nextGo = (idx < p.childCount - 1) ? p.GetChild(idx + 1).gameObject : null;
        prevGo = (idx > 0) ? p.GetChild(idx - 1).gameObject : null;
    }

    public void CompleteSection() {
        if (nextGo == null) {
            // 튜토리얼 종료 처리
            GameDataManager.Data.tutorialClear = true;

            var msdm  = MapSceneDataManager.Instance;
            var player= msdm?.Player;
            var pc    = msdm?.pc;
            if (msdm == null || player == null || pc == null) {
                Debug.LogWarning("[Tutorial] MapSceneDataManager/Player/PC가 없음");
                return;
            }

            // 튜토리얼 에어리어(마지막) 참조
            int tutIndex = msdm.areaObjects.Count - 1;
            var tutorialArea = (tutIndex >= 0 && tutIndex < msdm.areaObjects.Count)
                ? msdm.areaObjects[tutIndex]
                : null;

            // 2) 다른 에어리어들을 먼저 활성화 (비활성 부모로 들어가며 플레이어가 함께 꺼지는 것 방지)
            for (int i = 0; i < msdm.areaObjects.Count; i++) {
                if (i == tutIndex) continue; // 튜토리얼 제외
                var a = msdm.areaObjects[i];
                if (a) a.SetActive(true);
            }

            // 5) 저장 상태 정리 + 저장
            SaveLoadManager.Instance.SaveNow();

            // 6) 주변 감지/색상 갱신 (이제 근처 섹션들이 이동 가능으로 뜸)
            pc.DetectSection();
        }
    }
}