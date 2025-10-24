using System.Linq;
using UnityEngine;
using TMPro;

public class InventoryTextBinder : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InventoryManager inventory;   // 비워두면 자동 탐색
    [SerializeField] private LocalCatalog     catalog;     // 비워두면 자동 탐색 후 LoadAll()

    [Header("Text Targets")]
    [SerializeField] private TextMeshProUGUI itemsText;
    [SerializeField] private TextMeshProUGUI weaponsText;
    [SerializeField] private TextMeshProUGUI skillsText;

    [Header("Options")]
    [SerializeField] private bool   skipZeroAmount   = true;
    [SerializeField] private string emptyPlaceholder = "-";

#if UNITY_2023_1_OR_NEWER
    private T AutoFind<T>() where T : Object => FindFirstObjectByType<T>();
#else
    private T AutoFind<T>() where T : Object => FindObjectOfType<T>();
#endif

    private void Awake()
    {
        if (inventory == null) inventory = AutoFind<InventoryManager>();
        if (catalog   == null) catalog   = AutoFind<LocalCatalog>();
    }

    private void OnEnable()
    {
        if (catalog != null) catalog.LoadAll();

        if (inventory != null)
        {
            inventory.OnItemsUpdated   += UpdateItemsText;
            inventory.OnWeaponsUpdated += UpdateWeaponsText;
            inventory.OnSkillsUpdated  += UpdateSkillsText;
            inventory.OnAllUpdated     += UpdateAllTexts;
        }

        UpdateAllTexts();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnItemsUpdated   -= UpdateItemsText;
            inventory.OnWeaponsUpdated -= UpdateWeaponsText;
            inventory.OnSkillsUpdated  -= UpdateSkillsText;
            inventory.OnAllUpdated     -= UpdateAllTexts;
        }
    }

    // -------- 갱신 엔트리 포인트 --------
    public void UpdateAllTexts()
    {
        UpdateItemsText();
        UpdateWeaponsText();
        UpdateSkillsText();
    }

    public void UpdateItemsText()
    {
        if (itemsText == null || inventory == null) return;

        var lines = inventory.itemList
            .Where(t => !skipZeroAmount || t.amount > 0)
            .Select(t => $"{GetItemName(t.itemCode)}, {t.amount}")
            .ToArray();

        itemsText.text = lines.Length > 0 ? string.Join("\n", lines) : emptyPlaceholder;
    }

    public void UpdateWeaponsText()
    {
        if (weaponsText == null || inventory == null) return;

        var lines = inventory.weaponList
            .Where(t => !skipZeroAmount || t.amount > 0)
            .Select(t => $"{GetWeaponName(t.weaponCode)}, {t.amount}")
            .ToArray();

        weaponsText.text = lines.Length > 0 ? string.Join("\n", lines) : emptyPlaceholder;
    }

    public void UpdateSkillsText()
    {
        if (skillsText == null || inventory == null) return;

        var lines = inventory.skillList
            .Where(t => !skipZeroAmount || t.skillLevel > 0)
            .Select(t => $"{GetSkillName(t.skillCode)}, {t.skillLevel}")
            .ToArray();

        skillsText.text = lines.Length > 0 ? string.Join("\n", lines) : emptyPlaceholder;
    }

    // ---- 이름 해석(로컬 카탈로그 사용) ----
    private string GetItemName(string code)   => catalog != null ? catalog.GetItemName(code)   : code;
    private string GetWeaponName(string code) => catalog != null ? catalog.GetWeaponName(code) : code;
    private string GetSkillName(string code)  => catalog != null ? catalog.GetSkillName(code)  : code;
}