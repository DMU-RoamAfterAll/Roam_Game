using UnityEngine;

public enum TagName {
    Area,
    Tutorial,
    Section,
    VirtualSection,
    MainSection,
    Player,
    Origin,
    Sight
}

public static class Tag {
    public static string Area => TagName.Area.ToString();
    public static string Tutorial => TagName.Tutorial.ToString();
    public static string Section => TagName.Section.ToString();
    public static string VirtualSection => TagName.VirtualSection.ToString();
    public static string MainSection => TagName.MainSection.ToString();
    public static string Player => TagName.Player.ToString();
    public static string Origin => TagName.Origin.ToString();
    public static string Sight => TagName.Sight.ToString();
}