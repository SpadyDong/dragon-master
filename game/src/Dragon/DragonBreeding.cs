using UnityEngine;

/// <summary>
/// 繁育结果
/// </summary>
public enum BreedResultType
{
    Success,
    WrongSeason,
    MotherNotInDen,
    EggLimitReached,
    NotAdult,
    SameGender,
    NoPartnerData
}

/// <summary>
/// 龙繁育系统 — 简化版
/// 同种龙属性固定，无 IV 遗传
/// 硬规则：每雌性一生最多 2 颗蛋，需春秋季 + 独立龙舍
/// </summary>
public static class DragonBreeding
{
    public static BreedResultType TryBreed(
        DragonController mother, DragonController father, int currentSeason)
    {
        if (!mother.IsFemale || father.IsFemale) return BreedResultType.SameGender;
        if (mother.Stage != DragonStage.Adult || father.Stage != DragonStage.Adult) return BreedResultType.NotAdult;
        if (mother.Data == null || father.Data == null) return BreedResultType.NoPartnerData;
        if (currentSeason != 0 && currentSeason != 2) return BreedResultType.WrongSeason;
        if (!mother.IsInDen) return BreedResultType.MotherNotInDen;
        if (mother.EggsLaid >= 2) return BreedResultType.EggLimitReached;
        return BreedResultType.Success;
    }

    /// <summary>继承元素 — 同元素→传承；不同元素→主元素跟母方</summary>
    public static (DragonElement primary, DragonElement? secondary) InheritElement(
        DragonController mother, DragonController father)
    {
        DragonElement motherEl = mother.PrimaryElement;
        DragonElement fatherEl = father.PrimaryElement;

        if (motherEl == fatherEl)
        {
            DragonElement? secondary = null;
            if (mother.SecondaryElement.HasValue || father.SecondaryElement.HasValue)
            {
                var parentSec = mother.SecondaryElement ?? father.SecondaryElement;
                if (parentSec.HasValue && Random.value < 0.10f) secondary = parentSec;
            }
            return (motherEl, secondary);
        }

        DragonElement? sec = Random.value < 0.10f ? fatherEl : null;
        return (motherEl, sec);
    }

    /// <summary>执行繁育</summary>
    public static bool ExecuteBreed(DragonController mother, DragonController father, int currentSeason)
    {
        if (TryBreed(mother, father, currentSeason) != BreedResultType.Success)
        {
            Debug.LogWarning("繁育失败");
            return false;
        }

        var element = InheritElement(mother, father);
        DragonData childSpecies = mother.Data;
        DragonGender childGender = Random.value < 0.5f ? DragonGender.Male : DragonGender.Female;

        DragonManager.Instance?.CreateDragonEgg(
            childSpecies, childGender, element.primary, element.secondary,
            mother.transform.position);

        mother.OnEggLaid();
        return true;
    }

    public static string GetResultDescription(BreedResultType r) => r switch
    {
        BreedResultType.Success         => "繁育成功，母龙产下一颗龙蛋！",
        BreedResultType.WrongSeason     => "非交配季，仅春季和秋季可繁育。",
        BreedResultType.MotherNotInDen  => "母龙需要在独立龙舍中才能繁育。",
        BreedResultType.EggLimitReached => "母龙已达到产蛋上限（2颗）。",
        BreedResultType.NotAdult        => "双方都必须是成年龙。",
        BreedResultType.SameGender      => "需要异性龙才能繁育。",
        BreedResultType.NoPartnerData   => "缺少龙种数据。",
        _ => "未知错误。"
    };
}
