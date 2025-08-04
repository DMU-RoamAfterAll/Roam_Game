using UnityEngine;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
#else
using UnityEngine.Networking;
using System.Collections;
#endif

public class CreateAreaAssets : MonoBehaviour {
    public string folderPath;

    public string[] jsonUrls;

    async void Start() {
        jsonUrls = new [] {
            $"{GameDataManager.Data.baseUrl}/CNWV/Resources/AreaAssetData/Area_01.json",
            $"{GameDataManager.Data.baseUrl}/CNWV/Resources/AreaAssetData/Area_02.json",
            $"{GameDataManager.Data.baseUrl}/CNWV/Resources/AreaAssetData/Area_03.json",
            $"{GameDataManager.Data.baseUrl}/CNWV/Resources/AreaAssetData/Area_04.json",
            $"{GameDataManager.Data.baseUrl}/CNWV/Resources/AreaAssetData/Area_05.json",
            $"{GameDataManager.Data.baseUrl}/CNWV/Resources/AreaAssetData/Tutorial.json"
        };

        #if UNITY_EDITOR
            folderPath = MapSceneDataManager.mapData.areaAssetDataFolderPath;
        #else
            folderPath = Path.Combine(Application.persistentDataPath, "AreaAssetData");
        #endif

        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        #if UNITY_EDITOR
            await CreateAreaDataAssets();
        #else
            StartCoroutine(CreateAreaAssetsRunTime());
        #endif
    }

#if UNITY_EDITOR
    public async Task CreateAreaDataAssets() {
        using (var client = new HttpClient()) {
            foreach (var url in jsonUrls) {
                try {
                    string jsonContent = await client.GetStringAsync(url);

                    string areaName = Path.GetFileNameWithoutExtension(new Uri(url).LocalPath);
                    string assetPath = $"{folderPath}/{areaName}Data.asset";

                    AreaAsset jsonData = ScriptableObject.CreateInstance<AreaAsset>();
                    jsonData.name = areaName + "Data";
                    JsonConvert.PopulateObject(jsonContent, jsonData);
                    jsonData.areaName = areaName;

                    AreaAsset existing = AssetDatabase.LoadAssetAtPath<AreaAsset>(assetPath);
                    if (existing == null) {
                        AssetDatabase.CreateAsset(jsonData, assetPath);
                        Debug.Log($"Created new AreaAsset : {areaName}");
                    }
                    else {
                        EditorUtility.CopySerialized(jsonData, existing);
                        Debug.Log($"Updated existing AreaAsset : {areaName}");
                    }

                    GameObject areaObject = new GameObject(areaName);
                    areaObject.transform.SetParent(this.transform);

                    if (areaName == "Tutorial") areaObject.tag = Tag.Tutorial;
                    else areaObject.tag = Tag.Area;


                    MapSceneDataManager.Instance.areaObjects.Add(areaObject);
                    var mgr = areaObject.AddComponent<AreaAssetManager>();
                    mgr.areaAsset = jsonData;
                }
                catch (Exception ex) {
                    Debug.LogError($"Failed to load or parse JSON from {url} : {ex}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All remote AreaAssets created successfully!");
    }
#endif

#if !UNITY_EDITOR

    private IEnumerator CreateAreaAssetsRunTime() {
        Debug.Log("[CreateAreaAssets] Coroutine started, total URLs = " + jsonUrls.Length);

        foreach (var url in jsonUrls) {
            Debug.Log($"[CreateAreaAssets] Attempting download: {url}");
            using (var www = UnityWebRequest.Get(url)) {
                yield return www.SendWebRequest();

                Debug.Log($"[CreateAreaAssets] Download result: {www.result}, error: {www.error}");

                if (www.result != UnityWebRequest.Result.Success) {
                    Debug.LogWarning($"[CreateAreaAssets] Download failed: {url}");
                    continue;
                }

                string jsonText = www.downloadHandler.text;
                Debug.Log($"[CreateAreaAssets] Received JSON ({url}), length = {jsonText.Length}\n{jsonText.Substring(0, Mathf.Min(200, jsonText.Length))}");

                // Parse into ScriptableObject
                var jsonData = ScriptableObject.CreateInstance<AreaAsset>();
                JsonConvert.PopulateObject(jsonText, jsonData);
                jsonData.areaName = Path.GetFileNameWithoutExtension(new Uri(url).LocalPath);

                // Check parsed fields
                Debug.Log($"[CreateAreaAssets] Parsed data: areaName = {jsonData.areaName}");

                // Create runtime GameObject
                var go = new GameObject(jsonData.areaName);
                go.transform.SetParent(this.transform);
                go.tag = (jsonData.areaName == "Tutorial") ? Tag.Tutorial : Tag.Area;

                var mgr = go.AddComponent<AreaAssetManager>();
                mgr.areaAsset = jsonData;

                GameDataManager.Instance.areaObjects.Add(go);

                Debug.Log($"[CreateAreaAssets] Created AreaObject: {go.name}, total count = {GameDataManager.Instance.areaObjects.Count}");
            }
        }

        Debug.Log("[CreateAreaAssets] Coroutine finished");
    }

#endif
}