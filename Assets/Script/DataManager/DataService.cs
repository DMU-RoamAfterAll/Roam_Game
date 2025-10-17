using UnityEngine;

/// <summary>
/// 데이터 매니저 통합 서비스
/// </summary>
public class DataService : MonoBehaviour
{
    [Header("assign in inspector")]
    [SerializeField] private EnemyDataManager enemyDataManager;
    [SerializeField] private EnemyScriptManager enemyScriptManager;
    [SerializeField] private ItemDataManager itemDataManager;
    [SerializeField] private StoryFlagManager storyFlagManager;
    [SerializeField] private WeaponDataManager weaponDataManager;

    public EnemyDataManager Enemy => enemyDataManager;
    public EnemyScriptManager EnemyScript => enemyScriptManager;
    public ItemDataManager Item => itemDataManager;
    public StoryFlagManager StoryFlag => storyFlagManager;
    public WeaponDataManager Weapon => weaponDataManager;
}
