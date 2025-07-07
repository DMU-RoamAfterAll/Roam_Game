using UnityEngine;

public class LinkSection : MonoBehaviour {
    public GameObject linkObj;
    public GameObject linkObjPrefab;
    public float minDist;

    void Start() {
        linkObjPrefab = GameDataManager.Data.linkSectionPrefab;
        minDist = GameDataManager.Data.initialMinDistance;

        SpawnLinkObject();
    }

    public void SpawnLinkObject() {
        if (linkObj == null || linkObjPrefab == null) {
            Debug.LogWarning("Link target or prefab is missing.");
            return;
        }

        Vector2 direction = (linkObj.transform.position - transform.position).normalized;
        Vector2 spawnPos = (Vector2)transform.position + direction * minDist;

        GameObject go = Instantiate(linkObjPrefab, spawnPos, Quaternion.identity);
        go.transform.SetParent(this.transform);
        this.gameObject.GetComponent<SectionData>().linkSections.Add(go);
        go.GetComponent<LinkPosition>().linkedObject = linkObj;
        GameDataManager.Instance.linkSections.Add(go);
    }
}