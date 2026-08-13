using UnityEngine;

/// <summary>
/// 龙运行时控制器 — 简化版
/// 龙与普通动物仅两点不同：有元素属性 + 可参加战斗
/// 其余（喂食/抚摸/成长/游荡/繁育）与普通动物一致
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class DragonController : MonoBehaviour
{
    [Header("数据")]
    [SerializeField] private DragonData data;

    [Header("移动")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float wanderRadius = 4f;
    [SerializeField] private float wanderInterval = 4f;
    [SerializeField] private float arriveThreshold = 0.2f;

    [Header("交互")]
    [SerializeField] private float interactRange = 2f;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Interactable _interactable;

    // 身份
    private string _uid;
    private DragonElement _primaryElement;
    private DragonElement? _secondaryElement;
    private bool _isFemale;

    // 成长
    private DragonStage _stage = DragonStage.Egg;
    private int _growthDay;
    private int _eggHatchProgress;

    // 喂食/抚摸/受惊
    private bool _fedToday;
    private bool _pettedToday;
    private bool _isFrightened;

    // 繁育
    private int _eggsLaid;
    private bool _isInDen;

    // 战斗（M9 预留）
    private int _currentHP;
    private bool _isWild;

    // 游荡
    private Vector2 _homePosition;
    private Vector2 _wanderTarget;
    private float _wanderTimer;
    private bool _isMoving;

    // 存档
    [System.Serializable]
    public struct DragonSaveData
    {
        public string uid;
        public string speciesId;
        public int primaryElement;
        public int secondaryElement;
        public bool isFemale;
        public int stage;
        public int growthDay;
        public int eggHatchProgress;
        public bool fedToday, pettedToday;
        public int eggsLaid;
        public bool isInDen;
        public int currentHP;
        public bool isWild;
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
            _interactable.promptText = $"按 F 与{data?.speciesName ?? "龙"}互动";
            _interactable.interactRange = interactRange;
        }
    }

    void Start()
    {
        _homePosition = _rb.position;
        _wanderTarget = _homePosition;
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
    }

    // ==================== 初始化 ====================

    public void Initialize(DragonData species, DragonGender gender, bool isWild = false)
    {
        data = species;
        _uid = System.Guid.NewGuid().ToString("N").Substring(0, 8);
        _isFemale = gender == DragonGender.Female;
        _isWild = isWild;
        _primaryElement = species.element;

        if (species.canBeDualElement && Random.value < 0.05f)
        {
            var others = System.Array.FindAll(
                (DragonElement[])System.Enum.GetValues(typeof(DragonElement)),
                e => e != _primaryElement);
            _secondaryElement = others[Random.Range(0, others.Length)];
        }

        _stage = DragonStage.Egg;
        _growthDay = 0;
        _eggHatchProgress = 0;
        _currentHP = MaxHP;
        UpdateVisual();
    }

    public void InitializeFromEgg(DragonData species, DragonGender gender,
        DragonElement primary, DragonElement? secondary)
    {
        data = species;
        _uid = System.Guid.NewGuid().ToString("N").Substring(0, 8);
        _isFemale = gender == DragonGender.Female;
        _isWild = false;
        _primaryElement = primary;
        _secondaryElement = secondary;

        _stage = DragonStage.Egg;
        _growthDay = 0;
        _eggHatchProgress = 0;
        _currentHP = MaxHP;
        UpdateVisual();
    }

    // ==================== 属性 — 同种龙属性固定，全部来自 DragonData ====================

    public string Uid => _uid;
    public DragonData Data => data;
    public DragonElement PrimaryElement => _primaryElement;
    public DragonElement? SecondaryElement => _secondaryElement;
    public bool IsFemale => _isFemale;
    public DragonStage Stage => _stage;
    public int GrowthDay => _growthDay;
    public bool FedToday => _fedToday;
    public bool PettedToday => _pettedToday;
    public int EggsLaid => _eggsLaid;
    public bool IsInDen => _isInDen;
    public bool IsWild => _isWild;
    public bool IsFrightened => _isFrightened;

    public int Attack => data != null ? data.baseAttack : 0;
    public int Defense => data != null ? data.baseDefense : 0;
    public int Agility => data != null ? data.baseAgility : 0;
    public int MaxHP => data != null ? data.baseMaxHP : 100;
    public int CurrentHP => _currentHP;

    /// <summary>含龙装备加成的攻击力</summary>
    public int GetEquippedAttack()
    {
        int baseAtk = Attack;
        if (EquipmentManager.Instance != null)
            baseAtk += EquipmentManager.Instance.GetDragonAttackBonus();
        return baseAtk;
    }

    /// <summary>含龙装备加成的防御力</summary>
    public int GetEquippedDefense()
    {
        int baseDef = Defense;
        if (EquipmentManager.Instance != null)
            baseDef += EquipmentManager.Instance.GetDragonDefenseBonus();
        return baseDef;
    }

    /// <summary>含季节加成+龙装备的有效攻击力</summary>
    public int GetEffectiveAttack()
    {
        int season = GameManager.Instance != null ? GameManager.Instance.season : 0;
        float bonus = DragonElementUtils.GetSeasonalBonus(_primaryElement, season);
        return Mathf.RoundToInt(GetEquippedAttack() * (1f + bonus));
    }

    // ==================== 玩家操作 ====================

    public bool Feed()
    {
        if (_fedToday || data == null || _stage == DragonStage.Egg) return false;
        if (GameManager.Instance != null && !GameManager.Instance.SpendGold(5)) return false;
        _fedToday = true;
        EventBus.Publish(GameEvent.DragonFed, _uid);
        return true;
    }

    public bool Pet()
    {
        if (_pettedToday || _stage == DragonStage.Egg) return false;
        _pettedToday = true;
        EventBus.Publish(GameEvent.DragonPetted, _uid);
        return true;
    }

    public void SetInDen(bool inDen) => _isInDen = inDen;

    // ==================== 捕捉 ====================

    public bool TryCapture(float hpPercent, string captureItem)
    {
        if (!_isWild || data == null) return false;

        float baseRate = Mathf.Lerp(0.05f, 0.80f, 1f - hpPercent);
        float itemBonus = captureItem switch
        {
            "fresh_meat"  => 0.10f,
            "rope_net"    => 0.15f,
            "anesthetic"  => 0.25f,
            _ => 0f
        };
        float difficultyPenalty = data.captureDifficulty * 0.03f;
        float totalRate = Mathf.Clamp01(baseRate + itemBonus - difficultyPenalty);

        if (Random.value < totalRate)
        {
            _isWild = false;
            EventBus.Publish(GameEvent.DragonCaptured, data.speciesId);
            return true;
        }
        return false;
    }

    // ==================== 产蛋 ====================

    public bool CanLayEgg(int currentSeason)
    {
        if (!_isFemale || _stage != DragonStage.Adult) return false;
        if (_eggsLaid >= 2 || !_isInDen) return false;
        return currentSeason == 0 || currentSeason == 2;
    }

    public void OnEggLaid()
    {
        _eggsLaid++;
        EventBus.Publish(GameEvent.DragonEggLaid, _uid);
    }

    // ==================== 跨天推进 ====================

    private void OnDayChanged(int day) => AdvanceDay();

    public void AdvanceDay()
    {
        if (data == null) return;

        int weather = GameManager.Instance != null ? GameManager.Instance.weatherIndex : 0;
        ApplyWeatherEffects(weather);

        if (_stage == DragonStage.Egg)
        {
            _eggHatchProgress++;
            if (_eggHatchProgress >= data.eggHatchDays) Hatch();
            return;
        }

        _growthDay++;

        DragonStage newStage = data.GetStageForGrowthDay(_growthDay);
        if (newStage != _stage) TransitionStage(newStage);

        if (_currentHP < MaxHP)
            _currentHP = Mathf.Min(MaxHP, _currentHP + MaxHP / 10);

        _fedToday = false;
        _pettedToday = false;
        _isFrightened = false;
    }

    private void TransitionStage(DragonStage newStage)
    {
        _stage = newStage;
        _currentHP = MaxHP;
        UpdateVisual();
        EventBus.Publish(GameEvent.DragonStageChanged, _uid);
    }

    private void Hatch()
    {
        _stage = DragonStage.Juvenile;
        _growthDay = data.juvenileStartDay;
        _currentHP = MaxHP;
        UpdateVisual();
        EventBus.Publish(GameEvent.DragonHatched, _uid);
        EventBus.Publish(GameEvent.DragonStageChanged, _uid);
    }

    // ==================== 天气联动（仅雷暴受惊/逃出龙舍） ====================

    public void ApplyWeatherEffects(int weatherIndex)
    {
        if (weatherIndex == 4) // 雷暴
        {
            _isFrightened = true;
            if (!_isWild && _isInDen && Random.value < 0.15f)
            {
                _isInDen = false;
                Debug.LogWarning($"{data?.speciesName} 受惊逃出了龙舍！");
            }
        }
    }

    // ==================== 游荡移动 ====================

    private void UpdateWander()
    {
        if (_stage == DragonStage.Egg || _isFrightened) return;

        _wanderTimer += Time.deltaTime;
        if (_wanderTimer >= wanderInterval)
        {
            _wanderTimer = 0f;
            _wanderTarget = _homePosition + Random.insideUnitCircle * wanderRadius;
        }

        Vector2 pos = _rb.position;
        if (Vector2.Distance(pos, _wanderTarget) > arriveThreshold)
        {
            _isMoving = true;
            Vector2 dir = (_wanderTarget - pos).normalized;
            _rb.MovePosition(pos + dir * moveSpeed * Time.deltaTime);
            if (_sr != null && dir.x != 0) _sr.flipX = dir.x < 0;
        }
        else _isMoving = false;
    }

    // ==================== 视觉 ====================

    private void UpdateVisual()
    {
        if (_sr == null || data == null) return;
        _sr.sprite = data.GetStageSprite(_stage);
        transform.localScale = Vector3.one * data.GetVisualScale(_stage);
    }

    // ==================== 存档 ====================

    public DragonSaveData GetSaveData() => new()
    {
        uid = _uid,
        speciesId = data != null ? data.speciesId : "",
        primaryElement = (int)_primaryElement,
        secondaryElement = _secondaryElement.HasValue ? (int)_secondaryElement.Value : -1,
        isFemale = _isFemale,
        stage = (int)_stage,
        growthDay = _growthDay,
        eggHatchProgress = _eggHatchProgress,
        fedToday = _fedToday, pettedToday = _pettedToday,
        eggsLaid = _eggsLaid, isInDen = _isInDen,
        currentHP = _currentHP, isWild = _isWild,
        homeX = _homePosition.x, homeY = _homePosition.y
    };

    public void LoadSaveData(DragonSaveData d)
    {
        if (!string.IsNullOrEmpty(d.speciesId))
            data = Resources.Load<DragonData>($"Dragons/{d.speciesId}");

        _uid = d.uid;
        _primaryElement = (DragonElement)d.primaryElement;
        _secondaryElement = d.secondaryElement >= 0 ? (DragonElement)d.secondaryElement : null;
        _isFemale = d.isFemale;
        _stage = (DragonStage)d.stage;
        _growthDay = d.growthDay;
        _eggHatchProgress = d.eggHatchProgress;
        _fedToday = d.fedToday;
        _pettedToday = d.pettedToday;
        _eggsLaid = d.eggsLaid;
        _isInDen = d.isInDen;
        _currentHP = d.currentHP;
        _isWild = d.isWild;
        _homePosition = new Vector2(d.homeX, d.homeY);
        UpdateVisual();
    }

    public void SetHome(Vector2 pos) => _homePosition = pos;
    public void SetData(DragonData d) => data = d;
}
