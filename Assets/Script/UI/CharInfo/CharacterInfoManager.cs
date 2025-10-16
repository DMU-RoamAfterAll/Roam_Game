using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CharacterInfoManager : MonoBehaviour {
    // 인벤토리 요약을 보여줄 곳(선택)
    [SerializeField] private TextMeshProUGUI inventorySummary;

    // 최근 조회한 인벤토리 보관 (외부에서 참고 가능)
    private List<ItemData> _inventory = new List<ItemData>();

    // UserDataManager 참조
    [SerializeField] private UserDataManager userData;

    public TextMeshProUGUI playerName;
    public TextMeshProUGUI survivalDay;
    public TextMeshProUGUI stepCount;

    void OnEnable() {
        playerName.text = GameDataManager.Data.playerName;
        survivalDay.text = $" {TimeManager.Instance.DaysSinceLastSeen}일";
        stepCount.text = $" {StepManager.Instance.sessionSteps}보";

        StartCoroutine(RefreshInventoryUI());
    }

    /// <summary>
    /// 서버에서 인벤토리를 받아와 내부 보관 & UI 갱신
    /// </summary>
    public IEnumerator RefreshInventoryUI() {
        if (userData == null) yield break;

        yield return userData.ItemCheck(
            items => {
                _inventory = items ?? new List<ItemData>();

                if (inventorySummary != null) {
                    if (_inventory.Count == 0) {
                        inventorySummary.text = "인벤토리: 없음";
                    } else {
                        // 상위 몇 개만 요약 표시
                        var top = _inventory.Take(5)
                            .Select(i => $"{i.itemCode} x{i.amount}");
                        inventorySummary.text = "인벤토리: " + string.Join(", ", top);
                    }
                }
            },
            (code, err) => {
                if (inventorySummary != null)
                    inventorySummary.text = $"인벤토리 조회 실패 ({code})";
            }
        );
    }

    // --- 외부에서 참조하기 편한 헬퍼들 ---

    public bool HasItem(string itemCode) {
        return _inventory.Any(i => i.itemCode == itemCode);
    }

    public int GetItemAmount(string itemCode) {
        return _inventory.Where(i => i.itemCode == itemCode).Sum(i => i.amount);
    }

    public IReadOnlyList<ItemData> GetInventory() => _inventory;
}