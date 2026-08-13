/// <summary>
/// 结局判定器 — 对应 GDD 2.2 3-5
/// 根据净化/消灭 + 婚姻状态 + 同行者状态判定四种结局
/// </summary>
public static class StoryEndingResolver
{
    /// <summary>
    /// 判定结局
    /// </summary>
    /// <param name="purify">是否选择净化龙灵王（false=消灭）</param>
    /// <param name="isMarried">是否已结婚</param>
    /// <param name="hasCompanion">是否选定了同行者</param>
    public static StoryEnding Resolve(bool purify, bool isMarried, bool hasCompanion)
    {
        if (purify)
        {
            // 净化结局分支
            if (isMarried)
                return StoryEnding.Companion;       // 伴侣结局
            if (hasCompanion)
                return StoryEnding.Hermit;          // 归隐结局（带最爱龙+同伴离开）
            return StoryEnding.PurifyAlone;         // 单人结局
        }
        else
        {
            // 消灭结局分支
            // 家族繁荣：主角成为新一代驯龙宗师，龙脊镇扩建（视为默认消灭结局）
            return StoryEnding.FamilyProsperity;
        }
    }

    /// <summary>结局描述</summary>
    public static string GetEndingDescription(StoryEnding ending) => ending switch
    {
        StoryEnding.PurifyAlone      => "龙灵王被净化，主角独自守护龙脊镇，天空浮岛降为新的和平区域。",
        StoryEnding.Companion        => "与结婚对象一起生活，婚后日常的温馨图景。",
        StoryEnding.FamilyProsperity => "孩子出生，主角成为新一代驯龙宗师，龙脊镇扩建为龙脊城。",
        StoryEnding.Hermit           => "带上最爱的龙，和伴侣一起离开龙脊镇，去探索苍莽大陆的未知之地。",
        _ => ""
    };
}
