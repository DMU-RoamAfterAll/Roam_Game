using UnityEngine;
using System.Linq;
using System.IO;
using System.Collections;

public class AreaLocateControl : MonoBehaviour {
    [Header("GameData")]
    public GameObject Player; //플레이어
    public PlayerControl pc;
    public float minDistance; //section과의 최소 거린
    public int riverHeight; //중간 river 구역의 폭
    public int areaNumber; //area의 개수
    
    [Header("Data")]
    public int createdAreaCount; //총 구역 개수
    public RandomSectionSpawner[] areas; //구역을 참조

    [Header("Base Point")]
    public float[] width;
    public float[] height;
    public Vector2[] basePoint;

    public event System.Action OnAreaMoveFinished;

    void Awake() {
        createdAreaCount = 0;
    }

    ///처음 게임 생성 시 필요한 데이터들을 GameData에 맞게 설정된 수치를 가져 옴
    void Start() {
        minDistance = MapSceneDataManager.mapData.initialMinDistance;
        riverHeight = MapSceneDataManager.mapData.riverHeight;
        areaNumber = MapSceneDataManager.mapData.areaNumber;

        Player = MapSceneDataManager.Instance.Player;
        pc = MapSceneDataManager.Instance.pc;
    }

    ///구역의 규격을 알아내는 함수
    public void FindAreaPoint() {
        if(createdAreaCount == areaNumber) {
            createdAreaCount--;

            areas = MapSceneDataManager.Instance.areaObjects
                .Where(spawner => spawner.CompareTag(Tag.Area))
                .Select(go => go.GetComponent<RandomSectionSpawner>())
                .ToArray();

            float x = 0;
            float y = 0;

            basePoint = new Vector2[createdAreaCount];
            width = new float[createdAreaCount];
            height = new float[createdAreaCount];

            for(int i = 0; i < createdAreaCount; i++) {
                width[i] = areas[i].maxX - areas[i].minX;
                height[i] = areas[i].maxY - areas[i].minY;

                switch(i) {
                    case 0 :
                        x = minDistance + (width[i] / 2);
                        x *= -1f;

                        y = (height[i] / 2);

                        break;

                    case 1 :
                        x = minDistance + (width[i] / 2);

                        y = (height[i] / 2);

                        break;

                    case 2 :
                        x = minDistance + (width[i] / 2);
                        x *= -1f;

                        y = riverHeight + (height[0] > height[1] ? height[0] : height[1]) + minDistance + (height[i] / 2);

                        break;

                    case 3 :
                        x = minDistance + (width[i] / 2);

                        y = riverHeight + (height[0] > height[1] ? height[0] : height[1]) + minDistance + (height[i] / 2);

                        break;

                    case 4 :
                        x = 0;
                        
                        y = minDistance + (height[i] / 2) +
                            (height[0] > height[1] ? height[0] : height[1]) + riverHeight + minDistance +
                            (height[2] > height[3] ? height[2] : height[3]);

                        break;
                }

                basePoint[i] = new Vector2(x, y);
            }

            StartCoroutine(MoveArea());
        }
    }


    IEnumerator MoveArea() {
        int count = createdAreaCount;

        // 1) 애니메이션 없이 즉시 목표 위치로 이동
        for (int i = 0; i < count; i++) {
            areas[i].transform.position = basePoint[i];
        }

        // 2) 리버/아이리스 섹션 생성
        CreateRiverSection();
        CreateIrisSection();

        // 3) 링크 스포너 준비
        var linker = GetComponent<LinkSectionSpawner>();
        if (linker == null) linker = gameObject.AddComponent<LinkSectionSpawner>();

        // 한 프레임 양보(선택): 트랜스폼/인스턴스 반영 후 의존 코드가 있으면 안정적
        yield return null;

        // 4) 튜토리얼만 남기도록 비활성화
        StartToturial();

        // 5) 이벤트 알림
        OnAreaMoveFinished?.Invoke();
        EventManager.RaiseAreaMoveFinished();

        // 6) 펜딩 세이브 적용(있다면)
        var slm = SaveLoadManager.Instance;
        if (slm != null && slm.pendingLoadData != null) {
            Debug.Log("[AreaLocate] Applying pending save after map assembled");
            yield return StartCoroutine(slm.ApplyLoadedData(slm.pendingLoadData));
            slm.pendingLoadData = null;
        }

        // 7) 저장/업로드 및 상태 갱신
        SaveLoadManager.Instance.SaveNow();
        SaveLoadManager.Instance.SaveNowAndUpload(GameDataManager.Data.playerName);

        MapSceneDataManager.mapData.isMapSetUp = true;
        pc.isCanMove = true;
        pc.DetectSection();
        Debug.Log("Detect Section");
    }

    void CreateRiverSection() {
        float maxUpperHeight = Mathf.Max(height[0], height[1]);

        float riverCenterY = maxUpperHeight + (riverHeight / 2f) + (minDistance / 2f);

        GameObject go = Instantiate(MapSceneDataManager.mapData.riverSectionPrefab, new Vector2(0, riverCenterY), Quaternion.identity);
        go.transform.SetParent(this.transform);
    }

    void CreateIrisSection() {
        float irisHeight = basePoint[4].y + (height[4] / 2) + minDistance;

        GameObject go = Instantiate(MapSceneDataManager.mapData.IrisSectionPrefab, new Vector2(0, irisHeight), Quaternion.identity);
        go.transform.SetParent(this.transform);
    }

    void StartToturial() {
        if(GameDataManager.Data.tutorialClear) return;
        
        for(int i = 0; i < areaNumber - 1; i++) { areas[i].gameObject.SetActive(false); }
        foreach(var s in areas[areaNumber - 1].Sections) {
            s.gameObject.AddComponent<TutorialManager>();
        }
    }
}