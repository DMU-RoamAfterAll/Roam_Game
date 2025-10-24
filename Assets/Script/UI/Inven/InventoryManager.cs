using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class InventoryManager : MonoBehaviour
{
    // 서버에서 받아 캐싱해 두는 리스트들
    public List<(string itemCode,   int amount)>      itemList   = new();
    public List<(string weaponCode, int amount)>      weaponList = new();
    public List<(string skillCode,  int skillLevel)>  skillList  = new();

    // 로드 완료 이벤트 (원하면 UI 갱신에 바인딩)
    public event Action OnItemsUpdated;
    public event Action OnWeaponsUpdated;
    public event Action OnSkillsUpdated;
    public event Action OnAllUpdated;

    [Header("Auto fetch on enable")]
    [SerializeField] private bool  autoFetchOnEnable   = true;
    [SerializeField] private float fetchDelayOnEnable  = 0f;

    private bool fetchingItems, fetchingWeapons, fetchingSkills;

    private void OnEnable()
    {
        if (!autoFetchOnEnable) return;
        if (fetchDelayOnEnable > 0f) StartCoroutine(FetchAllAfterDelay(fetchDelayOnEnable));
        else                         RefreshAll();
    }

    private IEnumerator FetchAllAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RefreshAll();
    }

    // ----------------- 공개 API -----------------

    /// <summary>세 가지(아이템/무기/스킬) 전부 새로고침(비동기 시작)</summary>
    public void RefreshAll(string username = null)
    {
        // 동시에 돌려도 되지만, 순차가 더 단순합니다.
        StartCoroutine(RefreshAllRoutine(username));
    }

    private IEnumerator RefreshAllRoutine(string username = null)
    {
        yield return RefreshItemsRoutine(username);
        yield return RefreshWeaponsRoutine(username);
        yield return RefreshSkillsRoutine(username);
        OnAllUpdated?.Invoke();
    }

    /// <summary>아이템만 새로고침(즉시 비동기 시작)</summary>
    public void RefreshItems(string username = null)
    {
        StartCoroutine(RefreshItemsRoutine(username));
    }

    /// <summary>무기만 새로고침(즉시 비동기 시작)</summary>
    public void RefreshWeapons(string username = null)
    {
        StartCoroutine(RefreshWeaponsRoutine(username));
    }

    /// <summary>스킬만 새로고침(즉시 비동기 시작)</summary>
    public void RefreshSkills(string username = null)
    {
        StartCoroutine(RefreshSkillsRoutine(username));
    }

    // ----------------- 코루틴 본체 -----------------

    public IEnumerator RefreshItemsRoutine(string username = null)
    {
        if (fetchingItems) yield break;
        fetchingItems = true;

        username = ResolveUsername(username);
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("[Inventory] username 비어있음 - Items load skip");
            fetchingItems = false; yield break;
        }

        string url = $"{BaseUrl}:8081/api/inventory/items?username={UnityWebRequest.EscapeURL(username)}";
        yield return GetArray(url, arr =>
        {
            itemList.Clear();
            foreach (var el in arr)
            {
                string code = el.Value<string>("itemCode");
                int amount  = el.Value<int?>("amount") ?? 0;
                if (!string.IsNullOrEmpty(code)) itemList.Add((code, amount));
            }
            DumpItems();
            OnItemsUpdated?.Invoke();
        });

        fetchingItems = false;
    }

    public IEnumerator RefreshWeaponsRoutine(string username = null)
    {
        if (fetchingWeapons) yield break;
        fetchingWeapons = true;

        username = ResolveUsername(username);
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("[Inventory] username 비어있음 - Weapons load skip");
            fetchingWeapons = false; yield break;
        }

        string url = $"{BaseUrl}:8081/api/inventory/weapons?username={UnityWebRequest.EscapeURL(username)}";
        yield return GetArray(url, arr =>
        {
            weaponList.Clear();
            foreach (var el in arr)
            {
                string code = el.Value<string>("weaponCode");
                int amount  = el.Value<int?>("amount") ?? 0;
                if (!string.IsNullOrEmpty(code)) weaponList.Add((code, amount));
            }
            DumpWeapons();
            OnWeaponsUpdated?.Invoke();
        });

        fetchingWeapons = false;
    }

    public IEnumerator RefreshSkillsRoutine(string username = null)
    {
        if (fetchingSkills) yield break;
        fetchingSkills = true;

        username = ResolveUsername(username);
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("[Inventory] username 비어있음 - Skills load skip");
            fetchingSkills = false; yield break;
        }

        string url = $"{BaseUrl}:8081/api/skills?username={UnityWebRequest.EscapeURL(username)}";
        yield return GetArray(url, arr =>
        {
            skillList.Clear();
            foreach (var el in arr)
            {
                string code  = el.Value<string>("skillCode");
                int level    = el.Value<int?>("skillLevel") ?? 0;
                if (!string.IsNullOrEmpty(code)) skillList.Add((code, level));
            }
            DumpSkills();
            OnSkillsUpdated?.Invoke();
        });

        fetchingSkills = false;
    }

    // ----------------- 공통 요청/헬퍼 -----------------

    private string BaseUrl => GameDataManager.Data.baseUrl?.TrimEnd('/');

    private IEnumerator GetArray(string url, Action<JArray> onOK)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Accept", "application/json");

            var token = AuthManager.Instance != null ? AuthManager.Instance.AccessToken : null;
            if (!string.IsNullOrEmpty(token))
                req.SetRequestHeader("Authorization", $"Bearer {token}");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success &&
                req.responseCode >= 200 && req.responseCode < 300)
            {
                try
                {
                    var arr = JArray.Parse(req.downloadHandler.text);
                    onOK?.Invoke(arr);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Inventory] JSON 파싱 실패: {e.Message}\n{req.downloadHandler.text}");
                }
            }
            else
            {
                Debug.LogWarning($"[Inventory] GET 실패 code={req.responseCode}, err={req.error}, url={url}");
            }
        }
    }

    private string ResolveUsername(string overrideName)
    {
        if (!string.IsNullOrEmpty(overrideName)) return overrideName;
        if (!string.IsNullOrEmpty(GameDataManager.Data?.playerName))
            return GameDataManager.Data.playerName;
        return AuthManager.Instance != null ? AuthManager.Instance.username : null;
    }

    // ---------- 조회 편의 ----------
    public int  GetItemAmount  (string itemCode)   => itemList.Find(t => t.itemCode   == itemCode).amount;
    public int  GetWeaponAmount(string weaponCode) => weaponList.Find(t => t.weaponCode== weaponCode).amount;
    public int  GetSkillLevel  (string skillCode)  => skillList.Find(t => t.skillCode  == skillCode ).skillLevel;

    public bool HasItem  (string itemCode,   int need) => GetItemAmount(itemCode)   >= need;
    public bool HasWeapon(string weaponCode, int need) => GetWeaponAmount(weaponCode) >= need;
    public bool HasSkill (string skillCode,  int minLv) => GetSkillLevel(skillCode)  >= minLv;

    // ---------- 디버그 출력 ----------
    private void DumpItems()
    {
        var sb = new StringBuilder("[Inventory] Items\n");
        foreach (var (c,a) in itemList) sb.AppendLine($" - {c} : {a}");
        Debug.Log(sb.ToString());
    }
    private void DumpWeapons()
    {
        var sb = new StringBuilder("[Inventory] Weapons\n");
        foreach (var (c,a) in weaponList) sb.AppendLine($" - {c} : {a}");
        Debug.Log(sb.ToString());
    }
    private void DumpSkills()
    {
        var sb = new StringBuilder("[Inventory] Skills\n");
        foreach (var (c,l) in skillList) sb.AppendLine($" - {c} : Lv.{l}");
        Debug.Log(sb.ToString());
    }
}