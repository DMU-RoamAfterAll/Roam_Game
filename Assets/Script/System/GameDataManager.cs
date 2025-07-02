using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class GameDataManager : MonoBehaviour {
    public static GameDataManager Instance { get; private set; }
    public GameData gameData;

    public static GameData Data => Instance.gameData;

    public List<GameObject> areaObjects;
    public List<GameObject> sections;
    public List<GameObject> mainSections;
    public List<GameObject> linkSections;


    void Awake() {
        if (Instance != null && Instance != this)  {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);


        #region 임시 테스트용
        
        linkTrigger = false;
        #endregion
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

    #region 임시 테스트용 코드들
    
    bool linkTrigger;

    void Update() {
        if(Input.GetKeyDown("l")) {
            ControlLinkSection();
        }
    }

    void ControlLinkSection() {
        if(linkSections != null) {
            if(!linkTrigger) {
                foreach(var link in linkSections) {
                    link.SetActive(true);
                    linkTrigger = true;
                }
            }
            else {
                foreach(var link in linkSections) {
                    link.SetActive(false);
                    linkTrigger = false;
                }
            }
        }
    }

    #endregion
}