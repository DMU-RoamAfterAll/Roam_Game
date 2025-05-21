using UnityEngine;

public class AreaDataManager : MonoBehaviour {
    public AreaData areaData;

    void Start() {
        if (areaData == null) {
            Debug.LogError("AreaData is not assigned!");
            return;
        }

        RandomSectionSpawner randomSectionSpawner = this.gameObject.AddComponent<RandomSectionSpawner>();
        
        CreateEdge();
    }

    void CreateEdge() {
        foreach(Vector2 point in  areaData.edgePoint) {
            GameObject go = Instantiate(GameDataManager.Data.edgePrefab, point, Quaternion.identity);
            go.transform.SetParent(this.gameObject.transform);
        }
    }

    void CreateRandomSection() {
        RandomSectionSpawner randomSectionSpawner = this.gameObject.AddComponent<RandomSectionSpawner>();
    }
}