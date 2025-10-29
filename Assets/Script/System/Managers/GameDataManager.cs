using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System;

public class GameDataManager : MonoBehaviour {
    public static GameDataManager Instance { get; private set; }
    public GameData gameData;
    public static GameData Data => Instance.gameData;

    public string sectionPath;

    const string KEY_SEED = "gd_seed";
    const string KEY_TUTORIAL = "gd_tutorial";

    void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
        Instance = this;

        if (gameData == null) { Debug.LogError("GameData is None"); return; }

        gameData.baseUrl = "http://125.176.246.14";
        gameData.playerName = "Potato";
        Application.targetFrameRate = 60;

        // ★ 풀/리컴파일 등으로 재시작됐을 때 에셋 기본값이 덮지 않도록 PlayerPrefs에서 복원
        RestoreCoreFromPrefs();
    }

    void RestoreCoreFromPrefs() {
        if (PlayerPrefs.HasKey(KEY_SEED))
            gameData.seed = PlayerPrefs.GetInt(KEY_SEED);
        // tutorialClear는 bool → int로 저장
        if (PlayerPrefs.HasKey(KEY_TUTORIAL))
            gameData.tutorialClear = PlayerPrefs.GetInt(KEY_TUTORIAL) == 1;
    }

    void PersistCoreToPrefs() {
        PlayerPrefs.SetInt(KEY_SEED, gameData.seed);
        PlayerPrefs.SetInt(KEY_TUTORIAL, gameData.tutorialClear ? 1 : 0);
        PlayerPrefs.Save();
    }

    // === 요청했던 헬퍼들 유지 + 지속 저장 추가 ===
    public void NewSeed() {
        gameData.seed = Guid.NewGuid().GetHashCode();
        PersistCoreToPrefs();
    }

    public void ContinueSeed(int serverSeed) {
        gameData.seed = serverSeed;
        PersistCoreToPrefs();
    }

    public void SetTutorialClear(bool value) {
        gameData.tutorialClear = value;
        PersistCoreToPrefs();
    }
    
    #if UNITY_EDITOR
    [Header("설정")]
    public KeyCode captureKey = KeyCode.F12;  // 캡처 키
    public string saveFolder = "Screenshots"; // 저장 폴더 이름

    void Update()
    {
        // 특정 키 눌렀을 때 캡처 실행
        if (Input.GetKeyDown(captureKey))
        {
            CaptureScreenshot();
        }
    }

    void CaptureScreenshot()
    {
        // 저장 폴더 경로 생성 (프로젝트 폴더 기준)
        string folderPath = Path.Combine(Application.dataPath, "..", saveFolder);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        // 저장 파일명 (예: Screenshot_2025-10-27_21-44-12.png)
        string fileName = $"Screenshot_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        string filePath = Path.Combine(folderPath, fileName);

        // 캡처 실행
        ScreenCapture.CaptureScreenshot(filePath);
        Debug.Log($"✅ 스크린샷 저장 완료: {filePath}");
    }
    #endif
}