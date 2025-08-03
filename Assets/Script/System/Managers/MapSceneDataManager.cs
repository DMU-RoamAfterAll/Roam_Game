using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;

public class MapSceneDataManager : MonoBehaviour {
    public static MapSceneDataManager Instance { get; private set; }

    public MapSceneData mapSceneData;
    
    public GameObject Player;
    public GameObject originSection; //Player가 처음 위치하는 Section
    public string playerLocate; //Player가 어느 Section에 위치하고 있는지

    public static MapSceneData mapData => Instance.mapSceneData; //asset에 접근 시 사용하는 변수이름

    public List<GameObject> areaObjects;
    public List<GameObject> sections;
    public List<GameObject> mainSections;

    public StepManager stepManagerUI;

    ///Instance
    void Awake() {
        if (Instance != null && Instance != this)  {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;

        if (mapData == null) {
            Debug.LogError("MapSceneData is None");
            return;
        }

        Player = GameObject.FindGameObjectWithTag(Tag.Player);
        originSection = GameObject.FindGameObjectWithTag(Tag.Origin);
        stepManagerUI = GameObject.FindGameObjectWithTag(Tag.StepUI).GetComponent<StepManager>();
        playerLocate = "origin";

        mapSceneData.areaNumber = Regex.Matches(
            new HttpClient()
                .GetStringAsync($"{GameDataManager.Data.baseUrl}/CNWV/Resources/AreaAssetData/")
                .Result,
            @"href\s*=\s*""[^""]+\.json""",
            RegexOptions.IgnoreCase
        ).Count;

        mapSceneData.sightObjects = GameObject.Find("SightObjects");
    }
}