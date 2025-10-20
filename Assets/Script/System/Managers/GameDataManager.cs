using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System;

///GameData에 있는 수치를 직접적으로 조정 및 대입
public class GameDataManager : MonoBehaviour {
    public static GameDataManager Instance { get; private set; } //씬에서 모두 접근 가능하도록 Instance화

    public GameData gameData; //만들어둔 asset 사용

    public static GameData Data => Instance.gameData; //asset에 접근 시 사용하는 변수이름

    public string sectionPath;

    ///Instance
    void Awake() {
        if (Instance != null && Instance != this)  {
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(this.gameObject);

        Instance = this;

        if (gameData == null) {
            Debug.LogError("GameData is None");
            return;
        }

        gameData.baseUrl = "http://125.176.246.14";
        //다른 곳에서 baseUrl쓸때 포트번호는 붙혀서 쓰시기를 바랍니다
        gameData.playerName = "Potato";
        gameData.seed = Guid.NewGuid().GetHashCode();

        Application.targetFrameRate = 60;
    }
}