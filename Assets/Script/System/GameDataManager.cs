using UnityEngine;

public class GameDataManager : MonoBehaviour {
    public GameData gameData;

    void Start() {
        if(gameData == null) {
            Debug.LogError("GameData is None");
            return;
        }

        gameData.playerName = "Potato";
        gameData.seed = 12345;
        gameData.areaCount = 1;
    }   
}