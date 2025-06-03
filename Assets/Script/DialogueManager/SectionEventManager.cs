using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

//-------------------------------------------------------
// ** Json 데이터 클래스 구조 **

[System.Serializable]
public class BaseNode //공통 노드
{
    public string next; //텍스트 이동을 위한 키값
}

[System.Serializable]
public class TextNode : BaseNode //본문 노드
{
    public List<string> value; //텍스트 본문
}

[System.Serializable]
public class MenuOption //조사 선택지 노드
{
	public string id; //선택지 키값
	public string label; //조사 선택지
	public List<string> text; //선택지 출력 결과
	public List<Dictionary<string, object>> action; //아이템 습득 및 손실
}
[System.Serializable]
public class MenuNode : BaseNode //조사 노드
{
    public List<MenuOption> value; //조사 선택지 노드가 담긴 리스트
}

//-------------------------------------------------------

public class SectionEventManager : MonoBehaviour
{
    public string jsonFileName = ""; //불러올 Json파일 이름
    public string jsonFolderPath =
    "SectionData\\SectionEvent"; //Json폴더가 담긴 파일의 경로
    private Dictionary<string, object> sectionData = new Dictionary<string, object>();
    //파싱된 Json데이터

    void Start()
    {
        LoadJson(jsonFileName); //Json파일 로드

        //Json테스트 출력
        TextNode testjson = sectionData["Text1"] as TextNode;
        Debug.Log(testjson.value[0]);
    }

    void Update()
    {

    }

    //Json Event 데이터 파일 로드
    public void LoadJson(string jsonFileName)
    {
        string filePath = Path.Combine(jsonFolderPath, jsonFileName);
        TextAsset jsonFile = Resources.Load<TextAsset>(filePath); //Json 파일 로드
        if (jsonFile == null)
        {
            Debug.LogError($"[SectionEventManager] 파일을 찾을 수 없음: {jsonFileName}.json");
            return;
        }
        //Json파일에서 텍스트 데이터를 가져와 Json객체 구조로 변경
        string jsonText = jsonFile.text;
		JObject root = JObject.Parse(jsonText);

        // 각 노드를 순회하며 타입에 따라 파싱
        foreach (var pair in root)
        {
            string key = pair.Key;
            JObject nodeObj = (JObject)pair.Value;

            //본문과 선택지 여부에 따라 저장
            if (key.StartsWith("Menu"))
            {
                MenuNode menu = nodeObj.ToObject<MenuNode>();
                sectionData[key] = menu; //키가 존재하지 않으면 동적 추가
            }
            else if (key.StartsWith("Text"))
            {
                TextNode text = nodeObj.ToObject<TextNode>();
                sectionData[key] = text; //키가 존재하지 않으면 동적 추가
            }
            else
            {
                Debug.LogWarning($"[SectionEventManager] 인식할 수 없는 노드: {key}");
            }
        }
        Debug.Log("Reading File : " + jsonFileName + ".json"); //파일 로드 확인 로그
    }
}
