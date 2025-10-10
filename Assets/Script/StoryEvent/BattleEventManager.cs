using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

// ------------------------------------------------------
// 런타임 슬롯(이번 전투에서만 HP 추적). 스탯은 enemy json 기준으로 사용.
// ------------------------------------------------------
public class EnemySlot
{
    public EnemyDataNode enemyData; //적 데이터, 공격력 듯 필요한 고정 스탯을 불러옴
    public int hp; //이번 전투 hp
    public int instanceId = 0; //적이 여러명일 시 구분을 위해 사용하는 고유 번호 (ex : 들개1 들개2 ... )

    /// <summary>
    /// 전투 진입 시 hp를 json 기준값으로 설정하는 메소드
    /// </summary>
    /// <param name="data">적 데이터</param>
    public EnemySlot(EnemyDataNode data)
    {
        enemyData = data;
        hp = data.hp;
    }

    //적의 생존 여부, 변수 사용시 hp를 사용하여 자동 계산
    public bool IsDead => hp <= 0;
    
    //고유 번호를 포함한 이름
    public string InstanceName => instanceId == 0 ? enemyData.name : $"{enemyData.name}{instanceId}";
}

// ------------------------------------------------------
// 플레이어 스탯 (api 연동 전)
// ------------------------------------------------------
[System.Serializable]
public class PlayerStats
{
    public int hp = 100; //체력 (100)
    public int atk = 10; //공격력 (10)
    public int spd = 10; //민첩 (10)
    public int hitRate = 70; //공격 적중 확률 (70%)
    public int evasionRate = 12; //회피 확률 (12%)
    public int CounterRate = 3; //치명타 확률 (3%, 반격 시 +5 추가 데미지)
}

public class BattleEventManager : MonoBehaviour
{
    private UserDataManager userDataManager;
    private SectionEventManager sectionEventManager;
    private EventDisplayManager eventDisplayManager;
    private DataService dataService;
    public Dictionary<string, EnemyDataNode> enemyData = new Dictionary<string, EnemyDataNode>();

    public PlayerStats player;
    private string battleImage = ""; //전투 삽화

    private string nextOnWin; //승리 후 이동할 노드 키
    private string nextOnLose; //패배 후 이동할 노드 키

    private List<object> turnOrder; //전투 턴 순서를 표기한 리스트 (플레이어= null, 적 = EnemySlot)
    private List<string> enemySet = new List<string>();
    private int turnIndex; //전투 턴 인덱스
    private bool battleEnded = false; //배틀 종료 플래그
    private Coroutine battleRoutine; //전투 루프 코루틴
    private const string UNARMED_CODE = "__UNARMED__"; //'맨손' 메뉴 출력을 위한 특수 코드

    private void Awake()
    {
        //참조 캐싱
        userDataManager = GetComponent<UserDataManager>();
        sectionEventManager = GetComponent<SectionEventManager>();
        eventDisplayManager = GetComponent<EventDisplayManager>();
        dataService = GetComponent<DataService>();
    }

    // ------------- 외부에서 시작하는 진입점 -------------
    /// <summary>
    /// 전투 진입 메소드, 해당 메소드를 통해 전투 시작
    /// currentEnemyList: null = 플레이어 턴, EnemyDataNode = 적 턴
    /// </summary>
    /// <param name="node">출력할 전투 노드</param>
    public void EnterBattleTurn(BattleNode node)
    {
        List<string> battleOrder = node.battleOrder;
        nextOnWin = node.battleWin;
        nextOnLose = node.battleLose;

        //기존 루틴 정리
        if (battleRoutine != null)
        {
            StopCoroutine(battleRoutine);
            battleRoutine = null;
        }

        turnOrder = BuildCurrentEnemyList(battleOrder); //적 리스트를 받아 슬롯으로 인스턴스화

        if (turnOrder == null || turnOrder.Count == 0)
        { //배틀 턴이 비었다면 그대로 종료
            Debug.LogWarning($"[{GetType().Name}] 빈 턴 순서입니다.");
            OnBattleFinished(false);
            return;
        }
        else if (turnOrder.Count(item => item == null) != 1)
        {
            Debug.LogError($"[{GetType().Name}] 잘못된 battleOrder : {string.Join("→", battleOrder)}");
            OnBattleFinished(false);
            return;
        }

        Debug.Log($"배틀 실행 : {string.Join("→", battleOrder)}");
        eventDisplayManager.DisplayBattleIntro( //전투 인트로 출력
            node.battleIntro,
            battleImage,
            onBattleStart: () => { battleRoutine = StartCoroutine(BattleLoop()); } //인트로 출력이 끝난다면 메인 전투 루프 실행
        );
    }

    /// <summary>
    /// 적 리스트를 받아 인스턴스(슬롯)로 바꾸는 메소드
    /// 전투 삽화 설정을 함께 진행
    /// </summary>
    /// <param name="battleOrder">전투 순서가 담긴 리스트</param>
    /// <returns>인스턴스화 된 적 리스트</returns>
    private List<object> BuildCurrentEnemyList(List<string> battleOrder)
    {
        Dictionary<string, int> spawnCount = new Dictionary<string, int>(); //같은 종류별로 등장 횟수를 추적
        bool battleImageChange = true;
        var currentEnemyList = new List<object>(battleOrder.Count);
        foreach (var code in battleOrder)
        {
            if (code == "player") //플레이어 턴
            {
                currentEnemyList.Add(null);
            }
            else //적 턴
            {
                var enemyCode = code; //적 코드 저장

                //적 인스턴스 생성, EnemyDataNode에서 스탯을 읽고 hp만 런타임 관리
                var tpl = dataService.Enemy.GetEnemyByCode(enemyCode);
                if(tpl == null)
                {
                    Debug.LogError($"[{GetType().Name}] 적 데이터를 찾을 수 없습니다: {enemyCode}");
                    continue;
                }
                if (battleImageChange)
                {
                    battleImage = tpl.image;
                    battleImageChange = false;
                }

                if (!spawnCount.ContainsKey(enemyCode)) //등장 횟수 세기
                {
                    spawnCount[enemyCode] = 0;
                    enemySet.Add(enemyCode); //첫 등장한 적은 enemySet에 코드 저장
                }
                    

                spawnCount[enemyCode]++;

                int count = 0;
                foreach (var element in battleOrder) //전체에서 같은 적이 몇마리 있는지 확인
                {
                    if (element == enemyCode)
                        count++;
                }
                //같은 적이 여러 마리면 1부터 번호 부여, 1마리만 있으면 0
                int number = (count == 1) ? 0 : spawnCount[enemyCode];

                var slot = new EnemySlot(tpl); //적 슬롯 생성
                slot.instanceId = number; //고유 번호 추가
                currentEnemyList.Add(slot);
            }
        }
        return currentEnemyList;
    }

    /// <summary>
    /// 메인 전투 루프를 실행하는 메소드
    /// </summary>
    private IEnumerator BattleLoop()
    {
        turnIndex = 0;
        battleEnded = false;

        var menuList = new List<WeaponDataNode>{ //표시 메뉴 리스트
            new WeaponDataNode { code = UNARMED_CODE} //맨손 삽입
        };

        //-------전투 진행에 필요한 정보를 불러와 캐시로 저장-------

        //보유중인 무기
        List<WeaponData> ownedWeapons = null;
        userDataManager.WeaponCheck(onResult: list => //보유중인 무기 리스트 불러오기
        {
            ownedWeapons = list;
        });

        if (ownedWeapons != null) //보유중인 무기가 있다면 정보 불러오기
        {
            var weaponCache = dataService.Weapon.GetWeaponsByCodes( //중복 제거
                ownedWeapons.Select(w => w.weaponCode).ToList()
            );

            foreach (var ow in ownedWeapons) //무기 정보 삽입
            {
                if (weaponCache.TryGetValue(ow.weaponCode, out var w))
                    menuList.Add(w);
            }
        }

        //적 전투 스크립트
        Dictionary<string, EnemyScriptNode> enemyScriptDict = new Dictionary<string, EnemyScriptNode>();
        foreach (string enemyCode in enemySet)
        {
            var enemyScript = dataService.EnemyScript.GetEnemyScriptByCode(enemyCode);
            if (enemyScript != null)
                enemyScriptDict[enemyCode] = enemyScript;
            else
                Debug.LogError($"[{GetType().Name}] 적 스크립트를 찾을 수 없습니다. : {enemyCode}");
        }

        //-----------------------------------------------------

        while (!battleEnded)
        {
            //텍스트 비우기
            eventDisplayManager.dialogueText.text = "";

            // 전투 종료 체크
            if (AllEnemiesDead()) { OnBattleFinished(true); yield break; } //모든 적 사망 (승리)
            if (player.hp <= 0) { OnBattleFinished(false); yield break; } //플레이어 사망 (패배)

            var slot = turnOrder[turnIndex]; //현재 전투 대상 확인
            //--------- 플레이어 턴 ---------
            if (slot == null)
            {
                //살아있는 적 목록 생성
                var aliveList = turnOrder.OfType<EnemySlot>().Where(e => !e.IsDead).ToList();
                if (aliveList.Count == 0) //살아있는 적이 없으면 즉시 종료
                {
                    OnBattleFinished(true);
                    yield break;
                }

                EnemySlot target = null;

                //공격할 적 선택 메뉴 출력
                yield return eventDisplayManager.DisplaySelectMenu(
                    aliveList,
                    e => e.InstanceName,
                    chosen => target = chosen
                );

                WeaponDataNode setWeapon = null;

                //무기 선택 메뉴 출력
                yield return eventDisplayManager.DisplaySelectMenu(
                    menuList,
                    e => e.code == UNARMED_CODE ? "맨손" : e.name, //맨손과 구분하여 출력
                    chosen => setWeapon = chosen
                );

                int damage = ComputePlayerDamage(target, setWeapon); //플레이어 데미지 계산
                target.hp = Mathf.Max(0, target.hp - damage); //적 체력 수정
                Debug.Log($"[{GetType().Name}] 유저 -> 적:{target.InstanceName} | 데미지:{damage} (HP {target.hp}/{target.enemyData.hp})");

                //eventDisplayManager.StartTyping();

                if (AllEnemiesDead())
                { //모든 적이 죽었을 경우 전투 종료(승리)
                    OnBattleFinished(true);
                    battleEnded = true;
                }
            }
            //--------- 적 턴 ---------
            else
            {
                var enemy = (EnemySlot)slot; //slot을 enemy로 전환

                if (!enemy.IsDead) //적이 죽었는지 확인, 죽은 적 슬롯은 스킵
                {
                    int damage = ComputeEnemyDamage(enemy); //적 데미지 계산
                    player.hp = Mathf.Max(0, player.hp - damage); //플레이어 체력 수정
                    Debug.Log($"[{GetType().Name}] 적:{enemy.InstanceName} -> 유저 | 데미지:{damage} (HP {player.hp}/{player.hp})");

                    if (player.hp <= 0) //유저의 체력이 0이 되었을 경우 전투 종료 (패배)
                    {
                        OnBattleFinished(false);
                        yield break;
                    }
                }
            }

            turnIndex = (turnIndex + 1) % turnOrder.Count; //인덱스 증가

            yield return new WaitForSeconds(0.15f); //연출 딜레이
        }
    }

    //------------- 유틸/계산 -------------
    /// <summary>
    /// 적이 전멸했는지 확인하는 메소드
    /// </summary>
    /// <returns>하나라도 살아 있으면 false, 아니라면 true</returns>
    private bool AllEnemiesDead()
    {
        for (int i = 0; i < turnOrder.Count; i++)
        {
            var slot = turnOrder[i];
            if (slot is EnemySlot e && !e.IsDead) return false;
        }
        return true;
    }

    /// <summary>
    /// 플레이어 데미지 계산 메소드
    /// </summary>
    /// <param name="target">타겟 슬롯</param>
    /// <returns>데미지 값</returns>
    private int ComputePlayerDamage(EnemySlot target, WeaponDataNode weapon)
    {
        int damage = player.atk;
        if (weapon.code != UNARMED_CODE && weapon.code != null)
            damage =+ weapon.damage;
        
        return damage;
    }

    /// <summary>
    /// 적 데미지 계산 메소드
    /// </summary>
    /// <param name="enemy">적 슬롯</param>
    /// <returns>데미지 값</returns>
    private int ComputeEnemyDamage(EnemySlot enemy)
    {
        return enemy.enemyData.atk;
    }

    /// <summary>
    /// 배틀 종료 후, 다음 노드로 이동을 수행하는 메소드
    /// </summary>
    /// <param name="win"></param>
    private void OnBattleFinished(bool win)
    {
        Debug.Log(win ? "전투 승리" : "전투 패배");

        // 기존 루틴 정리
        if (battleRoutine != null)
        {
            StopCoroutine(battleRoutine);
            battleRoutine = null;
        }

        // 여기서 당신의 노드 시스템과 연결하세요:
        sectionEventManager.StartDialogue(win ? nextOnWin : nextOnLose);
    }
}