using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 天气系统
/// 管理 7 种天气 + 对种植/钓鱼/龙/畜牧/NPC/战斗的系统级影响
/// 每天跨天时根据季节概率随机切换天气
/// </summary>
public class WeatherSystem : MonoBehaviour
{
    public static WeatherSystem Instance { get; private set; }

    [System.Serializable]
    public struct WeatherConfig
    {
        public string name;
        public int index;               // 天气索引，对应 GameManager.WeatherNames
        public float baseProbability;   // 基准概率
        public int[] validSeasons;       // 有效季节（-1=全季）
        public float cropGrowthModifier;// 作物生长速度修正（1.0=基准）
        public bool needsWatering;      // 是否需要人工浇水
        public float fishingBonus;      // 钓鱼概率修正
        public float dragonMoodModifier; // 龙心情修正
        public int livestockYieldBonus; // 畜牧产出修正
        public float npcSpeedModifier;   // NPC移动速度修正
        public float combatEvasionMod;  // 战斗闪避修正
    }

    [Header("天气配置")]
    [SerializeField] private WeatherConfig[] weatherConfigs;

    // 当前生效的修正值（供各系统查询）
    public float CropGrowthMod { get; private set; } = 1f;
    public bool NeedsWatering { get; private set; } = true;
    public float FishingBonus { get; private set; } = 1f;
    public float DragonMoodMod { get; private set; } = 1f;
    public int LivestockYieldBonus { get; private set; } = 0;
    public float NPCSpeedMod { get; private set; } = 1f;

    void Awake()
    {
        Instance = this;

        if (weatherConfigs == null || weatherConfigs.Length == 0)
            BuildDefaultConfigs();
    }

    void Start()
    {
        EventBus.Subscribe<int>(GameEvent.DayChanged, OnDayChanged);
        // 初始化第一天的天气修正
        ApplyWeather(GameManager.Instance.weatherIndex);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.DayChanged, OnDayChanged);
    }

    private void BuildDefaultConfigs()
    {
        weatherConfigs = new WeatherConfig[]
        {
            new() { name="晴", index=0, baseProbability=0.60f, validSeasons=new[]{-1},
                cropGrowthModifier=1.0f, needsWatering=true, fishingBonus=1.0f, dragonMoodMod=1.0f, livestockYieldBonus=0, npcSpeedModifier=1.0f, combatEvasionMod=1.0f },
            new() { name="多云", index=1, baseProbability=0.15f, validSeasons=new[]{-1},
                cropGrowthModifier=1.0f, needsWatering=true, fishingBonus=1.05f, dragonMoodMod=1.0f, livestockYieldBonus=0, npcSpeedModifier=1.0f, combatEvasionMod=1.0f },
            new() { name="小雨", index=2, baseProbability=0.10f, validSeasons=new[]{0,1,2},
                cropGrowthModifier=1.15f, needsWatering=false, fishingBonus=1.1f, dragonMoodMod=0.95f, livestockYieldBonus=0, npcSpeedModifier=0.9f, combatEvasionMod=0.95f },
            new() { name="大雨", index=3, baseProbability=0.05f, validSeasons=new[]{0,1,2},
                cropGrowthModifier=1.2f, needsWatering=false, fishingBonus=0.8f, dragonMoodMod=0.9f, livestockYieldBonus=-1, npcSpeedModifier=0.8f, combatEvasionMod=0.9f },
            new() { name="雷暴", index=4, baseProbability=0.03f, validSeasons=new[]{1},
                cropGrowthModifier=1.2f, needsWatering=false, fishingBonus=0.5f, dragonMoodMod=0.7f, livestockYieldBonus=-2, npcSpeedModifier=0.6f, combatEvasionMod=0.85f },
            new() { name="大风", index=5, baseProbability=0.08f, validSeasons=new[]{0,2},
                cropGrowthModifier=0.9f, needsWatering=true, fishingBonus=0.9f, dragonMoodMod=1.05f, livestockYieldBonus=0, npcSpeedModifier=1.1f, combatEvasionMod=1.0f },
            new() { name="大雪", index=6, baseProbability=0.10f, validSeasons=new[]{3},
                cropGrowthModifier=0.5f, needsWatering=false, fishingBonus=0.3f, dragonMoodMod=0.8f, livestockYieldBonus=-1, npcSpeedModifier=0.7f, combatEvasionMod=0.95f },
        };
    }

    private void OnDayChanged(int day)
    {
        // 每日随机天气
        int newWeather = RollWeather();
        GameManager.Instance.SetWeather(newWeather);
        ApplyWeather(newWeather);
    }

    private int RollWeather()
    {
        int season = GameManager.Instance.season;
        int prevWeather = GameManager.Instance.weatherIndex;

        // 构建当天有效天气列表（按季节过滤 + 排除概率过低的）
        var validWeathers = new List<WeatherConfig>();
        foreach (var w in weatherConfigs)
        {
            bool seasonValid = false;
            foreach (int s in w.validSeasons)
            {
                if (s == -1 || s == season) { seasonValid = true; break; }
            }
            if (seasonValid) validWeathers.Add(w);
        }

        // 加权随机
        float total = 0f;
        foreach (var w in validWeathers) total += w.baseProbability;
        float roll = Random.Range(0f, total);

        float cumulative = 0f;
        foreach (var w in validWeathers)
        {
            cumulative += w.baseProbability;
            if (roll <= cumulative) return w.index;
        }

        return 0; // 默认晴
    }

    private void ApplyWeather(int weatherIndex)
    {
        foreach (var w in weatherConfigs)
        {
            if (w.index == weatherIndex)
            {
                CropGrowthMod = w.cropGrowthModifier;
                NeedsWatering = w.needsWatering;
                FishingBonus = w.fishingBonus;
                DragonMoodMod = w.dragonMoodModifier;
                LivestockYieldBonus = w.livestockYieldBonus;
                NPCSpeedMod = w.npcSpeedModifier;
                break;
            }
        }
    }
}
