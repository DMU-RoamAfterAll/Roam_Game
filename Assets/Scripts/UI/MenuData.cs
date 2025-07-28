[System.Serializable]
public class ActionData
{
    public string Image;
    public string[] Get; // "Get": ["item code name", count]
}

[System.Serializable]
public class MenuItem
{
    public string id;
    public string label;
    public string[] text;
    public ActionData[] action;
}

[System.Serializable]
public class MenuNode
{
    public MenuItem[] value;
    public string next;
}

[System.Serializable]
public class TextNode
{
    public string[] value;
    public string next;
    public ActionData[] action;
}

[System.Serializable]
public class RootNode
{
    public TextNode Text1;
    public MenuNode Menu1;
    public TextNode Text2;
}

// dummy