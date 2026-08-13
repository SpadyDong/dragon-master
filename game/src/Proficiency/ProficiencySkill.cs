/// <summary>
/// 熟练度大类 — 对应 GDD 7.1
/// 六大熟练度统一成长框架 Lv1-100
/// </summary>
public enum ProficiencySkill
{
    Farming,     // 种植
    Husbandry,   // 畜牧
    Fishing,     // 钓鱼
    Combat,      // 战斗
    Gathering,   // 采集
    Social       // 社交
}

/// <summary>
/// 专精分支选项
/// </summary>
public enum ProficiencyBranch
{
    None,
    BranchA,   // A 分支
    BranchB    // B 分支
}

/// <summary>
/// 熟练度工具类 — 名称/描述/通用收益说明
/// </summary>
public static class ProficiencyUtils
{
    /// <summary>熟练度中文名</summary>
    public static string GetSkillName(ProficiencySkill skill) => skill switch
    {
        ProficiencySkill.Farming   => "种植",
        ProficiencySkill.Husbandry => "畜牧",
        ProficiencySkill.Fishing   => "钓鱼",
        ProficiencySkill.Combat    => "战斗",
        ProficiencySkill.Gathering => "采集",
        ProficiencySkill.Social    => "社交",
        _ => "?"
    };

    /// <summary>熟练度图标（emoji 占位）</summary>
    public static string GetSkillIcon(ProficiencySkill skill) => skill switch
    {
        ProficiencySkill.Farming   => "🌾",
        ProficiencySkill.Husbandry => "🐑",
        ProficiencySkill.Fishing   => "🎣",
        ProficiencySkill.Combat    => "⚔️",
        ProficiencySkill.Gathering => "⛏️",
        ProficiencySkill.Social    => "❤️",
        _ => "?"
    };

    /// <summary>通用收益描述（每 Lv1）</summary>
    public static string GetPerLevelDescription(ProficiencySkill skill) => skill switch
    {
        ProficiencySkill.Farming   => "作物生长速度 +0.5%/级；品质提升概率 +0.3%/级",
        ProficiencySkill.Husbandry => "家畜心情上限 +0.5/级；龙产蛋后恢复速度 +0.8%/级",
        ProficiencySkill.Fishing   => "钓竿绿色稳定条 +0.5%/级；稀有鱼出现概率 +0.2%/级",
        ProficiencySkill.Combat    => "基础攻击 +0.3/级；暴击率 +0.05%/级",
        ProficiencySkill.Gathering => "采集暴击掉落概率 +0.3%/级；体力消耗 -0.3%/级",
        ProficiencySkill.Social    => "礼物效果 +0.5%/级；NPC心事件触发率 +0.2%/级",
        _ => ""
    };

    /// <summary>Lv10 专精分支名称（二选一）</summary>
    public static string GetBranchName(ProficiencySkill skill, ProficiencyBranch branch, int tier)
    {
        // tier: 1 = Lv10, 2 = Lv30
        if (branch == ProficiencyBranch.None) return "未选择";
        return (skill, tier, branch) switch
        {
            (ProficiencySkill.Farming, 1, ProficiencyBranch.BranchA)   => "精耕：作物+12%生长",
            (ProficiencySkill.Farming, 1, ProficiencyBranch.BranchB)   => "广种：耕地数+20%容量",
            (ProficiencySkill.Farming, 2, ProficiencyBranch.BranchA)   => "育种：巨化概率×2",
            (ProficiencySkill.Farming, 2, ProficiencyBranch.BranchB)   => "温室：反季节品质×1.3",
            (ProficiencySkill.Husbandry, 1, ProficiencyBranch.BranchA) => "繁育：家畜发情+30%",
            (ProficiencySkill.Husbandry, 1, ProficiencyBranch.BranchB) => "牧守：家畜心情+20/日",
            (ProficiencySkill.Husbandry, 2, ProficiencyBranch.BranchA) => "驯龙师：龙蛋孵化-2日",
            (ProficiencySkill.Husbandry, 2, ProficiencyBranch.BranchB) => "屠夫：肉类品质×1.3",
            (ProficiencySkill.Fishing, 1, ProficiencyBranch.BranchA)   => "稳钓：QTE条扩大20%",
            (ProficiencySkill.Fishing, 1, ProficiencyBranch.BranchB)   => "灵钓：稀有鱼+15%",
            (ProficiencySkill.Fishing, 2, ProficiencyBranch.BranchA)   => "传说猎人：传说鱼出现×1.5",
            (ProficiencySkill.Fishing, 2, ProficiencyBranch.BranchB)   => "鱼塘大亨：鱼塘容量×2",
            (ProficiencySkill.Combat, 1, ProficiencyBranch.BranchA)    => "战士：攻击+15%",
            (ProficiencySkill.Combat, 1, ProficiencyBranch.BranchB)    => "哨兵：暴击+10%",
            (ProficiencySkill.Combat, 2, ProficiencyBranch.BranchA)    => "狂战：暴击伤害×1.5",
            (ProficiencySkill.Combat, 2, ProficiencyBranch.BranchB)    => "铁壁：防御+20%",
            (ProficiencySkill.Gathering, 1, ProficiencyBranch.BranchA) => "矿工：矿石+1块/击",
            (ProficiencySkill.Gathering, 1, ProficiencyBranch.BranchB) => "勘测：宝石概率×2",
            (ProficiencySkill.Gathering, 2, ProficiencyBranch.BranchA) => "伐木工：树液+蜂箱产量×1.5",
            (ProficiencySkill.Gathering, 2, ProficiencyBranch.BranchB) => "追踪者：觅食自动标记稀有",
            (ProficiencySkill.Social, 1, ProficiencyBranch.BranchA)    => "人气：日常礼物×1.2效",
            (ProficiencySkill.Social, 1, ProficiencyBranch.BranchB)    => "亲和：配偶加成+10%",
            (ProficiencySkill.Social, 2, ProficiencyBranch.BranchA)    => "交际花：圆桌宴随机必定出席好友≥2",
            (ProficiencySkill.Social, 2, ProficiencyBranch.BranchB)    => "父母心：生育/养子属性+15%",
            _ => "?"
        };
    }

    /// <summary>Lv50 王牌称号</summary>
    public static string GetTitle(ProficiencySkill skill) => skill switch
    {
        ProficiencySkill.Farming   => "神农",
        ProficiencySkill.Husbandry => "龙牧圣手",
        ProficiencySkill.Fishing   => "太公钓",
        ProficiencySkill.Combat    => "战龙尊者",
        ProficiencySkill.Gathering => "山海行者",
        ProficiencySkill.Social    => "万人迷",
        _ => "?"
    };
}
