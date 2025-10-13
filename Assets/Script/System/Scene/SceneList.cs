using UnityEngine;

public enum SceneName {
    APIScene,
    LoginScene,
    MapScene,
    RegisterScene,
    StoryScene,
    MissionScene
}

public static class SceneList {
    public static string Api => SceneName.APIScene.ToString();
    public static string Login => SceneName.LoginScene.ToString();
    public static string Map => SceneName.MapScene.ToString();
    public static string Register => SceneName.RegisterScene.ToString();
    public static string Story => SceneName.StoryScene.ToString();
    public static string Mission => SceneName.MissionScene.ToString();
}