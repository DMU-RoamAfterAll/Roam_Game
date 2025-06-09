using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using Newtonsoft.Json;

public class CreateAreaAssets : MonoBehaviour {
    public string folderPath;

    void Start() {
#if UNITY_EDITOR
        CreateAreaDataAssets();
#endif
    }

#if UNITY_EDITOR
    public void CreateAreaDataAssets() {
        folderPath = GameDataManager.Data.areaAssetDataFolderPath;
        Debug.Log("folderPath : " + folderPath);

        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");

        foreach (string jsonFilePath in jsonFiles) {
            string jsonContent = File.ReadAllText(jsonFilePath);
            string areaName = Path.GetFileNameWithoutExtension(jsonFilePath);
            string assetPath = $"{folderPath}/{areaName}Data.asset";

            AreaAsset jsonData = ScriptableObject.CreateInstance<AreaAsset>();
            jsonData.name = areaName + "Data";
            JsonConvert.PopulateObject(jsonContent, jsonData);

            jsonData.areaName = areaName;

            AreaAsset existingData = AssetDatabase.LoadAssetAtPath<AreaAsset>(assetPath);

            if (existingData == null) {
                AssetDatabase.CreateAsset(jsonData, assetPath);
                Debug.Log($"Created new AreaAsset: {areaName}");
            }
            else {
                EditorUtility.CopySerialized(jsonData, existingData);
                Debug.Log($"Updated existing AreaAsset: {areaName}");
            }

            GameObject areaObject = new GameObject(areaName);
            areaObject.transform.SetParent(this.transform);
            GameDataManager.Instance.areaObjects.Add(areaObject);

            if (areaName == "Tutorial") areaObject.tag = Tag.Tutorial;
            else areaObject.tag = Tag.Area;

            AreaAssetManager areaManager = areaObject.AddComponent<AreaAssetManager>();
            areaManager.areaAsset = jsonData;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log("All AreaAsset created succesfully!");
    }
#endif
}