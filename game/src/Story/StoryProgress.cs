/// <summary>
/// 主线幕 — 对应 GDD 2.2 三幕式主线
/// </summary>
public enum StoryAct
{
    Prologue = 0,  // 前置·觉醒之日
    Act1 = 1,      // 第一幕·村落启程（春 1-28）
    Act2 = 2,      // 第二幕·集结探索（夏→秋 29-84）
    Act3 = 3       // 第三幕·宿命对决（冬 85-112）
}

/// <summary>
/// 主线剧情节点 — 对应 GDD 2.2
/// </summary>
public enum StoryNode
{
    // 前置·觉醒之日
    Awakening,          // 灵流星坠落，祖父的信
    // 第一幕·村落启程
    Arrival,            // 1-1 初抵
    Settle,             // 1-2 安家
    FirstMeet,          // 1-3 初识（阿岚）
    Work,               // 1-4 打工
    WellDragonCall,     // 1-5 枯井龙鸣（翠翠孵化）
    FirstDragonBattle,  // 1-6 驯龙初战（石甲犀）
    SpringFestival,     // 1-7 春祭
    Act1Finale,         // 1-8 幕末·老村长的托付
    // 第二幕·集结探索（四圣物）
    WindStone,          // 2-1 风息石（火山·赤焰）
    FlameStone,         // 2-2 炎心石（沙漠·哈桑）
    TideStone,          // 2-3 潮涌石（海底·罗刹女）
    FrostStone,         // 2-4 霜魄石（雪山·白霜）
    // 第三幕·宿命对决
    DarknessArrives,    // 3-1 黑暗降临
    FinalRune,          // 3-2 最后的灵纹
    SkyIsland,          // 3-3 天空浮岛
    FinalBattle,        // 3-4 最终战
    Ending              // 3-5 结局分歧
}

/// <summary>
/// 四圣物 — 对应 GDD 2.2 第二幕
/// </summary>
public enum SacredRelic
{
    WindStone,    // 风息石（火山）
    FlameStone,   // 炎心石（沙漠）
    TideStone,    // 潮涌石（海底遗迹）
    FrostStone    // 霜魄石（雪山）
}

/// <summary>
/// 结局分歧 — 对应 GDD 2.2 3-5
/// </summary>
public enum StoryEnding
{
    PurifyAlone,       // 净化·单人结局
    Companion,         // 伴侣结局
    FamilyProsperity,  // 家族繁荣结局
    Hermit             // 归隐结局
}

/// <summary>
/// 主线工具类 — 名称/描述
/// </summary>
public static class StoryUtils
{
    /// <summary>幕中文名</summary>
    public static string GetActName(StoryAct act) => act switch
    {
        StoryAct.Prologue => "前置·觉醒之日",
        StoryAct.Act1     => "第一幕·村落启程",
        StoryAct.Act2     => "第二幕·集结探索",
        StoryAct.Act3     => "第三幕·宿命对决",
        _ => "?"
    };

    /// <summary>节点中文名</summary>
    public static string GetNodeName(StoryNode node) => node switch
    {
        StoryNode.Awakening         => "觉醒之日",
        StoryNode.Arrival           => "初抵",
        StoryNode.Settle            => "安家",
        StoryNode.FirstMeet         => "初识",
        StoryNode.Work              => "打工",
        StoryNode.WellDragonCall    => "枯井龙鸣",
        StoryNode.FirstDragonBattle => "驯龙初战",
        StoryNode.SpringFestival    => "春祭",
        StoryNode.Act1Finale        => "幕末托付",
        StoryNode.WindStone         => "风息石",
        StoryNode.FlameStone        => "炎心石",
        StoryNode.TideStone         => "潮涌石",
        StoryNode.FrostStone        => "霜魄石",
        StoryNode.DarknessArrives   => "黑暗降临",
        StoryNode.FinalRune         => "最后的灵纹",
        StoryNode.SkyIsland         => "天空浮岛",
        StoryNode.FinalBattle       => "最终战",
        StoryNode.Ending            => "结局",
        _ => "?"
    };

    /// <summary>圣物中文名</summary>
    public static string GetRelicName(SacredRelic relic) => relic switch
    {
        SacredRelic.WindStone  => "风息石",
        SacredRelic.FlameStone => "炎心石",
        SacredRelic.TideStone  => "潮涌石",
        SacredRelic.FrostStone => "霜魄石",
        _ => "?"
    };

    /// <summary>结局中文名</summary>
    public static string GetEndingName(StoryEnding ending) => ending switch
    {
        StoryEnding.PurifyAlone      => "净化·单人结局",
        StoryEnding.Companion        => "伴侣结局",
        StoryEnding.FamilyProsperity => "家族繁荣结局",
        StoryEnding.Hermit           => "归隐结局",
        _ => "?"
    };

    /// <summary>节点对应的幕</summary>
    public static StoryAct GetActForNode(StoryNode node)
    {
        switch (node)
        {
            case StoryNode.Awakening:
                return StoryAct.Prologue;
            case StoryNode.Arrival:
            case StoryNode.Settle:
            case StoryNode.FirstMeet:
            case StoryNode.Work:
            case StoryNode.WellDragonCall:
            case StoryNode.FirstDragonBattle:
            case StoryNode.SpringFestival:
            case StoryNode.Act1Finale:
                return StoryAct.Act1;
            case StoryNode.WindStone:
            case StoryNode.FlameStone:
            case StoryNode.TideStone:
            case StoryNode.FrostStone:
                return StoryAct.Act2;
            default:
                return StoryAct.Act3;
        }
    }
}
