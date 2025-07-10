using UnityEngine;
using System.Linq;
using System.IO;
using System.Collections;

public class AreaLocateControl : MonoBehaviour {
    [Header("Data")]
    public static int totalAreaCount;
    public int river;
    public float minDistance;
    public bool flag;
    public RandomSectionSpawner[] spawners;

    [Header("Base Point")]
    public float[] width;
    public float[] height;
    public Vector2[] basePoint;
    
    void Awake() {
        totalAreaCount = 0;
        flag = false;
    }

    void Start() {
        minDistance = GameDataManager.Data.initialMinDistance;
        river = GameDataManager.Data.riverHeight;
    }

    void Update() {
        if(totalAreaCount == GameDataManager.Data.areaNumber && !flag) {
            FindAreaPoint();
            flag = true;
        }
    }

    void FindAreaPoint() {
        totalAreaCount--;

        spawners = GameDataManager.Instance.areaObjects
            .Where(spawner => spawner.CompareTag(Tag.Area))
            .Select(go => go.GetComponent<RandomSectionSpawner>())
            .ToArray();

        float x = 0;
        float y = 0;

        basePoint = new Vector2[totalAreaCount];
        width = new float[totalAreaCount];
        height = new float[totalAreaCount];

        for(int i = 0; i < totalAreaCount; i++) {
            width[i] = spawners[i].maxX - spawners[i].minX;
            height[i] = spawners[i].maxY - spawners[i].minY;

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

                    y = river + (height[0] > height[1] ? height[0] : height[1]) + minDistance + (height[i] / 2);

                    break;

                case 3 :
                    x = minDistance + (width[i] / 2);

                    y = river + (height[0] > height[1] ? height[0] : height[1]) + minDistance + (height[i] / 2);

                    break;

                case 4 :
                    x = 0;
                    
                    y = minDistance + (height[i] / 2) +
                        (height[0] > height[1] ? height[0] : height[1]) + river + minDistance +
                        (height[2] > height[3] ? height[2] : height[3]);

                    break;
            }

            basePoint[i] = new Vector2(x, y);
        }

        StartCoroutine(MoveArea());
    }

    IEnumerator MoveArea() {
        int count = totalAreaCount;
        Vector2[] starts = new Vector2[count];
        Vector2[] ends = new Vector2[count];
        float[] durations = new float[count];
        float[] elapsed = new float[count];

        // 초기 위치와 목표 위치, 시간 설정
        for (int i = 0; i < count; i++) {
            starts[i] = spawners[i].transform.position;
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
                    spawners[i].transform.position = Vector2.Lerp(starts[i], ends[i], t);
                    allDone = false; // 아직 이동 중인 스포너가 있음
                }
            }

            yield return null; // 다음 프레임까지 대기
        }

        // 끝 위치 고정
        for (int i = 0; i < count; i++) {
            spawners[i].transform.position = ends[i];
        }

        this.gameObject.AddComponent<LinkSectionSpawner>();

        GameDataManager.Instance.Player.GetComponent<PlayerControl>().DetectSection();
    }
}