using UnityEngine;

public enum TagName {
    Area,
    Tutorial
}

public static class Tag {
    public static string Area => TagName.Area.ToString();
    public static string Tutorial => TagName.Tutorial.ToString();
}