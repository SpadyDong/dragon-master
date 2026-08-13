using UnityEngine;

/// <summary>
/// 鱼塘 — 对应 GDD 5.7.4
/// 玩家可建造鱼塘，投放鱼苗繁殖
/// 鱼数量多后会产出鱼籽，鱼籽可加工成鱼子酱（中式：鱼子酱豆腐原料）
/// 鱼塘满员后可收获
/// </summary>
[RequireComponent(typeof(Interactable))]
public class FishPond : MonoBehaviour
{
    [Header("鱼塘设置")]
    [SerializeField] private int maxCapacity = 10;
    [SerializeField] private int breedDays = 3;      // 繁殖周期
    [SerializeField] private int roeThreshold = 5;   // 鱼数量达到此值后开始产出鱼籽
    [SerializeField] private float interactRange = 2f;

    private FishData _fishSpecies;
    private int _fishCount;
    private int _breedTimer;
    private bool _hasRoe;
    private Interactable _interactable;

    // 存档
    [System.Serializable]
    public struct FishPondSaveData
    {
        public string fishId;
        public int fishCount;
        public int breedTimer;
        public bool hasRoe;
        public float posX, posY;
    }

    void Awake()
    {
        _interactable = GetComponent<Interactable>();
        if (_interactable == null)
        {
            _interactable = gameObject.AddComponent<Interactable>();
            _interactable.type = InteractType.Talk;
            _interactable.promptText = "按 F 查看鱼塘";
            _interactable.interactRange = interactRange;
        }
    }

    void Start()
    {
        EventBus.Subscribe<int>(GameEvent.DayChanged, OnDayChanged);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.DayChanged, OnDayChanged);
    }

    // ==================== 玩家操作 ====================

    /// <summary>投放鱼苗</summary>
    public bool StockFish(FishData fish)
    {
        if (fish == null) return false;
        if (_fishSpecies != null && _fishSpecies.fishId != fish.fishId)
        {
            Debug.Log("鱼塘已有其他鱼种，无法混养。");
            return false;
        }

        if (_fishCount >= maxCapacity)
        {
            Debug.Log("鱼塘已满！");
            return false;
        }

        _fishSpecies = fish;
        _fishCount++;
        return true;
    }

    /// <summary>收获鱼</summary>
    public (string fishId, int count) HarvestFish()
    {
        if (_fishCount <= 0) return (null, 0);

        // 收获一半的鱼（至少1条）
        int harvestCount = Mathf.Max(1, _fishCount / 2);
        _fishCount -= harvestCount;

        if (_fishCount <= 0)
        {
            _fishSpecies = null;
            _breedTimer = 0;
        }

        string fishId = _fishSpecies != null ? _fishSpecies.fishId : null;
        EventBus.Publish(GameEvent.FishPondHarvested, fishId);
        return (fishId, harvestCount);
    }

    /// <summary>收获鱼籽</summary>
    public string HarvestRoe()
    {
        if (!_hasRoe) return null;

        _hasRoe = false;
        return "fish_roe";
    }

    // ==================== 跨天 ====================

    private void OnDayChanged(int day)
    {
        AdvanceDay();
    }

    /// <summary>跨天推进</summary>
    public void AdvanceDay()
    {
        if (_fishSpecies == null || _fishCount <= 0) return;

        // 繁殖计时
        _breedTimer++;

        if (_breedTimer >= breedDays)
        {
            _breedTimer = 0;

            // 繁殖：至少有2条鱼才能繁殖
            if (_fishCount >= 2 && _fishCount < maxCapacity)
            {
                _fishCount++;
                Debug.Log($"鱼塘繁殖成功！当前数量: {_fishCount}/{maxCapacity}");
            }
        }

        // 鱼籽产出
        if (_fishCount >= roeThreshold && !_hasRoe)
        {
            if (Random.value < 0.3f) // 30% 概率产出鱼籽
            {
                _hasRoe = true;
                Debug.Log("鱼塘产出了鱼籽！");
            }
        }
    }

    // ==================== 属性 ====================

    public FishData FishSpecies => _fishSpecies;
    public int FishCount => _fishCount;
    public int MaxCapacity => maxCapacity;
    public bool HasRoe => _hasRoe;
    public bool IsEmpty => _fishCount <= 0;
    public bool IsFull => _fishCount >= maxCapacity;

    // ==================== 存档 ====================

    public FishPondSaveData GetSaveData()
    {
        return new FishPondSaveData
        {
            fishId = _fishSpecies != null ? _fishSpecies.fishId : "",
            fishCount = _fishCount,
            breedTimer = _breedTimer,
            hasRoe = _hasRoe,
            posX = transform.position.x,
            posY = transform.position.y
        };
    }

    public void LoadSaveData(FishPondSaveData data)
    {
        if (!string.IsNullOrEmpty(data.fishId))
            _fishSpecies = Resources.Load<FishData>($"Fishes/{data.fishId}");

        _fishCount = data.fishCount;
        _breedTimer = data.breedTimer;
        _hasRoe = data.hasRoe;
    }
}
