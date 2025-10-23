using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScrollPosKeeper : MonoBehaviour
{
    [SerializeField] ScrollRect scroll;
    const string KEY = "MyScrollPos";

    void OnDisable()
    {
        PlayerPrefs.SetFloat(KEY + "_x", scroll.normalizedPosition.x);
        PlayerPrefs.SetFloat(KEY + "_y", scroll.normalizedPosition.y);
        PlayerPrefs.Save();
    }

    void OnEnable()
    {
        StartCoroutine(RestoreAfterLayout());
    }

    IEnumerator RestoreAfterLayout()
    {
        yield return null; 
        var x = PlayerPrefs.GetFloat(KEY + "_x", 0f);
        var y = PlayerPrefs.GetFloat(KEY + "_y", 1f);
        scroll.normalizedPosition = new Vector2(x, y);
    }
}
