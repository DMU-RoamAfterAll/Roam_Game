using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField idInput;
    public TMP_InputField pwInput;

    private const string SAVE_ID_KEY = "SavedUserID";
    private const string SAVE_PW_KEY = "SavedUserPW";

    private void Start()
    {
        LoadLoginData();  // 실행 시 저장된 정보 불러오기
    }

    public void OnLoginButtonPressed()
    {
        // ✅ 토글 여부와 상관없이 무조건 저장
        PlayerPrefs.SetString(SAVE_ID_KEY, idInput.text);
        PlayerPrefs.SetString(SAVE_PW_KEY, pwInput.text);
        PlayerPrefs.Save();

        // 이후 로그인 API 요청 로직
        Debug.Log("로그인 버튼 클릭됨 (항상 저장)");
    }

    private void LoadLoginData()
    {
        idInput.text = PlayerPrefs.GetString(SAVE_ID_KEY, "");
        pwInput.text = PlayerPrefs.GetString(SAVE_PW_KEY, "");
    }
}