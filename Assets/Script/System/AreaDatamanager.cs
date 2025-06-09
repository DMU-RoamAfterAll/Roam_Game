using UnityEngine;

public class AreaDataManager : MonoBehaviour {
    public AreaData areaData;

    void Start() {
        if (areaData == null) {
            Debug.LogError("AreaData is not assigned!");
            return;
        }

        RandomSectionSpawner randomSectionSpawner = this.gameObject.AddComponent<RandomSectionSpawner>();
    }
}