using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 节日管理器 — GDD 7.2
/// 9 个年度节日 + 日期判定 + 节日奖励
/// </summary>
public class FestivalManager : MonoBehaviour
{
    public static FestivalManager Instance { get; private set; }

    /// <summary>所有节日（运行时从 Resources 加载 + 内置默认表兜底）</summary>
    private List<FestivalData> _festivals = new();

    /// <summary>已参与过的节日（避免重复发奖励）</summary>
    private HashSet<string> _participated = new();

    /// <summary>当前正在进行的节日</summary>
    private FestivalData _currentFestival;

    /// <summary>当前节日（null=今日无节日）</summary>
    public FestivalData CurrentFestival => _currentFestival;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadFestivals();
    }

    void Start()
    {
        EventBus.Subscribe<int>(GameEvent.DayChanged, OnDayChanged);
        CheckTodayFestival();
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.DayChanged, OnDayChanged);
    }

    // ==================== 节日数据 ====================

    void LoadFestivals()
    {
        // 优先从 Resources 加载（Assets/Resources/Festivals/）
        var loaded = Resources.LoadAll<FestivalData>("Festivals");
        _festivals = new List<FestivalData>(loaded);

        // 内置默认节日表兜底（GDD 7.2）
        if (_festivals.Count == 0)
        {
            _festivals = new List<FestivalData>();
            AddBuiltin("spring_festival", "春日祭", 0, 13, "花舞比赛、寻找彩蛋、春日料理");
            AddBuiltin("dragon_ceremony", "驯龙大典", 0, 24, "龙赛飞行比赛、龙蛋拍卖会");
            AddBuiltin("sea_festival", "夏之海祭", 1, 11, "钓鱼大赛、沙滩舞蹈、焰火晚会");
            AddBuiltin("dragon_market", "龙脊大集市", 1, 28, "全镇摆摊、限量商品、迷你游戏");
            AddBuiltin("harvest_festival", "秋日丰收祭", 2, 16, "作物评比展、南瓜灯、美食街");
            AddBuiltin("spirit_stone", "灵石祭奠", 2, 30, "元素祈福、灵石折扣、四属性游行");
            AddBuiltin("snow_festival", "冬雪祭", 3, 8, "滑冰、堆雪人、雪花收集");
            AddBuiltin("star_newyear", "星陨除夕", 3, 25, "倒计时、交换礼物、星陨龙概率出现");
            AddBuiltin("new_year", "新年", 3, 28, "全镇祝贺、运势抽签（全年buff）");
        }
    }

    void AddBuiltin(string id, string name, int season, int day, string activities)
    {
        var festival = ScriptableObject.CreateInstance<FestivalData>();
        festival.festivalId = id;
        festival.festivalName = name;
        festival.season = season;
        festival.day = day;
        festival.activities = activities;
        festival.rewardGold = 100;
        festival.rewardAffection = 10;
        _festivals.Add(festival);
    }

    // ==================== 节日判定 ====================

    void OnDayChanged(int day)
    {
        CheckTodayFestival();
    }

    /// <summary>检查今天是否是节日</summary>
    public void CheckTodayFestival()
    {
        if (GameManager.Instance == null) return;
        int season = GameManager.Instance.season;
        int day = GameManager.Instance.day;

        _currentFestival = null;
        foreach (var festival in _festivals)
        {
            if (festival.IsToday(season, day))
            {
                _currentFestival = festival;
                break;
            }
        }

        if (_currentFestival != null && !_participated.Contains(_currentFestival.festivalId))
        {
            _participated.Add(_currentFestival.festivalId);
            OnFestivalStarted(_currentFestival);
        }
    }

    void OnFestivalStarted(FestivalData festival)
    {
        EventBus.Publish(GameEvent.FestivalStarted, festival.festivalId);

        // 发放节日奖励
        if (festival.rewardGold > 0)
            GameManager.Instance?.AddGold(festival.rewardGold);
        if (!string.IsNullOrEmpty(festival.rewardItemId))
            InventoryManager.Instance?.AddItem(festival.rewardItemId, 1);

        // 全体 NPC 好感
        if (festival.rewardAffection > 0)
            EventBus.Publish(GameEvent.NPCAffectionChanged, festival.rewardAffection);

        Debug.Log($"节日: {festival.festivalName}（{festival.activities}）");
    }

    // ==================== 查询 ====================

    /// <summary>获取所有节日</summary>
    public List<FestivalData> GetAllFestivals() => _festivals;

    /// <summary>根据季节日期查找节日</summary>
    public FestivalData GetFestival(int season, int day)
    {
        foreach (var festival in _festivals)
            if (festival.season == season && festival.day == day)
                return festival;
        return null;
    }

    // ==================== 存档 ====================

    [System.Serializable]
    public class FestivalSaveData
    {
        public List<string> participatedIds;
    }

    public FestivalSaveData GetSaveData()
    {
        return new FestivalSaveData { participatedIds = new List<string>(_participated) };
    }

    public void LoadSaveData(FestivalSaveData data)
    {
        if (data?.participatedIds == null) return;
        _participated = new HashSet<string>(data.participatedIds);
    }
}
