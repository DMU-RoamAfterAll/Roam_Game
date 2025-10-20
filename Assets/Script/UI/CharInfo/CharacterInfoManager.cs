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
    public TextMeshProUGUI stepCount; // ← 이제 "지금까지 쓴 걸음수" 표시에 사용

    void OnEnable() {
        playerName.text = GameDataManager.Data.playerName;
        survivalDay.text = $" {TimeManager.Instance.DaysSinceLastSeen}일";

        // Step UI 최초 갱신
        UpdateUsedStepsUI();

        // 실시간 갱신을 위해 구독
        if (StepManager.Instance != null)
            StepManager.Instance.AvailableStepsChanged += OnAvailableStepsChanged;

        StartCoroutine(RefreshInventoryUI());
    }

    void OnDisable() {
        if (StepManager.Instance != null)
            StepManager.Instance.AvailableStepsChanged -= OnAvailableStepsChanged;
    }

    private void OnAvailableStepsChanged(int _)
    {
        UpdateUsedStepsUI();
    }

    private void UpdateUsedStepsUI()
    {
        var sm = StepManager.Instance;
        if (sm == null) { stepCount.text = " - 보"; return; }

        // 지금까지 '사용한' 걸음수 = (오늘 벌어들인 걸음수) - (현재 남은 걸음수)
        int used = sm.GetTodayUsedSteps();
        stepCount.text = $" {used}보";
    }

    /// 버튼 등에서 호출: 모든 걸음수 기준을 지금으로 초기화
    public void OnClick_ResetAllSteps()
    {
        StepManager.Instance?.ResetAllSteps();
        UpdateUsedStepsUI();
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