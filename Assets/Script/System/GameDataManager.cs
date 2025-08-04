using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;

///GameData에 있는 수치를 직접적으로 조정 및 대입
public class GameDataManager : MonoBehaviour {
    public static GameDataManager Instance { get; private set; } //씬에서 모두 접근 가능하도록 Instance화
    public GameObject Player;
    public GameObject originSection; //Player가 처음 위치하는 Section
    public string playerLocate; //Player가 어느 Section에 위치하고 있는지

    public GameData gameData; //만들어둔 asset 사용

    public static GameData Data => Instance.gameData; //asset에 접근 시 사용하는 변수이름

    ///게임 내 Area, Section, MainSection 오브젝트 List화
    public List<GameObject> areaObjects;
    public List<GameObject> sections;
    public List<GameObject> mainSections;

    public StepManager stepManagerUI;

    public string baseUrl = "http://125.176.246.14";

    ///Instance
    void Awake() {
        if (Instance != null && Instance != this)  {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;

        if (gameData == null) {
            Debug.LogError("GameData is None");
            return;
        }

        Player = GameObject.FindGameObjectWithTag(Tag.Player);
        originSection = GameObject.FindGameObjectWithTag(Tag.Origin);
        stepManagerUI = GameObject.FindGameObjectWithTag(Tag.StepUI).GetComponent<StepManager>();
        playerLocate = "origin";

        gameData.playerName = "Potato";
        gameData.seed = 54321;
        gameData.isMapSetUp = false;
        gameData.initialMinDistance = 10f;
        gameData.initialMaxDistance = 12f;
        gameData.maxRadius = 30f;

        gameData.areaAssetDataFolderPath = "Assets/Resources/AreaAssetData";

        gameData.areaNumber = Regex.Matches(
            new HttpClient()
                .GetStringAsync($"{baseUrl}/CNWV/Resources/AreaAssetData/")
                .Result,
            @"href\s*=\s*""[^""]+\.json""",
            RegexOptions.IgnoreCase
        ).Count;

        gameData.riverHeight = 5;

        gameData.sightObjects = GameObject.Find("SightObjects");

        Application.targetFrameRate = 60;
    }
}