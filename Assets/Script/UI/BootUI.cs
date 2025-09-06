using UnityEngine;

public class BootUI : MonoBehaviour
{
    public void OnClickEnterMap()
    {
        SwitchSceneManager.Instance.EnterBaseFromBoot();
    }
}