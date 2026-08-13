using System.Collections.Generic;

/// <summary>
/// 主线任务数据工厂 — GDD 2.2
/// 生成主线 QuestData（QuestType.Story），复用 QuestManager 的解锁/完成逻辑
/// </summary>
public static class StoryQuests
{
    /// <summary>构建全部主线任务</summary>
    public static List<QuestData> BuildAll()
    {
        var list = new List<QuestData>();

        // ==================== 前置·觉醒之日 ====================
        list.Add(Story("story_awakening", "觉醒之日", "灵流星坠入苍莽大陆，一封泛黄的信揭开了主角的身世之谜。",
            requiredDay: 1,
            objectives: new[] { Obj(QuestObjectiveType.TalkTo, "laocunzhang", 1, "拜访老村长") }));

        // ==================== 第一幕·村落启程（春 1-28） ====================
        list.Add(Story("story_arrival", "初抵", "乘小舟沿苍莽河而下，在东港口登岸。",
            requiredDay: 1, prerequisite: new[] { "story_awakening" },
            objectives: new[] { Obj(QuestObjectiveType.TalkTo, "mafu_laoli", 1, "与马夫老李交谈") }));

        list.Add(Story("story_settle", "安家", "清理山脚下被藤蔓覆盖的木屋，修葺屋顶。",
            requiredDay: 1, prerequisite: new[] { "story_arrival" },
            objectives: new[] { Obj(QuestObjectiveType.ReachPlace, "home", 1, "到达主角木屋") }));

        list.Add(Story("story_first_meet", "初识", "阿岚主动上门，赠送风灵石碎片。",
            requiredDay: 2, prerequisite: new[] { "story_settle" },
            objectives: new[] { Obj(QuestObjectiveType.TalkTo, "alan", 1, "与阿岚交谈") }));

        list.Add(Story("story_work", "打工", "去武器店打工，赚取报酬。",
            requiredDay: 3, prerequisite: new[] { "story_first_meet" },
            objectives: new[] { Obj(QuestObjectiveType.TalkTo, "alan", 1, "在武器店打工") }));

        list.Add(Story("story_well_dragon", "枯井龙鸣", "夜晚去屋后枯井，孵化祖父留下的幼龙蛋。",
            requiredDay: 5, prerequisite: new[] { "story_work" },
            objectives: new[] { Obj(QuestObjectiveType.ReachPlace, "well", 1, "夜晚前往枯井") }));

        list.Add(Story("story_first_battle", "驯龙初战", "带翠翠到广场，迎战失控的荒兽·石甲犀。",
            requiredDay: 7, prerequisite: new[] { "story_well_dragon" },
            objectives: new[] { Obj(QuestObjectiveType.Kill, "stone_rhino", 1, "击败石甲犀") }));

        list.Add(Story("story_spring_festival", "春祭", "春耕祭：全镇摆宴庆祝。",
            requiredDay: 14, prerequisite: new[] { "story_first_battle" },
            objectives: new[] { Obj(QuestObjectiveType.TalkTo, "laocunzhang", 1, "参加春祭") }));

        list.Add(Story("story_act1_finale", "幕末托付", "老村长将祖父的驯龙笔记交予主角。",
            requiredDay: 24, prerequisite: new[] { "story_spring_festival" },
            objectives: new[] { Obj(QuestObjectiveType.TalkTo, "laocunzhang", 1, "找老村长（好感≥100）") }));

        // ==================== 第二幕·集结探索（夏→秋 29-84） ====================
        list.Add(Story("story_wind_stone", "风息石", "前往火山，通过赤焰的试炼，击败炎心龙。",
            requiredDay: 29, requiredSeason: 1, prerequisite: new[] { "story_act1_finale" },
            objectives: new[] { Obj(QuestObjectiveType.Kill, "flame_heart_dragon", 1, "击败炎心龙") }));

        list.Add(Story("story_flame_stone", "炎心石", "前往沙漠，追踪沙虫王取回炎心石。",
            requiredDay: 29, requiredSeason: 1, prerequisite: new[] { "story_wind_stone" },
            objectives: new[] { Obj(QuestObjectiveType.Kill, "sand_worm_king", 1, "击败沙虫王") }));

        list.Add(Story("story_tide_stone", "潮涌石", "与云涛出海，在海底遗迹击败深渊守护者。",
            requiredDay: 57, requiredSeason: 2, prerequisite: new[] { "story_flame_stone" },
            objectives: new[] { Obj(QuestObjectiveType.Kill, "abyss_guardian", 1, "击败深渊守护者") }));

        list.Add(Story("story_frost_stone", "霜魄石", "连续7天上雪山为白霜送热茶，获得霜魄石。",
            requiredDay: 57, requiredSeason: 2, prerequisite: new[] { "story_tide_stone" },
            objectives: new[] { Obj(QuestObjectiveType.TalkTo, "baishuang", 7, "连续7天拜访白霜") }));

        // ==================== 第三幕·宿命对决（冬 85-112） ====================
        list.Add(Story("story_darkness", "黑暗降临", "天空暗红，黑暗龙灵王的先遣军来袭。",
            requiredDay: 85, requiredSeason: 3, prerequisite: new[] { "story_frost_stone" },
            objectives: new[] { Obj(QuestObjectiveType.TalkTo, "laocunzhang", 1, "与老村长汇合") }));

        list.Add(Story("story_final_rune", "最后的灵纹", "解读笔记最后一页，选出同行者。",
            requiredDay: 85, requiredSeason: 3, prerequisite: new[] { "story_darkness" },
            objectives: new[] { Obj(QuestObjectiveType.TalkTo, "laocunzhang", 1, "选出三位同行者") }));

        list.Add(Story("story_sky_island", "天空浮岛", "敲钟12响，登上天空浮岛，穿越四大领域。",
            requiredDay: 85, requiredSeason: 3, prerequisite: new[] { "story_final_rune" },
            objectives: new[] { Obj(QuestObjectiveType.ReachPlace, "sky_island", 1, "抵达天空浮岛") }));

        list.Add(Story("story_final_battle", "最终战", "在王座前迎战黑暗龙灵王。",
            requiredDay: 85, requiredSeason: 3, prerequisite: new[] { "story_sky_island" },
            objectives: new[] { Obj(QuestObjectiveType.Kill, "dark_dragon_spirit_king", 1, "击败黑暗龙灵王") }));

        return list;
    }

    // ==================== 辅助 ====================

    static QuestData Story(string id, string name, string desc, int requiredDay, string[] prerequisite = null, int requiredSeason = -1, QuestObjective[] objectives = null)
    {
        return new QuestData
        {
            questId = id,
            questName = name,
            npcId = "laocunzhang",
            questType = QuestType.Story,
            description = desc,
            requiredDay = requiredDay,
            requiredSeason = requiredSeason,
            prerequisiteQuests = prerequisite,
            objectives = objectives,
            rewardAffection = 10
        };
    }

    static QuestObjective Obj(QuestObjectiveType type, string targetId, int count, string desc)
    {
        return new QuestObjective
        {
            type = type,
            targetId = targetId,
            requiredCount = count,
            description = desc
        };
    }
}
