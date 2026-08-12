using UnityEngine;

/// <summary>
/// 地块状态机
/// 空地 → 锄地 → 播种 → 浇水 → 发芽 → 生长阶段×N → 成熟（可收获）
/// </summary>
public enum FarmTileState
{
    Empty,       // 未开垦空地
    Hoed,        // 已锄地，可播种
    Seeded,      // 已播种，未浇水
    Watered,     // 已播种并浇水
    Sprouting,   // 发芽中
    Growing,     // 生长中
    Mature,      // 成熟，可收获
    Withered     // 枯萎（过季/缺水过久）
}

/// <summary>
/// 农场地块 — 挂到 Tilemap 上的每个可种植格子
/// 管理单个地块的完整生命周期
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FarmTile : MonoBehaviour
{
    [Header("地块索引")]
    [SerializeField] private int tileIndex = -1;

    private SpriteRenderer _sr;
    private FarmTileState _state = FarmTileState.Empty;
    private CropData _crop;
    private int _growthDayCounter;
    private int _currentStage;
    private bool _wateredToday;
    private CropQuality _quality = CropQuality.Normal;
    private int _consecutiveWaterDays;   // 连续浇水天数（影响品质）
    private int _dryDays;               // 连续未浇水天数
    private bool _fertilized;           // 是否施肥
    private bool _isGiant;              // 是否为巨化作物

    // 存档用
    [System.Serializable]
    public struct FarmTileSaveData
    {
        public int tileIndex;
        public int state;
        public string cropId;
        public int growthDayCounter;
        public int currentStage;
        public bool wateredToday;
        public int quality;
        public int consecutiveWaterDays;
        public int dryDays;
        public bool fertilized;
        public bool isGiant;
    }

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>当前状态</summary>
    public FarmTileState State => _state;
    public CropData Crop => _crop;
    public CropQuality Quality => _quality;
    public bool IsGiant => _isGiant;
    public bool IsWatered => _wateredToday;
    public int TileIndex => tileIndex;
    public bool HasCrop => _crop != null && _state != FarmTileState.Empty && _state != FarmTileState.Hoed && _state != FarmTileState.Withered;

    /// <summary>设置地块索引（由 FarmingManager 分配）</summary>
    public void SetTileIndex(int index) => tileIndex = index;

    // ==================== 玩家操作 ====================

    /// <summary>锄地：空地 → 已锄</summary>
    public bool Till()
    {
        if (_state != FarmTileState.Empty) return false;
        SetState(FarmTileState.Hoed);
        return true;
    }

    /// <summary>播种：已锄 → 已播种</summary>
    public bool Plant(CropData crop)
    {
        if (_state != FarmTileState.Hoed) return false;
        if (crop == null) return false;

        // 季节检查
        if (GameManager.Instance != null && !crop.IsValidSeason(GameManager.Instance.season))
        {
            Debug.Log($"{crop.cropName} 不适合本季节种植");
            return false;
        }

        _crop = crop;
        _growthDayCounter = 0;
        _currentStage = 0;
        _consecutiveWaterDays = 0;
        _dryDays = 0;
        _quality = CropQuality.Normal;
        _isGiant = false;
        SetState(FarmTileState.Seeded);
        UpdateVisual();
        EventBus.Publish(GameEvent.CropPlanted, crop.cropId);
        return true;
    }

    /// <summary>浇水</summary>
    public bool Water()
    {
        if (_state == FarmTileState.Empty || _state == FarmTileState.Hoed) return false;
        if (_state == FarmTileState.Withered) return false;
        if (_wateredToday) return false;

        _wateredToday = true;
        _consecutiveWaterDays++;

        // 已播种但未浇水的 → 浇水后转为浇水状态
        if (_state == FarmTileState.Seeded)
            SetState(FarmTileState.Watered);

        UpdateVisual();
        return true;
    }

    /// <summary>施肥（灵肥：品质提升一级）</summary>
    public bool Fertilize()
    {
        if (_state == FarmTileState.Empty || _state == FarmTileState.Withered) return false;
        _fertilized = true;
        return true;
    }

    /// <summary>收获 — 返回 (物品ID, 数量, 品质)</summary>
    public (string itemId, int count, CropQuality quality, bool giant) Harvest()
    {
        if (_state != FarmTileState.Mature || _crop == null)
            return (null, 0, CropQuality.Normal, false);

        // 巨化判定：连续晴天 + 金星品质 + 概率
        if (_crop.isGiantable && _quality >= CropQuality.Gold && Random.value < _crop.giantChance)
            _isGiant = true;

        int yield = _isGiant
            ? Random.Range(_crop.yieldMin, _crop.yieldMax + 1) * _crop.giantYieldMultiplier
            : Random.Range(_crop.yieldMin, _crop.yieldMax + 1);

        // 施肥提升品质
        if (_fertilized && _quality < CropQuality.Purple)
            _quality++;

        string itemId = _crop.cropItemId;
        CropQuality quality = _quality;
        bool giant = _isGiant;

        // 重置地块到锄地状态
        ResetToHoed();

        EventBus.Publish(GameEvent.CropHarvested, itemId);
        return (itemId, yield, quality, giant);
    }

    /// <summary>清除作物（锄掉枯萎/错误种植的作物）</summary>
    public bool ClearCrop()
    {
        if (_state == FarmTileState.Empty || _state == FarmTileState.Hoed) return false;
        ResetToHoed();
        return true;
    }

    // ==================== 跨天推进（由 FarmingManager 调用） ====================

    /// <summary>跨天推进生长</summary>
    public void AdvanceDay()
    {
        if (_crop == null) return;
        if (_state == FarmTileState.Empty || _state == FarmTileState.Hoed || _state == FarmTileState.Withered) return;

        // 过季检查
        if (GameManager.Instance != null && !_crop.IsValidSeason(GameManager.Instance.season))
        {
            SetState(FarmTileState.Withered);
            UpdateVisual();
            return;
        }

        // 缺水处理
        if (!_wateredToday)
        {
            _dryDays++;
            _consecutiveWaterDays = 0;

            // 连续2天不浇水 → 枯萎
            if (_dryDays >= 2 && _state != FarmTileState.Mature)
            {
                SetState(FarmTileState.Withered);
                UpdateVisual();
                return;
            }
        }
        else
        {
            _dryDays = 0;
        }

        // 已浇水 → 推进生长
        if (_wateredToday)
        {
            _growthDayCounter++;

            // 品质判定：连续浇水 + 晴天 → 品质提升
            EvaluateQuality();

            // 检查阶段跃迁
            int daysPerStage = Mathf.Max(1, _crop.growthDaysTotal / _crop.growthStages);

            if (_growthDayCounter >= daysPerStage && _currentStage < _crop.growthStages - 1)
            {
                _currentStage++;
                UpdateStage();
            }

            // 检查是否成熟
            if (_growthDayCounter >= _crop.growthDaysTotal)
            {
                SetState(FarmTileState.Mature);
                _currentStage = _crop.growthStages - 1;
            }
        }

        // 重置每日浇水标记
        _wateredToday = false;
        UpdateVisual();
    }

    /// <summary>外部强制浇水（洒水器/雨天）</summary>
    public void AutoWater()
    {
        if (_state == FarmTileState.Empty || _state == FarmTileState.Hoed) return;
        if (_state == FarmTileState.Withered) return;
        Water();
    }

    /// <summary>天气强制枯萎（雷暴冲毁低洼地块）</summary>
    public void ForceWither()
    {
        if (_crop == null) return;
        SetState(FarmTileState.Withered);
        UpdateVisual();
    }

    /// <summary>天气冻伤（大雪 → 品质降1档）</summary>
    public void ApplyFrostDamage()
    {
        if (!HasCrop) return;
        if (_quality > CropQuality.Normal)
            _quality--;
    }

    // ==================== 内部逻辑 ====================

    private void EvaluateQuality()
    {
        // 连续浇水天数越多 → 品质越高
        if (_consecutiveWaterDays >= _crop.growthDaysTotal)
        {
            // 连续浇满整个生长期
            if (_quality < CropQuality.Gold) _quality = CropQuality.Gold;
        }
        else if (_consecutiveWaterDays >= _crop.growthDaysTotal / 2)
        {
            if (_quality < CropQuality.Silver) _quality = CropQuality.Silver;
        }

        // 施肥额外加成
        if (_fertilized && _quality < CropQuality.Silver)
            _quality = CropQuality.Silver;
    }

    private void UpdateStage()
    {
        if (_state == FarmTileState.Seeded || _state == FarmTileState.Watered)
        {
            if (_currentStage >= 1)
                SetState(FarmTileState.Sprouting);
            if (_currentStage >= 2)
                SetState(FarmTileState.Growing);
        }
    }

    private void SetState(FarmTileState newState)
    {
        if (_state == newState) return;
        _state = newState;
        EventBus.Publish(GameEvent.FarmTileStateChanged, tileIndex);
    }

    private void UpdateVisual()
    {
        if (_sr == null) return;

        switch (_state)
        {
            case FarmTileState.Empty:
                _sr.sprite = null;
                _sr.color = new Color(0.4f, 0.3f, 0.2f, 0.5f);
                break;
            case FarmTileState.Hoed:
                _sr.sprite = null;
                _sr.color = new Color(0.35f, 0.25f, 0.15f, 0.8f);
                break;
            case FarmTileState.Withered:
                _sr.color = new Color(0.3f, 0.3f, 0.2f, 0.6f);
                break;
            default:
                // 有作物时显示阶段精灵
                if (_crop != null && _crop.stageSprites != null && _crop.stageSprites.Length > 0)
                {
                    int idx = Mathf.Clamp(_currentStage, 0, _crop.stageSprites.Length - 1);
                    _sr.sprite = _crop.stageSprites[idx];
                    _sr.color = Color.white;
                }
                break;
        }
    }

    private void ResetToHoed()
    {
        _crop = null;
        _growthDayCounter = 0;
        _currentStage = 0;
        _consecutiveWaterDays = 0;
        _dryDays = 0;
        _quality = CropQuality.Normal;
        _fertilized = false;
        _isGiant = false;
        _wateredToday = false;
        SetState(FarmTileState.Hoed);
        UpdateVisual();
    }

    // ==================== 存档 ====================

    public FarmTileSaveData GetSaveData()
    {
        return new FarmTileSaveData
        {
            tileIndex = tileIndex,
            state = (int)_state,
            cropId = _crop != null ? _crop.cropId : "",
            growthDayCounter = _growthDayCounter,
            currentStage = _currentStage,
            wateredToday = _wateredToday,
            quality = (int)_quality,
            consecutiveWaterDays = _consecutiveWaterDays,
            dryDays = _dryDays,
            fertilized = _fertilized,
            isGiant = _isGiant
        };
    }

    public void LoadSaveData(FarmTileSaveData data)
    {
        tileIndex = data.tileIndex;
        _state = (FarmTileState)data.state;
        _growthDayCounter = data.growthDayCounter;
        _currentStage = data.currentStage;
        _wateredToday = data.wateredToday;
        _quality = (CropQuality)data.quality;
        _consecutiveWaterDays = data.consecutiveWaterDays;
        _dryDays = data.dryDays;
        _fertilized = data.fertilized;
        _isGiant = data.isGiant;

        // 加载作物数据
        if (!string.IsNullOrEmpty(data.cropId))
            _crop = Resources.Load<CropData>($"Crops/{data.cropId}");

        UpdateVisual();
    }
}
