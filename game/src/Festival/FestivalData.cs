using UnityEngine;

/// <summary>
/// 节日数据 — 对应 GDD 7.2（每年 9 个节日）
/// </summary>
[CreateAssetMenu(menuName = "Festival/Festival Data", fileName = "NewFestival")]
public class FestivalData : ScriptableObject
{
    [Header("基本信息")]
    public string festivalId;
    public string festivalName;
    [TextArea(2, 4)]
    public string description;

    [Header("时间")]
    [Tooltip("季节：0=春 1=夏 2=秋 3=冬")]
    public int season;
    [Tooltip("日期（1-28）")]
    public int day;

    [Header("内容")]
    [TextArea(1, 2)]
    public string activities;   // 节日活动描述

    [Header("奖励")]
    public int rewardGold;
    public string rewardItemId;
    [Tooltip("全体 NPC 好感加成")]
    public int rewardAffection = 10;

    /// <summary>是否是当日节日</summary>
    public bool IsToday(int currentSeason, int currentDay)
    {
        return season == currentSeason && day == currentDay;
    }
}
