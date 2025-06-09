using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;

public class CreateAreaAssets : MonoBehaviour {
    public string folderPath;

    void Start() {
        CreateAreaDataAssets();
    }

    public void CreateAreaDataAssets() {
        folderPath = GameDataManager.Data.areaDataFolderPath;
        Debug.Log("folderPath : " + folderPath);

        if(!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");

        foreach(string jsonFilePath in jsonFiles) {
            string jsonContent = File.ReadAllText(jsonFilePath);
            string areaName = Path.GetFileNameWithoutExtension(jsonFilePath);
            string assetPath = $"{folderPath}/{areaName}Data.asset";

            AreaData jsonData = ScriptableObject.CreateInstance<AreaData>();
            jsonData.name = areaName + "Data";
            JsonConvert.PopulateObject(jsonContent, jsonData);

            jsonData.areaName = areaName;
            
            AreaData existingData = AssetDatabase.LoadAssetAtPath<AreaData>(assetPath);
    
            if (existingData == null) {
                AssetDatabase.CreateAsset(jsonData, assetPath);
                Debug.Log($"Created new AreaData: {areaName}");
            }
            else {
                EditorUtility.CopySerialized(jsonData, existingData);
                Debug.Log($"Updated existing AreaData: {areaName}");
            }

            GameObject areaObject = new GameObject(areaName);
            areaObject.transform.SetParent(this.transform);
            
            if(areaName == "Tutorial") areaObject.tag = Tag.Tutorial;
            else areaObject.tag = Tag.Area;

            AreaDataManager areaManager = areaObject.AddComponent<AreaDataManager>();
            areaManager.areaData = jsonData;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log("All AreaData assets created succesfully!");
    }
}