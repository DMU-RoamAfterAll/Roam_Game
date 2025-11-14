using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;

public class PointsDemo : MonoBehaviour
{
    [Header("Gen Params")]
    public int count = 20;
    public float minDist = 10f;
    public float maxDist = 12f;
    public float maxRadius = 30f;

    [Header("Visuals")]
    public GameObject pointPrefab;   // 선택사항: 안넣어도 동작(런타임 점 생성)
    public float pointSize = 2f;
    public Color pointColor = Color.black;
    public Color originColor = Color.cyan;
    public bool drawRadiusRing = true;

    [Header("Camera")]
    public bool makeOrthoCameraIfMissing = true;

    // 내부
    private Sprite _dotSprite;

    void Start()
    {
        count = 20;
        minDist = 10f;
        maxDist = 12f;
        maxRadius = 30f;

        pointSize = 2f;
        pointColor = Color.black;
        originColor = Color.white;
        drawRadiusRing = true;

        makeOrthoCameraIfMissing = true;

        EnsureCamera();
        BuildDotSpriteIfNeeded();

        var pts = GenerateGuaranteedPoints(count, minDist, maxDist, maxRadius);

        StartCoroutine(ShowPointsStepByStep(pts));
    }

    IEnumerator ShowPointsStepByStep(List<Vector2> pts)
    {
        // 원점 표시
        SpawnPoint(Vector2.zero, originColor, pointSize * 1.1f);

        // 범위 링
        if (drawRadiusRing) DrawRing(maxRadius, 96, new Color(1,1,1,0.25f), 0.03f);

        // 점 하나씩 천천히
        foreach (var p in pts)
        {
            SpawnPoint(p, pointColor, pointSize);
            yield return new WaitForSeconds(0.3f);
        }
    }

    void EnsureCamera()
    {
        var cam = Camera.main;
        if (!cam && makeOrthoCameraIfMissing)
        {
            var go = new GameObject("Main Camera");
            cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.1f, 1f);
            cam.orthographic = true;
            cam.transform.position = new Vector3(0, 0, -10);
        }

        if (cam && cam.orthographic)
        {
            cam.orthographicSize = 70f;
            cam.transform.position = new Vector3(0, 0, -10);
        }
    }

    void BuildDotSpriteIfNeeded()
    {
        if (_dotSprite != null || pointPrefab) return;

        _dotSprite = BuildCircleSprite(64, 1.0f); // (해상도, feather)
    }

    Sprite BuildCircleSprite(int size = 64, float feather = 1.0f)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear; // 가장자리 부드럽게. 각지게 원하면 Point

        var px = new Color[size * size];
        float r = (size - 1) * 0.5f;
        float cx = r, cy = r;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - cx, dy = y - cy;
            float d  = Mathf.Sqrt(dx * dx + dy * dy);

            // 원 경계에서 부드럽게 알파를 줄이는 feather
            float a = Mathf.Clamp01((r - d) / Mathf.Max(0.0001f, feather));
            // 원 밖이면 a=0, 안쪽이면 1~0 사이
            px[y * size + x] = new Color(1, 1, 1, a);
        }

        tex.SetPixels(px);
        tex.Apply();

        // pivot = 가운데, pixelsPerUnit = size (1:1 스케일 맞추기 편함)
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    void SpawnPoint(Vector2 pos, Color col, float size)
    {
        GameObject go;

        if (pointPrefab)
        {
            go = Instantiate(pointPrefab, transform);
            if (!go.GetComponent<SpriteRenderer>() && !go.GetComponent<MeshRenderer>())
                Debug.LogWarning("Prefab has no renderer. Add SpriteRenderer/MeshRenderer to see it.");
        }
        else
        {
            go = new GameObject("dot");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _dotSprite;
            sr.color = col;
        }

        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = Vector3.one * size;
    }

    public void DrawRing(float radius, int segments, Color col, float width)
    {
        var go = new GameObject("ring");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = segments + 1;
        lr.loop = true;
        lr.useWorldSpace = true;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = col;
        lr.endColor = col;
        lr.sortingOrder = 10;

        for (int i = 0; i <= segments; i++)
        {
            float t = (i % segments) / (float)segments;
            float ang = t * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * radius, Mathf.Sin(ang) * radius, 0f));
        }
    }

    // ===== 실제 게임과 동일한 포인트 생성 로직(간단 버전) =====
    List<Vector2> GenerateGuaranteedPoints(int cnt, float minD, float maxD, float maxR, Vector2? start = null)
    {
        Vector2 current = start ?? Vector2.zero;
        var generated = new List<Vector2> { current };
        var all = new List<Vector2>(generated);

        var rng = new System.Random(UniqueSeed("DEMO_SEED_KEY_19238479012387492"));

        int guard = 0;
        while (generated.Count < cnt)
        {
            guard++;
            Vector2 origin = all[rng.Next(all.Count)];
            float ang = (float)(rng.NextDouble() * Mathf.PI * 2);
            float dist = minD + (float)rng.NextDouble() * (maxD - minD);
            Vector2 cand = origin + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;

            if (cand.magnitude > maxR) continue;
            if (all.Any(p => Vector2.Distance(cand, p) < minD)) continue;
            if (!all.Any(p => Vector2.Distance(cand, p) <= maxD)) continue;

            generated.Add(cand);
            all.Add(cand);

            if (guard > 5000) { Debug.LogWarning("Generation guard break"); break; }
        }
        return generated;
    }

    int UniqueSeed(string key)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
        return System.BitConverter.ToInt32(hash, 0);
    }
}