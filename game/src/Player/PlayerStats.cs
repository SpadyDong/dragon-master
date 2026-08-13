using UnityEngine;

/// <summary>
/// 玩家属性系统
/// 四维属性（STR/AGI/VIT/INT）+ 生存四要素（HP/体力/饥饿/灵力）
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("四维属性")]
    [SerializeField] private int strength = 5;
    [SerializeField] private int agility = 5;
    [SerializeField] private int vitality = 5;
    [SerializeField] private int intelligence = 5;

    [Header("主元素 — GDD 6.4.2")]
    [Tooltip("玩家主元素（null=未选择，开局默认null）")]
    [SerializeField] private DragonElement? _playerElement = null;

    [Header("生存四要素")]
    [SerializeField] private int currentHP = 150;
    [SerializeField] private int currentStamina = 125;
    [SerializeField] private int currentHunger = 100;
    [SerializeField] private int currentMP = 100;

    // 属性
    public int Strength => strength;
    public int Agility => agility;
    public int Vitality => vitality;
    public int Intelligence => intelligence;

    /// <summary>玩家主元素（null=未选择）— 决定装备元素绑定</summary>
    public DragonElement? PlayerElement
    {
        get => _playerElement;
        private set
        {
            _playerElement = value;
            EventBus.Publish(GameEvent.PlayerElementChanged, value.HasValue ? (int)value.Value : -1);
        }
    }

    // 最大上限（由四维属性推导）
    public int MaxHP => 100 + vitality * 10;
    public int MaxStamina => 100 + vitality * 5;
    public int MaxHunger => 100;
    public int MaxMP => 50 + intelligence * 10;

    // 当前值
    public int CurrentHP
    {
        get => currentHP;
        private set { currentHP = Mathf.Clamp(value, 0, MaxHP); EventBus.Publish(GameEvent.PlayerHPChanged, currentHP); }
    }

    public int CurrentStamina
    {
        get => currentStamina;
        private set { currentStamina = Mathf.Clamp(value, 0, MaxStamina); EventBus.Publish(GameEvent.PlayerStaminaChanged, currentStamina); }
    }

    public int CurrentHunger
    {
        get => currentHunger;
        private set { currentHunger = Mathf.Clamp(value, 0, MaxHunger); EventBus.Publish(GameEvent.PlayerHungerChanged, currentHunger); }
    }

    public int CurrentMP
    {
        get => currentMP;
        private set { currentMP = Mathf.Clamp(value, 0, MaxMP); }
    }

    private float _runTickTimer;
    private const float RUN_TICK_INTERVAL = 1f;  // 每秒扣一次体力

    void Awake()
    {
        Instance = this;
        // 初始满状态
        currentHP = MaxHP;
        currentStamina = MaxStamina;
        currentHunger = MaxHunger;
        currentMP = MaxMP;
    }

    /// <summary>设置玩家主元素（剧情/道具触发）</summary>
    public void SetPlayerElement(DragonElement element)
    {
        PlayerElement = element;
    }

    void Start()
    {
        // 订阅深夜惩罚
        EventBus.Subscribe(GameEvent.HourChanged, OnHourChanged);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe(GameEvent.HourChanged, OnHourChanged);
    }

    /// <summary>深夜惩罚：00:00-06:00 每小时扣体力</summary>
    private void OnHourChanged(int hour)
    {
        if (hour >= 0 && hour < 6)
        {
            CurrentStamina -= 3;
        }
    }

    /// <summary>奔跑消耗体力（由 PlayerController 每帧调用）</summary>
    public void TickRunning(float deltaTime)
    {
        _runTickTimer += deltaTime;
        while (_runTickTimer >= RUN_TICK_INTERVAL)
        {
            _runTickTimer -= RUN_TICK_INTERVAL;
            CurrentStamina -= 1;
        }
    }

    /// <summary>消耗体力，返回是否足够</summary>
    public bool ConsumeStamina(int amount)
    {
        if (CurrentStamina < amount) return false;
        CurrentStamina -= amount;
        return true;
    }

    /// <summary>恢复体力</summary>
    public void RestoreStamina(int amount)
    {
        CurrentStamina += amount;
    }

    /// <summary>受到伤害</summary>
    public void TakeDamage(int amount)
    {
        CurrentHP -= amount;
        if (CurrentHP <= 0)
        {
            OnKnockedOut();
        }
    }

    /// <summary>治疗</summary>
    public void Heal(int amount)
    {
        CurrentHP += amount;
    }

    /// <summary>吃东西恢复饥饿和体力</summary>
    public void Eat(int hungerRestore, int staminaRestore)
    {
        CurrentHunger += hungerRestore;
        RestoreStamina(staminaRestore);
    }

    /// <summary>消耗灵力</summary>
    public bool ConsumeMP(int amount)
    {
        if (CurrentMP < amount) return false;
        CurrentMP -= amount;
        return true;
    }

    /// <summary>恢复灵力</summary>
    public void RestoreMP(int amount)
    {
        CurrentMP += amount;
    }

    /// <summary>恢复全部状态（温泉/泡澡用）— GDD 7.12</summary>
    public void RestoreFull()
    {
        CurrentHP = MaxHP;
        CurrentStamina = MaxStamina;
        CurrentMP = MaxMP;
    }

    /// <summary>晕倒：强制回家，丢失部分物品</summary>
    private void OnKnockedOut()
    {
        Debug.LogWarning("玩家晕倒了！");
        GameManager.Instance.AddGold(-Mathf.Min(GameManager.Instance.gold, 100)); // 掉钱
        // 恢复到最低限度
        currentHP = MaxHP / 4;
        currentStamina = MaxStamina / 4;
    }
}
