using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class GameDataManager : MonoBehaviour {
    public static GameDataManager Instance { get; private set; }
    public GameObject Player;
    public GameObject originSection;
    public string playerLocate;

    public GameData gameData;

    public static GameData Data => Instance.gameData;

    public List<GameObject> areaObjects;
    public List<GameObject> sections;
    public List<GameObject> mainSections;


    void Awake() {
        if (Instance != null && Instance != this)  {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void Start() {
        if (gameData == null) {
            Debug.LogError("GameData is None");
            return;
        }

        Player = GameObject.FindGameObjectWithTag(Tag.Player);
        originSection = GameObject.FindGameObjectWithTag(Tag.Origin);
        playerLocate = "origin";

        gameData.playerName = "Potato";
        gameData.seed = 54321;
        gameData.initialMinDistance = 10f;
        gameData.initialMaxDistance = 12f;
        gameData.maxRadius = 30f;

        gameData.areaAssetDataFolderPath = "Assets/Resources/AreaAssetData";

        gameData.areaNumber = (Directory.GetFiles(gameData.areaAssetDataFolderPath, "*.json")).Length;
        gameData.riverHeight = 5;

        gameData.sightObjects = GameObject.Find("SightObjects");

        Application.targetFrameRate = 60;
    }
}