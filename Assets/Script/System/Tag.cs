using UnityEngine;

public enum TagName {
    Area,
    Tutorial,
    Section,
    LinkSection,
    MainSection
}

public static class Tag {
    public static string Area => TagName.Area.ToString();
    public static string Tutorial => TagName.Tutorial.ToString();
    public static string Section => TagName.Section.ToString();
    public static string LinkSection => TagName.LinkSection.ToString();
    public static string MainSection => TagName.MainSection.ToString();
}