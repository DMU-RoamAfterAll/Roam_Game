using System.IO;
using UnityEngine;

[System.Serializable]
public class PlayerData {
    public string playerName;
    public int originSeed;
}

public class SaveLoadManager : MonoBehaviour {
    private string saveFilePath;
    public string path;

    void Start() {
        DontDestroyOnLoad(this.gameObject);

        saveFilePath = Application.persistentDataPath;

        Debug.Log(saveFilePath);

        path = Path.Combine(saveFilePath, "playerDataTest.json");
    }

    public void SaveData() {
        PlayerData playerData = new PlayerData {
            playerName = GameDataManager.Data.playerName,
            originSeed = GameDataManager.Data.seed
        };

        SaveFile(playerData);
    }

    public void LoadData() {
        if(File.Exists(path)) {
            string jsonData = File.ReadAllText(path);
            PlayerData data = JsonUtility.FromJson<PlayerData>(jsonData);
            
            Debug.Log(data.playerName);
            Debug.Log("seed : " + data.originSeed);
        }
    }

    void SaveFile(PlayerData data) {
        string jsonData = JsonUtility.ToJson(data, true);
        Debug.Log(" JSON Data : \n" + jsonData);

        File.WriteAllText(path, jsonData);

        Debug.Log("Data saved to  " + path);
    }
}