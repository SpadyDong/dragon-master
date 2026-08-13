using UnityEngine;

/// <summary>
/// 钓鱼 QTE 状态 — 对应 GDD 5.7.3
/// 抛竿 → 等待咬钩 → 咬钩提示 → 拉线QTE → 成功/失败
/// </summary>
public enum FishingQTEState
{
    Casting,    // 抛竿动画
    Waiting,    // 等待咬钩
    Biting,    // 咬钩提示（玩家需按键反应）
    Reeling,   // 拉线 QTE（绿条覆盖鱼图标）
    Success,   // 成功
    Failed     // 失败
}

/// <summary>
/// 钓鱼 QTE 小游戏 — 对应 GDD 5.7.3
/// 状态机：Casting → Waiting → Biting → Reeling → Success/Failed
///
/// QTE 机制：
/// - 绿条在垂直条上下移动
/// - 鱼图标随机上下窜
/// - 玩家按住空格/F控制绿条方向
/// - 绿条覆盖鱼图标时进度条增加，不覆盖时减少
/// - 进度满 → 成功，进度空 → 失败
/// </summary>
public class FishingQTE : MonoBehaviour
{
    [Header("QTE 参数")]
    [SerializeField] private float castingDuration = 1f;
    [SerializeField] private float minWaitTime = 1.5f;
    [SerializeField] private float maxWaitTime = 5f;
    [SerializeField] private float biteWindowDuration = 0.8f;  // 咬钩反应窗口
    [SerializeField] private float barMoveSpeed = 3f;           // 绿条移动速度
    [SerializeField] private float fishMoveSpeed = 2f;          // 鱼图标移动速度
    [SerializeField] private float progressBarMax = 100f;
    [SerializeField] private float progressBarGain = 30f;      // 覆盖时每秒增加
    [SerializeField] private float progressBarLoss = 20f;      // 不覆盖时每秒减少

    // 垂直条范围 (0-1)
    private const float BAR_MIN = 0f;
    private const float BAR_MAX = 1f;

    private FishingQTEState _state = FishingQTEState.Casting;
    private FishingSpot _spot;
    private PlayerController _player;
    private FishData _targetFish;

    // 计时
    private float _stateTimer;
    private float _waitDuration;
    private float _biteWindowTimer;

    // QTE 运行时数据
    private float _barPosition;      // 绿条中心位置 (0-1)
    private float _barSize = 0.2f;   // 绿条大小
    private float _fishPosition;     // 鱼图标位置 (0-1)
    private float _fishTargetPos;    // 鱼目标位置
    private float _fishMoveTimer;
    private float _progress;         // 进度 0-100
    private float _qteDuration;      // QTE 总时长

    // 难度修正
    private float _difficultyModifier = 1.0f;
    private float _reelingSpeed = 1.0f;

    /// <summary>当前 QTE 状态</summary>
    public FishingQTEState State => _state;

    /// <summary>初始化 QTE</summary>
    public void Initialize(FishingSpot spot, PlayerController player)
    {
        _spot = spot;
        _player = player;

        // 随机选择目标鱼
        var available = spot.AvailableFish;
        if (available.Count == 0)
        {
            spot.EndFishing(false, null);
            return;
        }

        // 天气影响稀有鱼概率
        int weather = GameManager.Instance != null ? GameManager.Instance.weatherIndex : 0;
        float rareBonus = FishingManager.Instance?.GetRarityBonus(weather) ?? 0f;

        // 按权重选择鱼
        _targetFish = SelectFish(available, rareBonus);

        // QTE 时长
        _qteDuration = _targetFish.qteDuration;

        // 难度修正
        _difficultyModifier = FishingManager.Instance?.GetQTEDifficultyModifier(weather) ?? 1f;

        // 鱼竿修正
        FishingRodData rod = FishingManager.Instance?.GetCurrentRod();
        if (rod != null)
        {
            _barSize += rod.qteBarSizeBonus;
            _reelingSpeed = rod.reelingSpeed;
        }

        // 初始化位置
        _barPosition = 0.5f;
        _fishPosition = 0.5f;
        _fishTargetPos = 0.5f;
        _progress = progressBarMax * 0.3f; // 起始 30%

        _state = FishingQTEState.Casting;
        _stateTimer = 0f;

        // 锁定玩家移动
        player.LockMovement();
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        switch (_state)
        {
            case FishingQTEState.Casting:
                UpdateCasting();
                break;
            case FishingQTEState.Waiting:
                UpdateWaiting();
                break;
            case FishingQTEState.Biting:
                UpdateBiting();
                break;
            case FishingQTEState.Reeling:
                UpdateReeling();
                break;
        }
    }

    // ==================== 状态更新 ====================

    private void UpdateCasting()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= castingDuration)
        {
            // 进入等待咬钩
            _state = FishingQTEState.Waiting;
            _waitDuration = Random.Range(minWaitTime, maxWaitTime) / _targetFish.biteSpeed;
            _stateTimer = 0f;
        }
    }

    private void UpdateWaiting()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= _waitDuration)
        {
            // 咬钩！
            _state = FishingQTEState.Biting;
            _biteWindowTimer = 0f;
            // TODO: 播放咬钩音效和视觉提示
        }
    }

    private void UpdateBiting()
    {
        _biteWindowTimer += Time.deltaTime;

        // 玩家需要在窗口期内按键
        if (InputManager.Instance != null && InputManager.Instance.ConsumeConfirm())
        {
            // 成功进入拉线阶段
            _state = FishingQTEState.Reeling;
            _stateTimer = 0f;
            return;
        }

        // 超过窗口未按键 → 失败
        if (_biteWindowTimer >= biteWindowDuration)
        {
            Finish(false);
        }
    }

    private void UpdateReeling()
    {
        _stateTimer += Time.deltaTime;

        // 鱼图标随机移动
        UpdateFishMovement();

        // 绿条控制（玩家输入）
        UpdateBarControl();

        // 检查覆盖
        bool isCovering = IsBarCoveringFish();

        // 进度增减
        float speedMod = _reelingSpeed * _difficultyModifier;
        if (isCovering)
            _progress += progressBarGain * speedMod * Time.deltaTime;
        else
            _progress -= progressBarLoss * speedMod * Time.deltaTime;

        _progress = Mathf.Clamp(_progress, 0f, progressBarMax);

        // 胜负判定
        if (_progress >= progressBarMax)
        {
            Finish(true);
        }
        else if (_progress <= 0f)
        {
            Finish(false);
        }
        else if (_stateTimer >= _qteDuration * 2f) // 超时保护
        {
            Finish(_progress >= progressBarMax * 0.5f);
        }
    }

    // ==================== 鱼移动 ====================

    private void UpdateFishMovement()
    {
        _fishMoveTimer += Time.deltaTime;

        // 难度越高，鱼换方向越频繁
        float changeInterval = Mathf.Max(0.3f, 2f - _targetFish.difficulty * 0.15f);

        if (_fishMoveTimer >= changeInterval)
        {
            _fishMoveTimer = 0f;
            // 随机选新目标位置
            float range = Mathf.Min(0.4f, 0.1f + _targetFish.difficulty * 0.04f);
            _fishTargetPos = Mathf.Clamp(_fishPosition + Random.Range(-range, range), 0.1f, 0.9f);
        }

        // 鱼向目标移动
        float fishSpeed = fishMoveSpeed * (1f + _targetFish.difficulty * 0.1f);
        _fishPosition = Mathf.MoveTowards(_fishPosition, _fishTargetPos, fishSpeed * Time.deltaTime);
    }

    // ==================== 绿条控制 ====================

    private void UpdateBarControl()
    {
        // 玩家按住确认键 → 绿条向上移动，松开 → 向下
        bool holdUp = InputManager.Instance != null && (InputManager.Instance.ConsumeConfirm() || Input.GetKey(KeyCode.Space));

        float moveDir = holdUp ? 1f : -1f;
        _barPosition += moveDir * barMoveSpeed * Time.deltaTime;
        _barPosition = Mathf.Clamp(_barPosition, BAR_MIN, BAR_MAX);
    }

    // ==================== 覆盖检查 ====================

    private bool IsBarCoveringFish()
    {
        float barMin = _barPosition - _barSize * 0.5f;
        float barMax = _barPosition + _barSize * 0.5f;
        return _fishPosition >= barMin && _fishPosition <= barMax;
    }

    // ==================== 结束 ====================

    private void Finish(bool success)
    {
        _state = success ? FishingQTEState.Success : FishingQTEState.Failed;

        // 解锁玩家移动
        if (_player != null)
            _player.UnlockMovement();

        // 通知钓点
        _spot.EndFishing(success, success ? _targetFish : null);
    }

    // ==================== 鱼选择 ====================

    /// <summary>按稀有度权重选择鱼</summary>
    private FishData SelectFish(System.Collections.Generic.List<FishData> available, float rareBonus)
    {
        // 权重：普通 100 / 罕见 20 / 稀有 5(×(1+bonus)) / 传说 1(×(1+bonus))
        float totalWeight = 0f;
        float[] weights = new float[available.Count];

        for (int i = 0; i < available.Count; i++)
        {
            weights[i] = available[i].rarity switch
            {
                FishRarity.Common   => 100f,
                FishRarity.Uncommon => 20f,
                FishRarity.Rare     => 5f * (1f + rareBonus),
                FishRarity.Legendary => 1f * (1f + rareBonus),
                _ => 100f
            };
            totalWeight += weights[i];
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < available.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
                return available[i];
        }

        return available[0];
    }

    // ==================== 调试数据（供 UI 读取） ====================

    /// <summary>当前进度百分比 (0-1)</summary>
    public float ProgressNormalized => _progress / progressBarMax;

    /// <summary>绿条位置 (0-1)</summary>
    public float BarPosition => _barPosition;

    /// <summary>绿条大小</summary>
    public float BarSize => _barSize;

    /// <summary>鱼图标位置 (0-1)</summary>
    public float FishPosition => _fishPosition;

    /// <summary>目标鱼</summary>
    public FishData TargetFish => _targetFish;
}
