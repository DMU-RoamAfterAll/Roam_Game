using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;

public class CreateAreaAssets : MonoBehaviour {
    public static string folderPath;

    [MenuItem("Tools/Create Area Data Assets")]
    public static void CreateAreaDataAssets() {
        folderPath = GameDataManager.Data.areaDataFolderPath;
        Debug.Log("folderPath : " + folderPath);

        if(!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");

        foreach(string jsonFilePath in jsonFiles) {
            string jsonContent = File.ReadAllText(jsonFilePath);
            AreaData jsonData = JsonConvert.DeserializeObject<AreaData>(jsonContent);

            string areaName = Path.GetFileNameWithoutExtension(jsonFilePath);
            jsonData.areaName = areaName;

            string assetPath = $"{folderPath}/{areaName}Data.asset";
            
            AreaData existingData = AssetDatabase.LoadAssetAtPath<AreaData>(assetPath);
            if (existingData == null) {
                AssetDatabase.CreateAsset(jsonData, assetPath);
                Debug.Log($"Created new AreaData: {areaName}");
            } else {
                EditorUtility.CopySerialized(jsonData, existingData);
                Debug.Log($"Updated existing AreaData: {areaName}");
            }

            GameObject areaObject = new GameObject(areaName);
            AreaDataManager areaManager = areaObject.AddComponent<AreaDataManager>();
            areaManager.areaData = jsonData;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log("All AreaData assets created succesfully!");
    }
}