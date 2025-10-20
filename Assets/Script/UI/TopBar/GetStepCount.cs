using UnityEngine;
using TMPro;

public class GetStepCount : MonoBehaviour {
    public TextMeshProUGUI stepCountText;

    void Awake() {
        if (!stepCountText) stepCountText = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable() {
        var sm = StepManager.Instance;
        if (sm == null) return;

        sm.AvailableStepsChanged += OnStepsChanged;

        // 현재 값으로 즉시 1회 갱신
        OnStepsChanged(sm.availableSteps);
    }

    void OnDisable() {
        var sm = StepManager.Instance;
        if (sm != null) sm.AvailableStepsChanged -= OnStepsChanged;
    }

    private void OnStepsChanged(int available) {
        if (stepCountText) stepCountText.text = $"{available}";
    }
}