using UnityEngine;

/// <summary>
/// 料理食材
/// </summary>
[System.Serializable]
public struct RecipeIngredient
{
    public string itemId;
    public int count;
}

/// <summary>
/// 料理增益效果 — 对应 GDD 7.5
/// 攻击/防御/暴击为「百分比加成」（5 = +5%），HP/MP 上限为绝对值
/// </summary>
[System.Serializable]
public struct FoodBuff
{
    [Tooltip("攻击加成百分比（5 = +5%）")]
    public int attackBonus;
    [Tooltip("防御加成百分比（8 = +8%）")]
    public int defenseBonus;
    [Tooltip("暴击加成（0.05 = +5%）")]
    public float critBonus;
    [Tooltip("生命上限加成（绝对值）")]
    public int hpMaxBonus;
    [Tooltip("灵力上限加成（绝对值）")]
    public int mpMaxBonus;
    [Tooltip("持续时间（游戏分钟）")]
    public int durationMinutes;
}

/// <summary>
/// 食谱数据 — 对应 GDD 7.5
/// 来源：NPC赠送 / 电视节目 / 节日学习 / 食谱书
/// </summary>
[CreateAssetMenu(menuName = "Cooking/Recipe Data", fileName = "NewRecipe")]
public class RecipeData : ScriptableObject
{
    [Header("基本信息")]
    public string recipeId;
    public string recipeName;
    [TextArea(2, 4)]
    public string description;

    [Header("食材")]
    public RecipeIngredient[] ingredients;

    [Header("产物")]
    public string resultItemId;
    public int resultCount = 1;

    [Header("烹饪")]
    [Tooltip("烹饪耗时（游戏分钟）")]
    public int cookTime = 30;

    [Header("来源")]
    [Tooltip("解锁来源说明（NPC赠送/电视/节日/商店）")]
    public string unlockSource;

    [Header("增益")]
    public FoodBuff buff;
}
