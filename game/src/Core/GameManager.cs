using UnityEngine;

/// <summary>
/// 游戏全局状态管理器（单例）
/// 管理时间、天气、金币等核心数据
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

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

    // 季节名映射
    public static readonly string[] SeasonNames = { "春", "夏", "秋", "冬" };

    // 天气名列表
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

    /// <summary>
    /// 获取格式化的时间字符串
    /// </summary>
    public string GetTimeString()
    {
        return $"第{year}年 · {SeasonNames[season]} · 第{day}天 · {hour:D2}:{minute:D2}";
    }

    /// <summary>
    /// 获取格式化的金币字符串
    /// </summary>
    public string GetGoldString()
    {
        return $"{gold} 文";
    }
}
