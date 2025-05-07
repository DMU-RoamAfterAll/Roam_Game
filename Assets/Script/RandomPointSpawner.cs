using System.Collections.Generic;
using UnityEngine;

public class RandomPointSpawner : MonoBehaviour
{
    [Header("좌표 범위 수동 입력")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    [Header("좌표 개수 및 최소 거리")]
    public int pointCount;
    public float initialMinDistance;

    [Header("랜덤 시드 (같은 값이면 결과도 같음)")]
    public int seed;

    [Header("생성할 프리팹")]
    public GameObject pointPrefab;

    void Start()
    {
        if (pointPrefab == null)
        {
            Debug.LogError("Point Prefab이 설정되지 않았습니다!");
            return;
        }

        List<Vector2> points = GenerateGuaranteedPoints(pointCount, initialMinDistance, seed);
        foreach (var point in points)
        {
            Instantiate(pointPrefab, new Vector3(point.x, point.y, 0f), Quaternion.identity);
        }
    }

    List<Vector2> GenerateGuaranteedPoints(int count, float minDist, int seed)
    {
        List<Vector2> result = new List<Vector2>();
        System.Random rng = new System.Random(seed);
        int maxAttemptsPerPoint = 500;
        float minDistStep = minDist * 0.1f; // 줄일 때 10%씩 감소

        while (result.Count < count)
        {
            int attempts = 0;
            bool pointPlaced = false;

            while (attempts < maxAttemptsPerPoint)
            {
                float x = (float)(rng.NextDouble() * (maxX - minX) + minX);
                float y = (float)(rng.NextDouble() * (maxY - minY) + minY);
                Vector2 candidate = new Vector2(x, y);

                bool isValid = true;
                foreach (var point in result)
                {
                    if (Vector2.Distance(point, candidate) < minDist)
                    {
                        isValid = false;
                        break;
                    }
                }

                if (isValid)
                {
                    result.Add(candidate);
                    pointPlaced = true;
                    break;
                }

                attempts++;
            }

            if (!pointPlaced)
            {
                // 너무 좁아서 새 점을 못 놓는 경우: 최소 거리 조건 완화
                minDist = Mathf.Max(minDist - minDistStep, 0f);
                Debug.LogWarning($"거리 조건 완화됨: 현재 최소 거리 {minDist:F2}");
            }
        }

        return result;
    }
}