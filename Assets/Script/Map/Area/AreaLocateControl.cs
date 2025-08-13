using UnityEngine;
using System.Linq;
using System.IO;
using System.Collections;

public class AreaLocateControl : MonoBehaviour {
    [Header("GameData")]
    public GameObject Player; //플레이어
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

        OnAreaMoveFinished += CreateRiverSection;
        OnAreaMoveFinished += CreateIrisSection;
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


    ///구한 구역의 귝겨에 맞게 구역을 이동시킨
    IEnumerator MoveArea() {
        int count = createdAreaCount;
        Vector2[] starts = new Vector2[count];
        Vector2[] ends = new Vector2[count];
        float[] durations = new float[count];
        float[] elapsed = new float[count];

        // 초기 위치와 목표 위치, 시간 설정
        for (int i = 0; i < count; i++) {
            starts[i] = areas[i].transform.position;
            ends[i] = basePoint[i];
            durations[i] = Random.Range(0.5f, 1.2f);
            elapsed[i] = 0f;
        }

        bool allDone = false;

        while (!allDone) {
            allDone = true;

            for (int i = 0; i < count; i++) {
                if (elapsed[i] < durations[i]) {
                    elapsed[i] += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed[i] / durations[i]);
                    areas[i].transform.position = Vector2.Lerp(starts[i], ends[i], t);
                    allDone = false; // 아직 이동 중인 스포너가 있음
                }
            }

            yield return null; // 다음 프레임까지 대기  
        }

        // 끝 위치 고정
        for (int i = 0; i < count; i++) {
            areas[i].transform.position = ends[i];
        }

        OnAreaMoveFinished?.Invoke();

        this.gameObject.AddComponent<LinkSectionSpawner>();

        MapSceneDataManager.mapData.isMapSetUp = true;
        Player.GetComponent<PlayerControl>().DetectSection();
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
}