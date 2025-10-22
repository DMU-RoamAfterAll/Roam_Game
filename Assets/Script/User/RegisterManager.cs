using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using Newtonsoft.Json.Converters;
using System.Text.RegularExpressions;


#region 데이터 클래스
[System.Serializable]
// 회원가입 요청에 사용되는 데이터 클래스
public class RegisterRequest 
{
    public string username;
    public string password;
    public string nickname;
    public DateTime birthDate;
    public string email;
}

[System.Serializable]
// 회원가입 응답 데이터 클래스 (JWT 토큰 수신용)
public class RegisterResponse 
{
    public string accessToken;
    public string refreshToken;
}
#endregion

public class RegisterManager : MonoBehaviour {
    private string baseUrl;

    // UI 입력 창 아웃렛 접속
    [Header("Register")] 
    public TMP_InputField idInputField;
    public TMP_InputField pwInputField;
    public TMP_InputField pwConfirmInputField;
    public TMP_InputField emailInputField;
    public TMP_InputField nicknameInputField;
    public TMP_InputField birthInputField;

    [Header("UI Object")]
    public GameObject registerUI;
    public GameObject loginUI;

    void Start() {
        baseUrl = $"{GameDataManager.Data.baseUrl}:8081/api/users";
    }

    // 회원가입 버튼 onclick 함수
    public void RegisterBtn() 
    {
        StartCoroutine(Register(
            idInputField.text,
            pwInputField.text, 
            nicknameInputField.text, 
            birthInputField.text, 
            emailInputField.text));
    }

    // 로그인 씬으로 되돌아가는 버튼
    public void GoLoginBtn() 
    {
        registerUI.SetActive(false);
        loginUI.SetActive(true);
    }

    // 이메일 형식 검사 메소드
    public static bool IsValidEmail(string email) 
    {
        // 빈칸이면 false 반환
        if (string.IsNullOrWhiteSpace(email)) 
            return false;

        // 이메일 표현 정규식
        string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

        // 입력된 문자열이 정규식에 맞는지 검사
        return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
    }

    #region 로그인 코루틴 함수
    public IEnumerator Register(string username, string password, string nickname, string birth, string email)
    {
        // 입력값이 하나라도 비워져있는지 확인
        if (string.IsNullOrEmpty(idInputField.text) ||
            string.IsNullOrEmpty(pwInputField.text) || 
            string.IsNullOrEmpty(emailInputField.text) || 
            string.IsNullOrEmpty(nicknameInputField.text) || 
            string.IsNullOrEmpty(birthInputField.text)) 
        {
            Debug.Log("빈 칸을 채워주세요.");
            yield break;
        }

        // 비밀번호 칸과 비밀번호 확인 입력값이 다른지 확인
        if (pwInputField.text != pwConfirmInputField.text) 
        {
            Debug.Log("비밀번호와 비밀번호 확인란 입력값이 다릅니다.");
            yield break;
        }

        DateTime birthdate;
        // 문자열을 Datetime 객체로 정해진 형식으로 변환해 birthdate로 반환
        if (!DateTime.TryParseExact(birth, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out birthdate))
        {
            Debug.Log("생년월일 형식이 잘못되었습니다. 8자리로 입력해주세요. 예: 19000101");
            yield break;
        }

        // 이메일 형식이 갖춰졌는지 확인
        if (!IsValidEmail(emailInputField.text))
        {
            Debug.Log("이메일 형식이 올바르지 않습니다.");
            yield break;
        }

        // 요청 데이터 생성
        var registerData = new RegisterRequest
        {
            username = username,
            password = password,
            nickname = nickname,
            birthDate = birthdate,
            email = email
        };

        // 날짜 포맷 서버에 맞게 적용 (yyyyMMdd -> yyyy-MM-dd)
        var dateConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd" };

        // RegisterRequest 객체를 JSON 문자열로 직렬화할 때 해당 날짜 포맷 사용
        string jsonData = JsonConvert.SerializeObject(registerData, new JsonSerializerSettings
        {
            Converters = { dateConverter }
        });
        
        // POST 요청 생성
        UnityWebRequest request = new UnityWebRequest($"{baseUrl}/register", "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // 요청 url 전송
        yield return request.SendWebRequest();

        // 요청 결과 확인
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("회원가입 성공");
            GameDataManager.Data.playerName = nickname;
            registerUI.SetActive(false);
            loginUI.SetActive(true);
        }
        else // 회원가입 실패시 표시할 텍스트
        {
            Debug.Log("회원가입 실패: " + request.error);
        }
    }
    #endregion

}
