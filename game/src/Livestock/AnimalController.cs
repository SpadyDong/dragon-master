using UnityEngine;

/// <summary>
/// 畜禽运行时控制器
/// 管理单个畜禽的移动、喂食、抚摸、产出、放牧、游荡
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class AnimalController : MonoBehaviour
{
    [Header("数据")]
    [SerializeField] private AnimalData data;

    [Header("移动")]
    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float wanderInterval = 3f;
    [SerializeField] private float arriveThreshold = 0.15f;

    [Header("交互")]
    [SerializeField] private float interactRange = 1.5f;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Interactable _interactable;

    // 运行时状态
    private int _mood = 50;
    private bool _fedToday;
    private bool _pettedToday;
    private bool _productReady;
    private int _daysSinceLastProduct;
    private bool _isGrazing;
    private bool _isFrightened;     // 雷暴受惊

    // 游荡
    private Vector2 _homePosition;
    private Vector2 _wanderTarget;
    private float _wanderTimer;
    private bool _isMoving;
    private int _walkFrame;
    private float _frameTimer;

    // 存档
    [System.Serializable]
    public struct AnimalSaveData
    {
        public string animalId;
        public int mood;
        public bool fedToday;
        public bool pettedToday;
        public bool productReady;
        public int daysSinceLastProduct;
        public float homeX, homeY;
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();

        _interactable = GetComponent<Interactable>();
        if (_interactable == null)
        {
            _interactable = gameObject.AddComponent<Interactable>();
            _interactable.type = InteractType.Talk;
            _interactable.promptText = $"按 F 与{data?.animalName ?? "动物"}互动";
            _interactable.interactRange = interactRange;
        }
    }

    void Start()
    {
        _homePosition = _rb.position;
        _wanderTarget = _homePosition;

        if (data != null)
        {
            _mood = data.moodMax / 2; // 初始心情为最大值的一半
        }

        EventBus.Subscribe<int>(GameEvent.DayChanged, OnDayChanged);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.DayChanged, OnDayChanged);
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        UpdateWander();
        UpdateAnimation();
    }

    // ==================== 玩家操作 ====================

    /// <summary>喂食</summary>
    public bool Feed()
    {
        if (_fedToday) return false;
        if (data == null) return false;

        // 消耗饲料（金币）
        if (GameManager.Instance != null && !GameManager.Instance.SpendGold(data.feedCost))
            return false;

        _fedToday = true;
        ChangeMood(8);
        EventBus.Publish(GameEvent.AnimalFed, data.animalId);
        return true;
    }

    /// <summary>抚摸</summary>
    public bool Pet()
    {
        if (_pettedToday) return false;

        _pettedToday = true;
        ChangeMood(5);
        EventBus.Publish(GameEvent.AnimalPetted, data.animalId);
        return true;
    }

    /// <summary>收集产出</summary>
    public (string productId, int count) CollectProduct()
    {
        if (!_productReady || data == null) return (null, 0);

        _productReady = false;
        _daysSinceLastProduct = 0;

        // 心情度影响产量倍率（0-100 → 0%~150%）
        float moodMultiplier = Mathf.Clamp01((float)_mood / data.moodMax) * 1.5f;
        int count = Mathf.Max(1, Mathf.RoundToInt(moodMultiplier));

        string productId = data.primaryProduct;
        EventBus.Publish(GameEvent.AnimalProductCollected, productId);
        return (productId, count);
    }

    /// <summary>出售/屠宰 → 获得副产品</summary>
    public (string productId, int count) Slaughter()
    {
        if (data == null) return (null, 0);

        string productId = data.secondaryProduct;
        int count = Mathf.RoundToInt(1 + (float)_mood / data.moodMax * 2);
        return (productId, count);
    }

    // ==================== 跨天 ====================

    private void OnDayChanged(int day)
    {
        AdvanceDay();
    }

    /// <summary>跨天推进（由 LivestockManager 调用）</summary>
    public void AdvanceDay()
    {
        if (data == null) return;

        // 1. 未喂食 → 心情下降
        if (!_fedToday)
            ChangeMood(-data.dailyMoodDecay);

        // 2. 未抚摸 → 额外小幅下降
        if (!_pettedToday)
            ChangeMood(-2);

        // 3. 产出计时
        _daysSinceLastProduct++;
        if (_daysSinceLastProduct >= data.productionInterval && !_productReady)
        {
            // 只有心情度 >= 20 才产出
            if (_mood >= 20)
                _productReady = true;
        }

        // 4. 放牧检查（好天气）
        UpdateGrazingStatus();

        // 5. 松露检查（猪，秋冬野外放牧 1%）
        if (data.canFindTruffle && _isGrazing)
        {
            if (GameManager.Instance != null)
            {
                int season = GameManager.Instance.season;
                if ((season == 2 || season == 3) && Random.value < 0.01f)
                {
                    InventoryManager.Instance?.AddItem("truffle", 1);
                    Debug.Log($"{data.animalName} 在放牧时找到了松露！");
                }
            }
        }

        // 6. 重置每日标记
        _fedToday = false;
        _pettedToday = false;
    }

    /// <summary>天气联动（由 LivestockManager 调用）</summary>
    public void ApplyWeatherEffects(int weatherIndex)
    {
        switch (weatherIndex)
        {
            case 2: // 小雨
                // 回舍躲雨，无放牧产出
                _isGrazing = false;
                ChangeMood(-3);
                break;
            case 4: // 雷暴
                // 受惊，当日不产奶/蛋
                _isFrightened = true;
                _productReady = false;
                _daysSinceLastProduct = 0; // 重置产出计时
                ChangeMood(-10);
                break;
            case 6: // 大雪
                // 需额外保暖，否则掉品质（心情下降）
                ChangeMood(-5);
                _isGrazing = false;
                break;
        }
    }

    // ==================== 放牧 ====================

    private void UpdateGrazingStatus()
    {
        if (!data.canGraze) return;
        if (WeatherSystem.Instance == null) return;

        // 晴天/多云可以放牧
        int weather = GameManager.Instance != null ? GameManager.Instance.weatherIndex : 0;
        bool goodWeather = weather == 0 || weather == 1;

        if (goodWeather)
        {
            _isGrazing = true;
            ChangeMood(data.grazingMoodBonus);
        }
        else
        {
            _isGrazing = false;
        }
    }

    // ==================== 心情 ====================

    private void ChangeMood(int delta)
    {
        int oldMood = _mood;
        _mood = Mathf.Clamp(_mood + delta, 0, data != null ? data.moodMax : 100);

        if (_mood != oldMood)
            EventBus.Publish(GameEvent.AnimalMoodChanged, data != null ? data.animalId : "");
    }

    /// <summary>获取心情对产出品质的影响倍率</summary>
    public float GetMoodProductionMultiplier()
    {
        if (data == null) return 1f;
        return Mathf.Clamp01((float)_mood / data.moodMax) * 1.5f;
    }

    // ==================== 游荡移动 ====================

    private void UpdateWander()
    {
        if (_isFrightened)
        {
            // 受惊时原地不动
            return;
        }

        _wanderTimer += Time.deltaTime;

        if (_wanderTimer >= wanderInterval)
        {
            _wanderTimer = 0f;
            PickNewWanderTarget();
        }

        // 移动向游荡目标
        Vector2 currentPos = _rb.position;
        float dist = Vector2.Distance(currentPos, _wanderTarget);

        if (dist > arriveThreshold)
        {
            _isMoving = true;
            Vector2 dir = (_wanderTarget - currentPos).normalized;
            _rb.MovePosition(currentPos + dir * moveSpeed * Time.deltaTime);

            // 朝向翻转
            if (_sr != null && dir.x != 0)
                _sr.flipX = dir.x < 0;
        }
        else
        {
            _isMoving = false;
        }
    }

    private void PickNewWanderTarget()
    {
        Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
        _wanderTarget = _homePosition + randomOffset;
    }

    private void UpdateAnimation()
    {
        if (_sr == null || data == null) return;

        if (!_isMoving)
        {
            _sr.sprite = data.idleSprite;
            return;
        }

        // 步行帧动画（2帧交替）
        _frameTimer += Time.deltaTime;
        if (_frameTimer >= 0.2f)
        {
            _frameTimer -= 0.2f;
            _walkFrame = (_walkFrame + 1) % 2;
        }

        _sr.sprite = _walkFrame == 0 ? data.walkSprite1 : data.walkSprite2;
    }

    // ==================== 属性 ====================

    public AnimalData Data => data;
    public int Mood => _mood;
    public bool FedToday => _fedToday;
    public bool PettedToday => _pettedToday;
    public bool ProductReady => _productReady;
    public bool IsGrazing => _isGrazing;
    public bool IsFrightened => _isFrightened;

    /// <summary>设置数据（购买时动态赋值）</summary>
    public void SetData(AnimalData newData)
    {
        data = newData;
        if (data != null)
            _mood = data.moodMax / 2;
    }

    /// <summary>设置家位置</summary>
    public void SetHome(Vector2 position)
    {
        _homePosition = position;
    }

    // ==================== 存档 ====================

    public AnimalSaveData GetSaveData()
    {
        return new AnimalSaveData
        {
            animalId = data != null ? data.animalId : "",
            mood = _mood,
            fedToday = _fedToday,
            pettedToday = _pettedToday,
            productReady = _productReady,
            daysSinceLastProduct = _daysSinceLastProduct,
            homeX = _homePosition.x,
            homeY = _homePosition.y
        };
    }

    public void LoadSaveData(AnimalSaveData saveData)
    {
        if (!string.IsNullOrEmpty(saveData.animalId))
            data = Resources.Load<AnimalData>($"Animals/{saveData.animalId}");

        _mood = saveData.mood;
        _fedToday = saveData.fedToday;
        _pettedToday = saveData.pettedToday;
        _productReady = saveData.productReady;
        _daysSinceLastProduct = saveData.daysSinceLastProduct;
        _homePosition = new Vector2(saveData.homeX, saveData.homeY);
    }
}
