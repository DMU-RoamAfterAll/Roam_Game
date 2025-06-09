using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GameData", menuName = "ScriptableObject/GameData", order = 1)]

public class GameData : ScriptableObject {
    [Header("GameData")]
    public string playerName;
    public int seed;

    [Header("AreaAssetData")]
    public int areaNumber;
    public int riverHeight;
    public float maxRadius;
    public string areaAssetDataFolderPath;

    [Header("SectionData")]
    public float initialMinDistance;
    public float initialMaxDistance;

    [Header("Prefab")]
    public GameObject edgePrefab;
    public GameObject sectionPrefab;
    public GameObject mainSectionPrefab;
    public GameObject linkSectionPrefab;
}