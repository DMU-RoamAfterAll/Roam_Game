using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine.UI;

// --------------------------------------------------------------------
// 적 런타임 슬롯(이번 전투에서만 HP 추적). 스탯은 enemy json 기준으로 사용.
// --------------------------------------------------------------------
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

// --------------------------------------------------------------------
// 플레이어 런타임 슬롯(이번 전투에서만 HP 추적). 스탯은 player api 기준으로 사용.
// --------------------------------------------------------------------
public class PlayerSlot
{
    public PlayerDataNode playerData; //적 데이터, 공격력 듯 필요한 고정 스탯을 불러옴
    public int hp; //이번 전투 hp

    /// <summary>
    /// 전투 진입 시 hp를 api 기준값으로 설정하는 메소드
    /// </summary>
    /// <param name="data">플레이어 데이터</param>
    public PlayerSlot(PlayerDataNode data)
    {
        playerData = data;
        hp = data.hp;
    }

    //플레이어의 생존 여부, 변수 사용시 hp를 사용하여 자동 계산
    public bool IsDead => hp <= 0;
}
// --------------------------------------------------------------------

public class BattleEventManager : MonoBehaviour
{
    private UserDataManager userDataManager;
    private SectionEventManager sectionEventManager;
    private EventDisplayManager eventDisplayManager;
    private DataService dataService;

    public PlayerDataNode player;
    public PlayerSlot playerSlot;
    private string battleImage = string.Empty; //전투 삽화
    private string nextOnWin; //승리 후 이동할 노드 키
    private string nextOnLose; //패배 후 이동할 노드 키

    private List<object> turnOrder; //전투 턴 순서를 표기한 리스트 (플레이어= null, 적 = EnemySlot)
    private List<string> enemySet = new List<string>();
    private int turnIndex; //전투 턴 인덱스
    enum BattleOutcome { None, Victory, Defeat } //전투 결과 집합
    private BattleOutcome outcome = BattleOutcome.None; //전투 결과
    private Coroutine battleRoutine; //전투 루프 코루틴
    List<WeaponData> ownedWeapons = new List<WeaponData>(); //유저가 보유중인 무기 리스트
    private const string UNARMED_CODE = "__UNARMED__"; //'맨손' 메뉴 출력을 위한 특수 코드
    List<WeaponDataNode> menuList = new List<WeaponDataNode>(); //메뉴 표시 리스트
    private Dictionary<string, EnemyScriptNode> enemyScriptDict =
        new Dictionary<string, EnemyScriptNode>(); //적 전투 스크립트
    
    public TextMeshProUGUI hptext;

    private void Awake()
    {
        //참조 캐싱
        userDataManager = GetComponent<UserDataManager>();
        sectionEventManager = GetComponent<SectionEventManager>();
        eventDisplayManager = GetComponent<EventDisplayManager>();
        dataService = GetComponent<DataService>();

        RefreshHPText();
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

        playerSlot = new PlayerSlot(dataService.player.GetPlayerData()); //플레이어 슬롯 생성
        turnOrder = BuildCurrentEnemyList(battleOrder); //적 리스트를 받아 슬롯 생성
        RefreshHPText();

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
                if (tpl == null)
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
        List<string> enemyAtkScriptsList = new List<string>();

        //전투 진행에 필요한 정보를 불러와 캐시로 저장
        OwnedWeaponCacheLoad();
        EnemyScriptCacheLoad();

        //-----------------------------------------------------

        while (outcome == BattleOutcome.None)
        {
            var slot = turnOrder[turnIndex]; //현재 전투 대상 확인
            //--------- 플레이어 턴 ---------
            if (slot == null)
            {
                //이전까지 진행되었던 적 전투 턴 스크립트 출력
                if (enemyAtkScriptsList != null && enemyAtkScriptsList.Count() > 0)
                {
                    yield return StartCoroutine(
                        eventDisplayManager.DisplayScript(
                            enemyAtkScriptsList,
                            eventDisplayManager.nextText,
                            null)
                    );
                    enemyAtkScriptsList.Clear(); //출력 후 스크립트 비우기
                }

                //살아있는 적 목록 생성
                var aliveList = turnOrder.OfType<EnemySlot>().Where(e => !e.IsDead).ToList();
                if (aliveList.Count == 0) //살아있는 적이 없으면 즉시 종료
                {
                    outcome = BattleOutcome.Victory;
                    break;
                }

                EnemySlot target = null;
                //공격할 적 선택 메뉴 출력
                yield return eventDisplayManager.DisplaySelectMenu(
                    aliveList,
                    e => e.InstanceName,
                    chosen => target = chosen
                );

                WeaponDataNode setWeapon = null;

                Debug.Log(menuList);

                //무기 선택 메뉴 출력
                yield return eventDisplayManager.DisplaySelectMenu(
                    menuList,
                    e => e.code == UNARMED_CODE ? "맨손" : e.name, //맨손과 구분하여 출력
                    chosen => setWeapon = chosen
                );

                //공격 처리 스크립트 생성
                EnemyScriptNode enemyScriptNode = enemyScriptDict[target.enemyData.code]; //대상 적 스크립트 노드
                List<string> atkScriptList = PlayerBattleResultCreate(target, enemyScriptNode, setWeapon); //플레이어 전투 결과 스크립트 제작
                List<string> conditionScriptList = BattleConditionCreate(target, enemyScriptNode, setWeapon); //공격 이후 현재 상태를 나타내는 스크립트 제작


                //전투 결과 스크립트 출력
                yield return StartCoroutine(
                    eventDisplayManager.DisplayScript(
                        atkScriptList.Concat(conditionScriptList).ToList(),
                        eventDisplayManager.nextText,
                        null)
                );

                if (ResolveOutcome()) break;
            }
            //--------- 적 턴 ---------
            else
            {
                var enemy = (EnemySlot)slot; //slot을 enemy로 전환

                if (!enemy.IsDead) //적이 죽었는지 확인, 죽은 적 슬롯은 스킵
                {
                    EnemyScriptNode enemyScriptNode = enemyScriptDict[enemy.enemyData.code]; //대상 적 스크립트 노드

                    //적 전투 결과 스크립트 제작
                    List<string> atkScriptList = EnemyBattleResultCreate(enemy, enemyScriptNode);
                    //공격 이후 현재 상태를 나타내는 스크립트 제작
                    List<string> conditionScriptList = BattleConditionCreate(enemy, enemyScriptNode, null);
                    //전투 결과 스크립트 저장 (이후 플레이어의 턴에서 출력)
                    enemyAtkScriptsList = enemyAtkScriptsList.Concat(atkScriptList).Concat(conditionScriptList).ToList();

                    if (ResolveOutcome()) break;
                }
            }
            if (!ResolveOutcome()) {
                turnIndex = (turnIndex + 1) % turnOrder.Count; //게임 종료가 아니라면 인덱스 증가
                yield return new WaitForSeconds(0.15f); //연출 딜레이
            }
        }
        switch (outcome)
        {
            case BattleOutcome.Defeat: OnBattleFinished(false); yield break;
            case BattleOutcome.Victory: OnBattleFinished(true); yield break;
            default:
                Debug.LogWarning($"[{GetType().Name}] 잘못된 전투 종료 상태");
                yield break;
        }
    }

    /// <summary>
    /// 플레이어 데미지 계산 및 배틀 결과 스크립트 제작 메소드
    /// </summary>
    /// <param name="target">타겟 적 슬롯</param>
    /// <param name="setWeapon">장착 무기</param>
    /// <returns></returns>
    private List<string> PlayerBattleResultCreate(
        EnemySlot target,
        EnemyScriptNode enemyScriptNode,
        WeaponDataNode setWeapon)
    {
        //-----------플레이어의 공격 결과를 나타내는 스크립트 제작-----------
        var attackResult = SecureRng.Weighted(new[] { //플레이어의 공격 결과 상태
            ("atkHit", playerSlot.playerData.hitRate),
            ("atkMiss", (float)100 - playerSlot.playerData.hitRate - target.enemyData.CounterRate),
            ("ctrEHit", target.enemyData.CounterRate)
        });
        List<string> playerAttackResult = new List<string>(); //플레이어의 공격 결과 스크립트
        List<string> weaponAtkScript; //무기에 따른 공격 성공 및 실패 스크립트 리스트

        if (attackResult == "atkHit")
        {
            //플레이어의 공격이 성공 -> 적 회피 실패
            if (!SecureRng.Chance(target.enemyData.evasionRate / 100))//적 회피 판정
            {
                //플레이어 데미지 계산
                int damage = ComputePlayerDamage(target, setWeapon); //데미지 계산
                target.hp = Mathf.Max(0, target.hp - damage); //적 체력 수정
                Debug.Log($"[{GetType().Name}] 유저 -> 적:{target.InstanceName} | 데미지:{damage} (HP {target.hp}/{target.enemyData.hp})");

                //해당 공격 수준에 맞는 스크립트를 저장
                string weaponName = "맨 손";
                weaponAtkScript = enemyScriptNode.atkHit; //맨손 및 기타

                if (setWeapon.code == "wp_2001") //삽
                {
                    weaponAtkScript = enemyScriptNode.atkHit2001;
                    weaponName = setWeapon.name;
                }
                else if (setWeapon.code == "wp_2002") //식칼
                {
                    weaponAtkScript = enemyScriptNode.atkHit2002;
                    weaponName = setWeapon.name;
                }
                else if (setWeapon.code == "wp_2003") //녹슨 파이프
                {
                    weaponAtkScript = enemyScriptNode.atkHit2003;
                    weaponName = setWeapon.name;
                }

                int idx = SecureRng.Range(0, weaponAtkScript.Count()); //문장 랜덤 선택
                playerAttackResult.Add($"당신은 {weaponName}을 사용하여 {target.InstanceName}을(를) 공격했다.");
                playerAttackResult.Add(weaponAtkScript[idx].Replace(target.enemyData.name, target.InstanceName));
                playerAttackResult.Add($"{target.InstanceName}에게 {damage}의 피해를 입혔다!");

                if (target.hp <= 0)
                {
                    // 적 사망 & 생존
                    bool othersAlive = turnOrder
                        .OfType<EnemySlot>()
                        .Any(e => e != target && e != null && !e.IsDead);
                    if (othersAlive)
                    {
                        playerAttackResult.Add($"{target.InstanceName}이(가) 쓰러졌다.");
                    }
                }
                else
                {
                    playerAttackResult.Add($"{target.InstanceName}은(는) 아직 쓰러지지 않았다.");
                }
            }
            else //플레이어의 공격이 성공 -> 적 회피 성공
            {
                attackResult = "atkMiss"; //플레이어 공격 실패 판정
            }
        }
        //적의 반격이 성공일 시
        else if (attackResult == "ctrEHit")
        {
            //적 데미지 계산
            int damage = ComputeEnemyDamage(target); //데미지 계산
            playerSlot.hp = Mathf.Max(0, playerSlot.hp - damage); //적 체력 수정
            RefreshHPText();
            Debug.Log($"[{GetType().Name}] 적:{target.InstanceName} -> 유저 | 데미지:{damage} (HP {playerSlot.hp}/{playerSlot.playerData.hp})");

            int idx = SecureRng.Range(0, enemyScriptNode.atkMiss.Count()); //문장 랜덤 선택
            playerAttackResult.Add(enemyScriptNode.ctrEHit[idx].Replace(target.enemyData.name, target.InstanceName));
            playerAttackResult.Add($"{target.InstanceName}에게 {damage}의 피해를 입었다!");
        }
        //플레이어의 공격이 실패일 시
        if (attackResult == "atkMiss")
        {
            int idx = SecureRng.Range(0, enemyScriptNode.atkMiss.Count()); //문장 랜덤 선택
            playerAttackResult.Add(enemyScriptNode.atkMiss[idx].Replace(target.enemyData.name, target.InstanceName));
            playerAttackResult.Add($"{target.InstanceName}이 당신의 공격을 피했다.");
        }

        return playerAttackResult;
    }

    private List<string> EnemyBattleResultCreate(EnemySlot enemy, EnemyScriptNode enemyScriptNode)
    {
        //-----------적의 공격 결과를 나타내는 스크립트 제작-----------
        var attackResult = SecureRng.Weighted(new[] { //적의 공격 결과 상태
            ("atkHit", enemy.enemyData.hitRate),
            ("atkMiss", (float)100 - enemy.enemyData.hitRate - playerSlot.playerData.CounterRate),
            ("ctrPHit", playerSlot.playerData.CounterRate)
        });
        List<string> enemyAttackResult = new List<string>(); //적의 공격 결과 스크립트

        if (attackResult == "atkHit")
        {
            //적의 공격이 성공 -> 플레이어 회피 실패
            if (!SecureRng.Chance(player.evasionRate / 100))//적 회피 판정
            {
                //적 데미지 계산
                int damage = ComputeEnemyDamage(enemy); //데미지 계산
                playerSlot.hp = Mathf.Max(0, playerSlot.hp - damage); //플레이어 체력 수정
                RefreshHPText();
                Debug.Log($"[{GetType().Name}] 적:{enemy.InstanceName} -> 유저 | 데미지:{damage} (HP {playerSlot.hp}/{playerSlot.playerData.hp})");

                int idx = SecureRng.Range(0, enemyScriptNode.evdMiss.Count()); //문장 랜덤 선택
                enemyAttackResult.Add(enemyScriptNode.evdMiss[idx].Replace(enemy.enemyData.name, enemy.InstanceName));
                enemyAttackResult.Add($"{enemy.InstanceName}에게 {damage}의 피해를 입었다!");
            }
            else //적의 공격이 성공 -> 플레이어 회피 성공
            {
                attackResult = "atkMiss"; //적 공격 실패 판정
            }
        }
        //플레이어의 반격이 성공일 시
        else if (attackResult == "ctrEHit")
        {
            //플레이어 데미지 계산
            int damage = ComputePlayerDamage(enemy, null) + 3; //데미지 계산 (플레이어는 반격 시 추가 데미지)
            enemy.hp = Mathf.Max(0, enemy.hp - damage); //적 체력 수정
            Debug.Log($"[{GetType().Name}] 유저 -> 적:{enemy.InstanceName} | 데미지:{damage} (HP {enemy.hp}/{enemy.enemyData.hp})");

            int idx = SecureRng.Range(0, enemyScriptNode.ctrPHit.Count()); //문장 랜덤 선택
            enemyAttackResult.Add(enemyScriptNode.ctrPHit[idx].Replace(enemy.enemyData.name, enemy.InstanceName));
            enemyAttackResult.Add($"{enemy.InstanceName}에게 {damage}의 피해를 입혔다!");

            //적 사망 & 생존
            if (enemy.hp <= 0)
            {
                bool othersAlive = turnOrder
                    .OfType<EnemySlot>()
                    .Any(e => e != enemy && e != null && !e.IsDead);
                if (othersAlive)
                    enemyAttackResult.Add($"{enemy.InstanceName}이(가) 쓰러졌다.");
            }
            else
            {
                enemyAttackResult.Add($"{enemy.InstanceName}은(는) 아직 쓰러지지 않았다.");
            }
        }
        //적의 공격이 실패일 시
        if (attackResult == "atkMiss")
        {
            int idx = SecureRng.Range(0, enemyScriptNode.evdSuccess.Count()); //문장 랜덤 선택
            enemyAttackResult.Add(enemyScriptNode.evdSuccess[idx].Replace(enemy.enemyData.name, enemy.InstanceName));
            enemyAttackResult.Add($"당신은 {enemy.InstanceName}의 공격을 피했다.");
        }

        return enemyAttackResult;
    }

    private List<string> BattleConditionCreate(
        EnemySlot enemy,
        EnemyScriptNode enemyScriptNode,
        WeaponDataNode setweapon)
    {
        List<string> conditionScriptList = new List<string>();

        //플레이어 전투 패배
        if (playerSlot.hp <= 0)
        {
            int idx = SecureRng.Range(0, enemyScriptNode.battleDefeat.Count()); //문장 랜덤 선택
            conditionScriptList.Add(enemyScriptNode.battleDefeat[idx]);
            return conditionScriptList;
        }
        else
        {
            //플레이어 전투 승리
            if (AllEnemiesDead())
            {
                int idx = SecureRng.Range(0, enemyScriptNode.battleEnd.Count()); //문장 랜덤 선택
                conditionScriptList.Add(enemyScriptNode.battleEnd[idx]);
                conditionScriptList.Add("당신은 전투에서 승리했다.");
            }

            //무기 상태 확인
            if (setweapon != null && setweapon.code != UNARMED_CODE)
            {
                if (setweapon.durability == 1) //내구도가 1일 시 내구도 경고
                {
                    int idx = SecureRng.Range(0, setweapon.breakWarningMessages.Count()); //문장 랜덤 선택
                    conditionScriptList.Add(setweapon.breakWarningMessages[idx]);
                }
                else if (setweapon.durability <= 0)
                {
                    int idx = SecureRng.Range(0, setweapon.breakMessages.Count()); //문장 랜덤 선택
                    conditionScriptList.Add(setweapon.breakMessages[idx]);
                }
            }
        }

        return conditionScriptList;
    }

    //------------- 유틸/계산 -------------

    /// <summary>
    /// 유저가 보유중인 무기를 로드해 캐시로 저장하는 메소드
    /// </summary>
    private void OwnedWeaponCacheLoad()
    {
        //캐시 로드 전, 기존 데이터 정리
        menuList.Clear();
        ownedWeapons.Clear();

        menuList.Add(new WeaponDataNode { code = UNARMED_CODE }); //메뉴에 맨손 선택지 추가

        StartCoroutine(userDataManager.WeaponCheck(onResult: list => //보유중인 무기 리스트 불러오기
        {
            Debug.Log("무기 리스트 로드 완료.");
            ownedWeapons = list;
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
        },
        onError: (code, msg) =>
        {
            Debug.LogError($"[OwnedWeaponCacheLoad] 무기 로드 실패({code}) : {msg}");
        }));
    }

    /// <summary>
    /// 적 전투 스크립트를 로드해 캐시로 저장하는 메소드
    /// </summary>
    private void EnemyScriptCacheLoad()
    {
        //캐시 로드 전, 기존 데이터 정리
        enemyScriptDict.Clear();

        foreach (string enemyCode in enemySet)
        {
            var enemyScript = dataService.EnemyScript.GetEnemyScriptByCode(enemyCode);
            if (enemyScript != null)
                enemyScriptDict[enemyCode] = enemyScript;
            else
                Debug.LogError($"[{GetType().Name}] 적 스크립트를 찾을 수 없습니다. : {enemyCode}");
        }
    }

    /// <summary>
    /// 전투 종료를 확인하는 메소드
    /// </summary>
    /// <returns>전투 종료 조건 충족 시 true 아닐 시 false</returns>
    private bool ResolveOutcome()
    {
        if (playerSlot.hp <= 0) { outcome = BattleOutcome.Defeat; return true; }
        if (AllEnemiesDead())   { outcome = BattleOutcome.Victory; return true; }
        return false;
    }

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
        int damage = playerSlot.playerData.atk;
        if (weapon != null && weapon.code != UNARMED_CODE && weapon.code != null)
            damage += weapon.damage;

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
        RefreshHPText();
        Debug.Log(win ? "전투 승리" : "전투 패배");

        // 기존 루틴 정리
        if (battleRoutine != null)
        {
            StopCoroutine(battleRoutine);
            battleRoutine = null;
        }

        //다음 노드로 이동
        StartCoroutine(sectionEventManager.StartDialogue(win ? nextOnWin : nextOnLose));
    }

    void RefreshHPText() {
        if (hptext == null || playerSlot == null || playerSlot.playerData == null) return;

        int cur = Mathf.Max(0, playerSlot.hp);
        int max = Mathf.Max(1, playerSlot.playerData.hp);
        hptext.text = $"{cur}/{max}";

        // 가독성용 색상(옵션): >50% 흰색, 20~50% 주황, ≤20% 빨강
        float r = (float)cur / max;
        if      (r > 0.5f) hptext.color = Color.black;
        else if (r > 0.2f) hptext.color = new Color(1f, 0.65f, 0f);
        else               hptext.color = Color.red;
    }
}