using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class GameDataManager : MonoBehaviour {
    public static GameDataManager Instance { get; private set; }
    public GameData gameData;

    public static GameData Data => Instance.gameData;

    public List<GameObject> areaObjects;

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

        gameData.playerName = "Potato";
        gameData.seed = 54321;
        gameData.initialMinDistance = 5f;
        gameData.initialMaxDistance = 8f;
        gameData.maxRadius = 30f;

        gameData.areaAssetDataFolderPath = "Assets/Resources/AreaAssetData";

        gameData.areaNumber = (Directory.GetFiles(gameData.areaAssetDataFolderPath, "*.json")).Length;
        gameData.riverHeight = 5;

        Application.targetFrameRate = 60;
    }
}