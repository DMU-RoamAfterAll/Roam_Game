using UnityEngine;
using System.Linq;
using System.IO;

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

        MoveArea();
    }

    void MoveArea() {
        for(int i = 0; i < totalAreaCount; i++) {
            spawners[i].gameObject.transform.position = basePoint[i];
        }

        LinkSectionSpawner linkSectionSpawner = this.gameObject.AddComponent<LinkSectionSpawner>();
    }
}