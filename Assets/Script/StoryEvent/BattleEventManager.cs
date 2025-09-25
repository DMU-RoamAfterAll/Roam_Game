using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEditor;

// ------------------------------------------------------
// 런타임 슬롯(이번 전투에서만 HP 추적). 스탯은 enemy json 기준으로 사용.
// ------------------------------------------------------
class EnemySlot {
    public EnemyDataNode enemyData; //적 데이터, 공격력 듯 필요한 고정 스탯을 불러옴
    public int hp; //이번 전투 hp

    /// <summary>
    /// 전투 진입 시 hp를 json 기준값으로 설정하는 메소드
    /// </summary>
    /// <param name="data">적 데이터</param>
    public EnemySlot(EnemyDataNode data)
    {
        enemyData = data;
        hp = data.hp;
    }

    public bool isDead => hp <= 0; //적의 생존 여부, 변수 사용시 hp를 사용하여 자동 계산
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
    public Dictionary<string, EnemyDataNode> enemyData = new Dictionary<string, EnemyDataNode>();

    public PlayerStats player;

    // 승/패 후 이동할 노드 키 (원하면 EnterBattleTurn 호출 때 인자로 받기)
    private string _nextOnWin;
    private string _nextOnLose;

    // 전투 턴 순서(플레이어= null, 적 = EnemySlot)
    private List<object> _turnOrder; // object: null 또는 EnemySlot
    private int _turnIndex;
    private Coroutine _battleRoutine;

    // ------------- 외부에서 시작하는 진입점 -------------
    /// <summary>
    /// 전투 진입 메소드, 해당 메소드를 통해 전투 시작
    /// currentEnemyList: null = 플레이어 턴, EnemyDataNode = 적 턴
    /// nextOnWin/nextOnLose: 전투 종료 후 넘어갈 노드 키
    /// </summary>
    public void EnterBattleTurn(List<string> battleOrder, string nextOnWin, string nextOnLose)
    {
        _nextOnWin  = nextOnWin;
        _nextOnLose = nextOnLose;

        // 기존 루틴 정리
        if (_battleRoutine != null) {
            StopCoroutine(_battleRoutine);
            _battleRoutine = null;
        }

        _turnOrder = BuildTurnOrder(battleOrder); //적 리스트를 받아 슬롯으로 인스턴스화

        if (_turnOrder == null || _turnOrder.Count == 0) { //배틀 턴이 비었다면 그대로 종료
            Debug.LogWarning($"[{GetType().Name}] 빈 턴 순서입니다.");
            OnBattleFinished(false);
            return;
        }

        _turnIndex = 0;
        _battleRoutine = StartCoroutine(BattleLoop()); //메인 전투 실행
    }

    /// <summary>
    /// 적 리스트를 받아 인스턴스(슬롯)로 바꾸는 메소드
    /// </summary>
    /// <param name="battleOrder">전투 순서가 담긴 리스트</param>
    /// <returns>인스턴스화 된 적 리스트</returns>
    private List<object> BuildTurnOrder(List<string> battleOrder)
    {
        var order = new List<object>(battleOrder.Count);
        foreach (var enemyCode in battleOrder)
        {
            if (enemyCode == "player") //플레이어 턴
            {
                order.Add(null);
            }
            else //적 턴
            {
                //적 인스턴스 생성, EnemyDataNode에서 스탯을 읽고 hp만 런타임 관리
                if (!enemyData.TryGetValue(enemyCode, out var tpl) || tpl == null)
                {
                    Debug.LogError($"[{GetType().Name}] 적 데이터를 찾을 수 없습니다: {enemyCode}");
                    continue;
                }
                order.Add(new EnemySlot(tpl));
            }
        }
        return order;
    }

    /// <summary>
    /// 메인 전투 루프를 실행하는 메소드
    /// </summary>
    private IEnumerator BattleLoop()
    {
        while (true) {
            // 전투 종료 체크
            if (AllEnemiesDead()) { OnBattleFinished(true); yield break; } //모든 적 사망 (승리)
            if (player.hp <= 0) { OnBattleFinished(false); yield break; } //플레이어 사망 (패배)

            var slot = _turnOrder[_turnIndex]; //현재 전투 대상 확인

            if (slot == null) {
                //--------- 플레이어 턴 ---------
                var target = FindFirstAliveEnemy(); //살아있는 적 찾기

                if (target == null)
                {
                    OnBattleFinished(true); //살아있는 적이 없을 경우 전투 종료 (승리)
                    yield break;
                }

                int damage = ComputePlayerDamage(target); //플레이어 데미지 계산
                target.hp = Mathf.Max(0, target.hp - damage); //적 체력 수정
                Debug.Log($"[{GetType().Name}] 유저 -> 적:{target.enemyData.name} | 데미지:{damage} (HP {target.hp}/{target.enemyData.hp})");

                if (AllEnemiesDead()) { //모든 적이 죽었을 경우 전투 종료(승리)
                    OnBattleFinished(true);
                    yield break;
                }
            } else {
                //--------- 적 턴 ---------
                var enemy = (EnemySlot)slot; //slot을 enemy로 전환
                
                if (!enemy.isDead) //적이 죽었는지 확인, 죽은 적 슬롯은 스킵
                {
                    int damage = ComputeEnemyDamage(enemy); //적 데미지 계산
                    player.hp = Mathf.Max(0, player.hp - damage); //플레이어 체력 수정
                    Debug.Log($"[{GetType().Name}] 적:{enemy.enemyData.name} -> 유저 | 데미지:{damage} (HP {player.hp}/{player.hp})");

                    if (player.hp <= 0) //유저의 체력이 0이 되었을 경우 전투 종료 (패배)
                    {
                        OnBattleFinished(false);
                        yield break;
                    }
                }
            }

            _turnIndex = (_turnIndex + 1) % _turnOrder.Count; //인덱스 증가

            // 연출 딜레이 (원하면 조절/삭제)
            yield return null; // or yield return new WaitForSeconds(0.15f);
        }
    }

    //------------- 유틸/계산 -------------
    /// <summary>
    /// 적이 전멸했는지 확인하는 메소드
    /// </summary>
    /// <returns>하나라도 살아 있으면 false, 아니라면 true</returns>
    private bool AllEnemiesDead()
    {
        for (int i = 0; i < _turnOrder.Count; i++)
        {
            var slot = _turnOrder[i];
            if (slot is EnemySlot e && !e.isDead) return false;
        }
        return true;
    }

    //해당 메소드를 적을 골라 때리는 방식으로 변환 필요
    private EnemySlot FindFirstAliveEnemy()
    {
        for (int i = 0; i < _turnOrder.Count; i++)
        {
            var slot = _turnOrder[i];
            if (slot is EnemySlot e && !e.isDead) return e;
        }
        return null;
    }

    /// <summary>
    /// 플레이어 데미지 계산 메소드
    /// </summary>
    /// <param name="target">타겟 슬롯</param>
    /// <returns>데미지 값</returns>
    private int ComputePlayerDamage(EnemySlot target)
    {
        return player.atk;
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
        Debug.Log(win ? "[Battle] 승리" : "[Battle] 패배");

        // 기존 루틴 정리
        if (_battleRoutine != null)
        {
            StopCoroutine(_battleRoutine);
            _battleRoutine = null;
        }

        // 여기서 당신의 노드 시스템과 연결하세요:
        // StartDialogue(win ? _nextOnWin : _nextOnLose);
    }
}
