using UnityEngine;
using UnityEngine.SceneManagement;

public class GameDataManager : MonoBehaviour {
    public static GameDataManager Instance { get; private set; }
    public GameData gameData;

    public static GameData Data => Instance.gameData;

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
        gameData.areaCount = 1;
        gameData.initialMinDistance = 3f;

        gameData.areaDataFolderPath = "Assets/Resources/AreaData";
    }
}