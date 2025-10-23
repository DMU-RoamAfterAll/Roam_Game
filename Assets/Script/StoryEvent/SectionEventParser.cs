using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class SectionEventParser : MonoBehaviour
{
    /// <summary>
    /// Action태그의 사용 편의를 위한 수동 parser 틀
    /// 아이템 처리와 flag처리를 간편하게 쓰기 위해 사용됨
    /// </summary>
    /// <param name="actionObj">처리가 필요한 action노드</param>
    /// <returns>parsing 완료된 action노드</returns>
    public ActionNode ParseActionNode(JObject actionObj)
    {
        ActionNode action = new ActionNode();

        //허용된 Action값 체크 변수
        var allowedKeys = new HashSet<string>(StringComparer.Ordinal) //대소문자 구별
        {
            "image",
            "checkI", "getI", "lostI",
            "checkW", "getW", "lostW",
            "checkS", "getS",
            "flagSet", "flagCheck",
            "prob"
        };

        //알 수 없는 Action값 제외
        foreach (var prop in actionObj.Properties())
        {
            if (!allowedKeys.Contains(prop.Name))
            {
                UnityEngine.Debug.LogWarning(
                    $"[ParseActionNode] 알 수 없는 action 키: '{prop.Name}'  value={prop.Value}");
            }
        }

        //올바른 Action 처리
        if (actionObj.TryGetValue("image", out var imgToken))
            action.image = imgToken.ToString();

        if (actionObj.TryGetValue("checkI", out var checkIToken))
            action.checkI = ParseItemData(checkIToken);

        if (actionObj.TryGetValue("getI", out var getIToken))
            action.getI = ParseItemData(getIToken);

        if (actionObj.TryGetValue("lostI", out var lostIToken))
            action.lostI = ParseItemData(lostIToken);

        if (actionObj.TryGetValue("checkW", out var checkWToken))
            action.checkW = ParseWeaponData(checkWToken);

        if (actionObj.TryGetValue("getW", out var getWToken))
            action.getW = ParseWeaponData(getWToken);

        if (actionObj.TryGetValue("lostW", out var lostWToken))
            action.lostW = ParseWeaponData(lostWToken);
        
        if (actionObj.TryGetValue("checkS", out var checkSToken))
            action.checkS = ParseSkillData(checkSToken);

        if (actionObj.TryGetValue("getS", out var getSToken))
            action.getS = ParseSkillData(getSToken);

        if (actionObj.TryGetValue("flagSet", out var flagSetToken))
            action.flagSet = ParseStoryFlag(flagSetToken);

        if (actionObj.TryGetValue("flagCheck", out var flagCheckToken))
            action.flagCheck = ParseStoryFlag(flagCheckToken);
        
        if (actionObj.TryGetValue("prob", out var probToken))
            action.prob = ParseProbOption(probToken);
            
        return action;
    }

    /// <summary>
    /// 단일/이중 배열 공통 파서
    /// </summary>
    /// <typeparam name="TOut">출력 타입(ItemData, WeaponData, FlagData)</typeparam>
    /// <typeparam name="TValue">값 타입(int, bool)</typeparam>
    /// <param name="token">파싱 대상 Json 토큰</param>
    /// <param name="factory">출력 인스턴스를 생성하는 함수</param>
    /// <param name="convert">값 요소를 변환하는 함수</param>
    /// <param name="defaultValue">값 요소가 없거나 변환 실패시 기본 값</param>
    /// <returns>TOut 리스트</returns>
    private static List<TOut> ParsePairs<TOut, TValue>
    (
        JToken token,
        Func<string, TValue, TOut> factory,
        Func<JToken, TValue> convert,
        TValue defaultValue = default
    )
    {
        var list = new List<TOut>();
        if (token == null)
            return list;

        if (token.Type != JTokenType.Array)
        {
            Debug.LogError($"[ParsePairs<{typeof(TOut).Name},{typeof(TValue).Name}>] 알 수 없는 토큰 타입: {token.Type}");
            return list;
        }

        var arr = (JArray)token;
        if (arr.Count == 0)
            return list;

        // 이중 배열: [ ["code", value], ... ]
        if (arr[0].Type == JTokenType.Array)
        {
            foreach (var inner in arr)
            {
                var innerArr = inner as JArray;
                if (innerArr == null || innerArr.Count == 0)
                {
                    Debug.LogWarning("[ParsePairs] 잘못된 내부 배열 (길이 0)");
                    continue;
                }

                string code = innerArr[0]?.ToString();
                TValue value;

                if (innerArr.Count > 1 && innerArr[1] != null && innerArr[1].Type != JTokenType.Null)
                {
                    try { value = convert(innerArr[1]); }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ParsePairs] 값 변환 실패 code='{code}', token='{innerArr[1]}': {e.Message} → default 사용");
                        value = defaultValue;
                    }
                }
                else
                {
                    value = defaultValue;
                }

                list.Add(factory(code, value));
            }
        }
        else
        {
            // 단일 배열: ["code", value]
            if (arr.Count == 0)
            {
                Debug.LogWarning("[ParsePairs] 잘못된 단일 배열 (길이 0)");
                return list;
            }

            string code = arr[0]?.ToString();
            TValue value;

            if (arr.Count > 1 && arr[1] != null && arr[1].Type != JTokenType.Null)
            {
                try { value = convert(arr[1]); }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ParsePairs] 값 변환 실패(단일) code='{code}', token='{arr[1]}': {e.Message} → default 사용");
                    value = defaultValue;
                }
            }
            else
            {
                value = defaultValue;
            }

            list.Add(factory(code, value));
        }

        return list;
    }

    /// <summary>
    /// JToken 타입을 JValue(int)타입으로 변환시키는 함수
    /// </summary>
    /// <param name="t">변환할 JToken값</param>
    /// <returns>변환된 JValue(int)값</returns>
    /// <exception cref="FormatException">변환 불가능한 string값 경고</exception>
    /// <exception cref="InvalidCastException">변환 불가능한 기타 타입 경고</exception>
    private static int ConvertToInt(JToken t)
    {
        switch (t.Type)
        {
            case JTokenType.Integer: return t.ToObject<int>();
            case JTokenType.Float: return Convert.ToInt32(t.ToObject<double>());
            case JTokenType.Boolean: return t.ToObject<bool>() ? 1 : 0;
            case JTokenType.String:
                {
                    var s = t.ToString().Trim();
                    if (int.TryParse(s, out var i)) return i;
                    if (double.TryParse(s, out var d)) return Convert.ToInt32(d);
                    if (bool.TryParse(s, out var b)) return b ? 1 : 0;
                    throw new FormatException($"[SectionEventParser] 정수로 변환 불가: '{s}'");
                }
            default:
                throw new InvalidCastException($"[SectionEventParser] 정수 변환 불가 타입: {t.Type}");
        }
    }

    /// <summary>
    /// JToken 타입을 JValue(bool)타입으로 변환시키는 함수
    /// </summary>
    /// <param name="t">변환할 JToken값</param>
    /// <returns>변환된 JValue(bool)값</returns>
    /// <exception cref="FormatException">변환 불가능한 string값 경고</exception>
    /// <exception cref="InvalidCastException">변환 불가능한 기타 타입 경고</exception>
    private static bool ConvertToBool(JToken t)
    {
        switch (t.Type)
        {
            case JTokenType.Boolean: return t.ToObject<bool>();
            case JTokenType.Integer: return t.ToObject<long>() != 0;
            case JTokenType.Float: return Math.Abs(t.ToObject<double>()) > double.Epsilon;
            case JTokenType.String:
                {
                    var s = t.ToString().Trim().ToLowerInvariant();
                    if (s == "true" || s == "t" || s == "yes" || s == "y") return true;
                    if (s == "false" || s == "f" || s == "no" || s == "n") return false;
                    if (s == "1") return true;
                    if (s == "0") return false;
                    if (bool.TryParse(s, out var b)) return b;
                    throw new FormatException($"[SectionEventParser] 불리언으로 변환 불가: '{s}'");
                }
            default:
                throw new InvalidCastException($"[SectionEventParser] 불리언 변환 불가 타입: {t.Type}");
        }
    }

    /// <summary>
    /// [string, int] 전용 parser
    /// </summary>
    /// <typeparam name="TOut">생성할 결과 타입(ItemData, WeaponData 등)</typeparam>
    /// <param name="token">파싱 대상 토큰</param>
    /// <param name="factory">파싱된 토큰을 인스턴스로 바꾸는 매퍼</param>
    /// <param name="defaultInt">파싱에 실패할 시, 기본으로 들어가는 int 값</param>
    /// <returns></returns>
    private static List<TOut> ParsePairsInt<TOut>(
        JToken token,
        Func<string, int, TOut> factory,
        int defaultInt = 1
    ) => ParsePairs(token, factory, ConvertToInt, defaultInt);

    /// <summary>
    /// [string, bool] 전용 parser
    /// </summary>
    /// <typeparam name="TOut">생성할 결과 타입(FlagData 등)</typeparam>
    /// <param name="token">파싱 대상 토큰</param>
    /// <param name="factory">파싱된 토큰을 인스턴스로 바꾸는 매퍼</param>
    /// <param name="defaultBool">파싱에 실패할 시, 기본으로 들어가는 bool 값</param>
    /// <returns></returns>
    private static List<TOut> ParsePairsBool<TOut>(
        JToken token,
        Func<string, bool, TOut> factory,
        bool defaultBool = false
    ) => ParsePairs(token, factory, ConvertToBool, defaultBool);

    /// <summary>
    /// 아이템 처리 부분의 parser (ParseActionNode함수에 사용)
    /// </summary>
    /// <param name="token">아이템 처리 action 정보</param>
    private List<ItemData> ParseItemData(JToken token)
    {
        return ParsePairsInt(token, (code, amount) => new ItemData
        {
            itemCode = code,
            amount = amount
        }, defaultInt: 1);
    }

    /// <summary>
    /// 무기 처리 부분의 parser (ParseActionNode함수에 사용)
    /// </summary>
    /// <param name="token">아이템 처리 action 정보</param>
    private List<WeaponData> ParseWeaponData(JToken token)
    {
        return ParsePairsInt(token, (code, amount) => new WeaponData
        {
            weaponCode = code,
            amount = amount
        }, defaultInt: 1);
    }

    /// <summary>
    /// 스킬 처리 부분의 parser (ParseActionNode함수에 사용)
    /// </summary>
    /// <param name="token">스킬 처리 action 정보</param>
    private List<SkillData> ParseSkillData(JToken token)
    {
        return ParsePairsInt(token, (code, amount) => new SkillData
        {
            skillCode = code,
            skillLevel = amount
        }, defaultInt: 1);
    }

    /// <summary>
    /// 플래그 처리 부분의 parser (ParseActionNode함수에 사용)
    /// </summary>
    /// <param name="token">플래그 처리 action 정보</param>
    private List<FlagData> ParseStoryFlag(JToken token)
    {
        return ParsePairsBool(token, (code, state) => new FlagData
        {
            flagCode = code,
            flagState = state
        }, defaultBool: false);
    }

    /// <summary>
    /// 확률 이동 처리 부분의 parser (ParseActionNode함수에 사용)
    /// </summary>
    /// <param name="token">확률 이동 처리 action 정보</param>
    private List<ProbData> ParseProbOption(JToken token)
    {
        return ParsePairsInt(token, (next, probability) => new ProbData
        {
            next = next,
            probability = probability
        }, defaultInt: 50);
    }
}
