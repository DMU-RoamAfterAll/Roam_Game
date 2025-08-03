using UnityEngine;
using System.Collections.Generic;

///게임 진행에 필요한 구체적인 데이터들
[CreateAssetMenu(fileName = "GameData", menuName = "ScriptableObject/GameData", order = 1)]
public class GameData : ScriptableObject {
    [Header("GameData")]
    public string baseUrl;
    public string playerName;
    public int seed;
}