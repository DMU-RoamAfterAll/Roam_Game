using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AreaAsset", menuName = "ScriptableObject/AreaData", order = 1)]
public class AreaAsset : ScriptableObject {
    [Header("Area Settings")]
    public string areaName;

    [Header("Section JSON Data path")]
    public string sectionDataFolderPath;
    public string mainSectionDataFolderPath;
}