using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AreaData", menuName = "ScriptableObject/AreaData", order = 1)]
public class AreaData : ScriptableObject {
    [Header("Area Settings")]
    public string areaName;

    [Header("Area Bound")]
    public List<Vector2> edgePoint;
    public float maxX, minX;
    public float maxY, minY;

    [Header("Section JSON Data path")]
    public string sectionDataFolderPath;
    public string mainSectionDataFolderPath;
}