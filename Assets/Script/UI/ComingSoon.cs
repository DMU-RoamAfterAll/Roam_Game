using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ComingSoon : MonoBehaviour {
    public GameObject content;

    void OnEnable() {
        content.SetActive(false);
    }
    
    public void SeeYou() {
        StartCoroutine(TextSet());     
    }

    IEnumerator TextSet() {
        content.SetActive(true);

        yield return new WaitForSeconds(1f);

        content.SetActive(false);
    }

}
