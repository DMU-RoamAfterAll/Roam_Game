using UnityEngine;

public class AreaAssetManager : MonoBehaviour {
    public AreaAsset areaAsset;

    void Start() {
        if (areaAsset == null) {
            Debug.LogError("AreaData is not assigned!");
            return;
        }

        RandomSectionSpawner randomSectionSpawner = this.gameObject.AddComponent<RandomSectionSpawner>();
    }
}