using UnityEngine;
using System.Collections.Generic;

public class ObjectLineConnector : MonoBehaviour {
    public Vector2[] mainSections;

    [Header("Line Manual")]
    public float lineWidth = 0.05f;
    public Color lineColor = Color.red;

    [Header("Random Seed")]
    public int seed = 12345;

    private LineRenderer lineRenderer;

    public void ConnectorLine(List<Vector2> sections) {
        for (int i = 0; i < mainSections.Length; i++) {
            sections.Add(mainSections[i]);
        }

        if (sections == null || sections.Count == 0) {
            Debug.LogError("No Sections");
            return;
        }

        // MST (Prim's Algorithm) 생성
        HashSet<Vector2> visited = new HashSet<Vector2>();
        List<(Vector2, Vector2)> edges = new List<(Vector2, Vector2)>();

        Vector2 current = sections[0];
        visited.Add(current);

        while (visited.Count < sections.Count) {
            float shortestDistance = float.MaxValue;
            Vector2 nextPoint = Vector2.zero;
            Vector2 closestPoint = Vector2.zero;

            foreach (var start in visited) {
                foreach (var end in sections) {
                    if (visited.Contains(end)) continue;

                    float distance = Vector2.Distance(start, end);
                    if (distance < shortestDistance) {
                        shortestDistance = distance;
                        nextPoint = end;
                        closestPoint = start;
                    }
                }
            }

            if (nextPoint != Vector2.zero) {
                edges.Add((closestPoint, nextPoint));
                visited.Add(nextPoint);
            }
        }

        // 랜덤으로 몇 개의 추가 연결 생성 (시드값 적용)
        int extraEdges = Mathf.Min(5, sections.Count / 2); // 섹션 수에 비례하여 추가 연결 제한
        System.Random rng = new System.Random(seed);

        for (int i = 0; i < extraEdges; i++) {
            Vector2 start = sections[rng.Next(sections.Count)];
            Vector2 end = sections[rng.Next(sections.Count)];

            if (start != end && !edges.Contains((start, end)) && !edges.Contains((end, start))) {
                edges.Add((start, end));
            }
        }

        // 생성된 모든 엣지를 LineRenderer로 연결
        foreach (var (start, end) in edges) {
            GameObject lineObject = new GameObject("LineSegment");
            lineObject.tag = "LineSegment";
            lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.loop = false;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }
    }
}