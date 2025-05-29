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
public class RegisterRequest
{
    public string username;
    public string password;
    public string nickname;
    public DateTime birthDate;
    public string email;
}

[System.Serializable]
public class RegisterResponse
{
    public string accessToken;
    public string refreshToken;
}
#endregion

public class RegisterManager : MonoBehaviour
{
    private string baseUrl = "http://44.218.171.57:8080/api/users";

    [Header("Register")]
    public TMP_InputField idInputField;
    public TMP_InputField pwInputField;
    public TMP_InputField pwConfirmInputField;
    public TMP_InputField emailInputField;
    public TMP_InputField nicknameInputField;
    public TMP_InputField birthInputField;

    public void RegisterBtn() // 회원가입 버튼 onclick 함수
    {
        StartCoroutine(Register(idInputField.text, pwInputField.text, nicknameInputField.text, birthInputField.text, emailInputField.text));
    }

    public void GoLoginBtn() // 로그인 씬으로 되돌아가는 버튼
    {
        SceneManager.LoadScene("LoginScene");
    }
    public static bool IsValidEmail(string email) // 이메일 형식 검사 메소드
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

        return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
    }

    #region 로그인 코루틴 함수
    public IEnumerator Register(string username, string password, string nickname, string birth, string email)
    {
        if (string.IsNullOrEmpty(idInputField.text) || string.IsNullOrEmpty(pwInputField.text) || string.IsNullOrEmpty(emailInputField.text)
            || string.IsNullOrEmpty(nicknameInputField.text) || string.IsNullOrEmpty(birthInputField.text)) // 칸이 하나라도 비워져있으면
        {
            Debug.Log("빈 칸을 채워주세요.");
            yield break;
        }

        if (pwInputField.text != pwConfirmInputField.text)
        {
            Debug.Log("비밀번호와 비밀번호 확인란 입력값이 다릅니다.");
            yield break;
        }

        DateTime birthdate;
        if (!DateTime.TryParseExact(birth, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out birthdate))
        {
            Debug.Log("생년월일 형식이 잘못되었습니다. 8자리로 입력해주세요. 예: 19000101");
            yield break;
        }

        if (!IsValidEmail(emailInputField.text))
        {
            Debug.Log("이메일 형식이 올바르지 않습니다.");
            yield break;
        }

        var registerData = new RegisterRequest
        {
            username = username,
            password = password,
            nickname = nickname,
            birthDate = birthdate,
            email = email
        };
        // 날짜 포맷 적용
        var dateConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd" };
        // 직렬화 시 설정 적용
        string jsonData = JsonConvert.SerializeObject(registerData, new JsonSerializerSettings
        {
            Converters = { dateConverter }
        });
        
        UnityWebRequest request = new UnityWebRequest($"{baseUrl}/register", "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("회원가입 성공");
            SceneManager.LoadScene("LoginScene");
        }
        else // 회원가입 실패시 표시할 텍스트
        {
            Debug.Log("회원가입 실패: " + request.error);
        }
    }
    #endregion

}
