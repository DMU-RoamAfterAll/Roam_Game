using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GameData", menuName = "ScriptableObject/GameData", order = 1)]

public class GameData : ScriptableObject {
    public string playerName;
    public int seed;
    public int areaCount;

    public float initialMinDistance;
    public float initialMaxDistance;
    public float maxRadius;

    public string areaDataFolderPath;

    public GameObject edgePrefab;
    public GameObject sectionPrefab;
    public GameObject mainSectionPrefab;
}