using UnityEngine;

/// <summary>
/// 时间推进引擎
/// 驱动游戏内分钟→小时→天→季节→年的流转
/// </summary>
public class TimeManager : MonoBehaviour
{
    [Header("时间流速")]
    [Tooltip("1 游戏分钟对应的现实秒数")]
    [SerializeField] private float realSecondsPerGameMinute = 0.7f;

    [Header("跳转")]
    [Tooltip("跳过时间时是否触发展示过天界面")]
    [SerializeField] private bool showDayTransition = true;

    private float _accumulator;
    private bool _isInitialized;

    void Start()
    {
        _isInitialized = true;
    }

    void Update()
    {
        if (!_isInitialized) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        _accumulator += Time.deltaTime;

        while (_accumulator >= realSecondsPerGameMinute)
        {
            _accumulator -= realSecondsPerGameMinute;
            AdvanceByTenMinutes();
        }
    }

    /// <summary>每 10 游戏分钟一跳</summary>
    private void AdvanceByTenMinutes()
    {
        GameManager gm = GameManager.Instance;

        gm.minute += 10;

        // 深夜惩罚（00:00-06:00）
        if (gm.hour >= 0 && gm.hour < 6)
        {
            EventBus.Publish(GameEvent.MinuteChanged, gm.minute);
        }

        if (gm.minute >= 60)
        {
            gm.minute = 0;
            AdvanceHour(gm);
        }
    }

    private void AdvanceHour(GameManager gm)
    {
        gm.hour++;
        EventBus.Publish(GameEvent.HourChanged, gm.hour);

        if (gm.hour >= 24)
        {
            gm.hour = 0;
            AdvanceDay(gm);
        }
    }

    private void AdvanceDay(GameManager gm)
    {
        gm.day++;
        EventBus.Publish(GameEvent.DayChanged, gm.day);

        if (gm.day > GameManager.DAYS_PER_SEASON)
        {
            gm.day = 1;

            // 随机天气变化（跨天时可能有天气变化）
            if (Random.value < 0.3f) // 30% 概率天气变化
            {
                int newWeather = Random.Range(0, GameManager.WeatherNames.Length);
                gm.SetWeather(newWeather);
            }

            AdvanceSeason(gm);
        }
    }

    private void AdvanceSeason(GameManager gm)
    {
        gm.season++;
        EventBus.Publish(GameEvent.SeasonChanged, gm.season);

        if (gm.season >= GameManager.SEASONS_PER_YEAR)
        {
            gm.season = 0;
            AdvanceYear(gm);
        }
    }

    private void AdvanceYear(GameManager gm)
    {
        gm.year++;
        EventBus.Publish(GameEvent.YearChanged, gm.year);
    }

    /// <summary>跳到指定时间（睡觉/剧情快进）</summary>
    public void SkipToTime(int targetHour, int targetMinute)
    {
        GameManager gm = GameManager.Instance;
        // 如果目标时间在当前时间之前，说明跨天了
        if (targetHour < gm.hour || (targetHour == gm.hour && targetMinute < gm.minute))
        {
            gm.hour = targetHour;
            gm.minute = targetMinute;
            AdvanceDay(gm);
        }
        else
        {
            gm.hour = targetHour;
            gm.minute = targetMinute;
        }
        EventBus.Publish(GameEvent.HourChanged, gm.hour);
    }

    /// <summary>睡到第二天指定时间</summary>
    public void SkipToNextDay(int wakeHour = 6)
    {
        GameManager gm = GameManager.Instance;
        AdvanceDay(gm);
        gm.hour = wakeHour;
        gm.minute = 0;
        EventBus.Publish(GameEvent.HourChanged, gm.hour);
    }

    /// <summary>获取当前时段描述</summary>
    public string GetTimeOfDay()
    {
        int h = GameManager.Instance.hour;
        if (h >= 6 && h < 8) return "清晨";
        if (h >= 8 && h < 12) return "上午";
        if (h >= 12 && h < 14) return "中午";
        if (h >= 14 && h < 18) return "下午";
        if (h >= 18 && h < 20) return "傍晚";
        if (h >= 20 && h < 24) return "夜晚";
        return "深夜";
    }
}
