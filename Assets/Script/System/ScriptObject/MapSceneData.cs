using UnityEngine;
using System.Collections.Generic;

///게임 진행에 필요한 구체적인 데이터들
[CreateAssetMenu(fileName = "MapSceneData", menuName = "ScriptableObject/MapSceneData", order = 1)]
public class MapSceneData : ScriptableObject {
    [Header("GameData")]
    public bool isMapSetUp;

    [Header("AreaAssetData")]
    public int areaNumber; //구역의 개수
    public int riverHeight; //구역을 가르는 공간의 크기
    public float maxRadius; //Section이 생성되는 최대 범위 (원형 기준)
    public string areaAssetDataFolderPath; //areaAsset이 위치한 경로

    [Header("SectionData")]
    public float initialMinDistance; //section과의 최소 거리
    public float initialMaxDistance; //section과의 최대 거리

    ///Map Scene에 쓰이는 Prefab모음
    [Header("Prefab")]
    public GameObject sectionPrefab;
    public GameObject mainSectionPrefab;
    public GameObject linkSectionPrefab;
    public GameObject sightSectionPrefab;
    public GameObject riverSectionPrefab;
    
    [Header("Object")]
    public GameObject sightObjects; //시야 오브젝트를 모아두는 오브젝트
}