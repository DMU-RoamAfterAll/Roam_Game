using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class LocalCatalog : MonoBehaviour
{
    [Header("Assign JSON (TextAsset) or keep empty to load from Resources")]
    [SerializeField] private TextAsset itemsJson;   // 예: Resources/Catalog/items.json
    [SerializeField] private TextAsset weaponsJson; // 예: Resources/Catalog/weapons.json
    [SerializeField] private TextAsset skillsJson;  // 예: Resources/Catalog/skills.json

    [Header("Options")]
    [SerializeField] private bool autoLoadOnEnable = true;
    [SerializeField] private bool debugLog = false;

    private readonly Dictionary<string,string> itemNames   = new();
    private readonly Dictionary<string,string> weaponNames = new();
    private readonly Dictionary<string,string> skillNames  = new();

    private bool loaded;

#if UNITY_2023_1_OR_NEWER
    private T AutoFind<T>() where T : Object => FindFirstObjectByType<T>();
#else
    private T AutoFind<T>() where T : Object => FindObjectOfType<T>();
#endif

    private void OnEnable()
    {
        if (autoLoadOnEnable) LoadAll();
    }

    public void LoadAll()
    {
        if (loaded) return;

        // 필요 시 Resources에서 자동 로드
        if (itemsJson   == null) itemsJson   = Resources.Load<TextAsset>("StoryGameData/CommonData/item");
        if (weaponsJson == null) weaponsJson = Resources.Load<TextAsset>("StoryGameData/CommonData/weapon");
        if (skillsJson  == null) skillsJson  = Resources.Load<TextAsset>("StoryGameData/CommonData/skill");

        itemNames.Clear();
        weaponNames.Clear();
        skillNames.Clear();

        // JSON은 사용자가 제공한 포맷(배열 루트) 가정
        ParseCodeNameArray(itemsJson,   itemNames);
        ParseCodeNameArray(weaponsJson, weaponNames);
        ParseCodeNameArray(skillsJson,  skillNames);

        loaded = true;

        if (debugLog)
        {
            Debug.Log($"[LocalCatalog] Items:{itemNames.Count} Weapons:{weaponNames.Count} Skills:{skillNames.Count}");
        }
    }

    private void ParseCodeNameArray(TextAsset ta, Dictionary<string,string> dict)
    {
        if (ta == null || string.IsNullOrEmpty(ta.text)) return;
        try
        {
            var arr = JArray.Parse(ta.text);
            foreach (var el in arr)
            {
                var code = el.Value<string>("code");
                var name = el.Value<string>("name");
                if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(name))
                    dict[code.Trim()] = name.Trim();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LocalCatalog] Parse failed: {ta.name}\n{e.Message}");
        }
    }

    // ---- Public API ----
    public string GetItemName(string code)   => TryGet(itemNames, code);
    public string GetWeaponName(string code) => TryGet(weaponNames, code);
    public string GetSkillName(string code)  => TryGet(skillNames, code);

    private string TryGet(Dictionary<string,string> dict, string code)
    {
        if (string.IsNullOrEmpty(code)) return code;
        code = code.Trim();
        return dict.TryGetValue(code, out var n) ? n : code; // 없으면 코드 그대로
    }
}