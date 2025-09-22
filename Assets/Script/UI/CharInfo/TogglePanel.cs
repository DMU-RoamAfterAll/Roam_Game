using UnityEngine;

public class TogglePanel : MonoBehaviour
{
    public GameObject body;

    public void Toggle()
    {
        if (body != null)
            body.SetActive(!body.activeSelf);
    }
}
