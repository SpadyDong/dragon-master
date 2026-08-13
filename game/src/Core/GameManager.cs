using UnityEngine;

/// <summary>
/// 玩家性别 — GDD 3.1（开局选择，影响可结婚对象，仅允许异性结婚）
/// </summary>
public enum PlayerGender
{
    Male,   // 男
    Female  // 女
}

/// <summary>
/// 游戏全局状态管理器（单例）
/// 管理时间、天气、金币、游戏状态等核心数据
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("玩家")]
    [Tooltip("玩家性别（GDD 3.1 开局选择，影响可结婚对象）")]
    public PlayerGender playerGender = PlayerGender.Male;

    [Header("时间")]
    public int year = 1;
    public int season = 0;   // 0=春, 1=夏, 2=秋, 3=冬
    public int day = 1;
    public int hour = 8;
    public int minute = 0;

    [Header("经济")]
    public int gold = 500;

    [Header("天气")]
    public string weather = "晴";
    public int weatherIndex = 0;  // 对应 WeatherNames 索引

    // 游戏状态
    public GameState CurrentState { get; private set; } = GameState.Playing;

    // 当前场景
    public string currentScene = "Town";

    // 每天的天数
    public const int DAYS_PER_SEASON = 28;
    public const int SEASONS_PER_YEAR = 4;

    // 常量
    public static readonly string[] SeasonNames = { "春", "夏", "秋", "冬" };
    public static readonly string[] WeatherNames = { "晴", "多云", "小雨", "大雨", "雷暴", "大风", "大雪" };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>设置玩家性别（开局选择）</summary>
    public void SetPlayerGender(PlayerGender gender)
    {
        playerGender = gender;
    }

    /// <summary>设置游戏状态并发布事件</summary>
    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        EventBus.Publish(GameEvent.GameStateChanged, newState);
    }

    /// <summary>暂停/恢复游戏</summary>
    public void PauseGame()
    {
        SetState(GameState.Paused);
        Time.timeScale = 0f;
        EventBus.Publish(GameEvent.GamePaused);
    }

    public void ResumeGame()
    {
        SetState(GameState.Playing);
        Time.timeScale = 1f;
        EventBus.Publish(GameEvent.GameResumed);
    }

    /// <summary>设置天气</summary>
    public void SetWeather(int index)
    {
        weatherIndex = Mathf.Clamp(index, 0, WeatherNames.Length - 1);
        weather = WeatherNames[weatherIndex];
        EventBus.Publish(GameEvent.WeatherChanged, weatherIndex);
    }

    /// <summary>增加金币</summary>
    public void AddGold(int amount)
    {
        gold += amount;
        EventBus.Publish(GameEvent.PlayerGoldChanged, gold);
    }

    public bool SpendGold(int amount)
    {
        if (gold < amount) return false;
        gold -= amount;
        EventBus.Publish(GameEvent.PlayerGoldChanged, gold);
        return true;
    }

    /// <summary>获取格式化的时间字符串</summary>
    public string GetTimeString()
    {
        return $"第{year}年 · {SeasonNames[season]} · 第{day}天 · {hour:D2}:{minute:D2}";
    }

    /// <summary>获取格式化的金币字符串</summary>
    public string GetGoldString()
    {
        return $"{gold} 文";
    }
}
