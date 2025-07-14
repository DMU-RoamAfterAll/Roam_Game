using UnityEngine;
using System.Collections.Generic;

///생성할 구역의 정보들
[CreateAssetMenu(fileName = "AreaAsset", menuName = "ScriptableObject/AreaData", order = 1)]
public class AreaAsset : ScriptableObject {
    [Header("Area Settings")]
    public string areaName;

    [Header("Section JSON Data path")]
    public string sectionDataFolderPath; //생성할 section의 정보가 들어있는 폴더 경로
    public string mainSectionDataFolderPath; //생성할 mainSection의 정보가 들어있는 폴더 경로
}