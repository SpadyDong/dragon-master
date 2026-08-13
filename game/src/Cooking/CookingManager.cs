using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 烹饪管理器 — GDD 7.5
/// 管理食谱解锁、烹饪、料理增益应用
/// </summary>
public class CookingManager : MonoBehaviour
{
    public static CookingManager Instance { get; private set; }

    /// <summary>已解锁食谱ID集合</summary>
    private HashSet<string> _knownRecipes = new();

    /// <summary>当前激活的料理增益</summary>
    private FoodBuff _activeBuff;
    private float _buffRemainingMinutes;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ==================== 食谱解锁 ====================

    /// <summary>学习食谱</summary>
    public bool LearnRecipe(string recipeId)
    {
        if (string.IsNullOrEmpty(recipeId)) return false;

        var recipe = Resources.Load<RecipeData>($"Recipes/{recipeId}");
        if (recipe == null)
        {
            Debug.LogWarning($"CookingManager: 食谱 {recipeId} 不存在");
            return false;
        }

        if (_knownRecipes.Contains(recipeId)) return false;

        _knownRecipes.Add(recipeId);
        EventBus.Publish(GameEvent.RecipeLearned, recipeId);
        Debug.Log($"学会新食谱: {recipe.recipeName}");
        return true;
    }

    /// <summary>是否已掌握食谱</summary>
    public bool HasRecipe(string recipeId) => _knownRecipes.Contains(recipeId);

    /// <summary>获取所有已解锁食谱ID</summary>
    public List<string> GetAllKnownRecipes() => new(_knownRecipes);

    // ==================== 烹饪 ====================

    /// <summary>检查是否能烹饪（材料是否足够）</summary>
    public bool CanCook(string recipeId)
    {
        var recipe = Resources.Load<RecipeData>($"Recipes/{recipeId}");
        if (recipe == null) return false;

        foreach (var ingredient in recipe.ingredients)
        {
            if (InventoryManager.Instance == null) return false;
            if (!InventoryManager.Instance.HasItem(ingredient.itemId, ingredient.count))
                return false;
        }
        return true;
    }

    /// <summary>烹饪（消耗食材 + 产出成品 + 应用增益）</summary>
    public bool Cook(string recipeId)
    {
        var recipe = Resources.Load<RecipeData>($"Recipes/{recipeId}");
        if (recipe == null) return false;
        if (!CanCook(recipeId)) return false;

        // 消耗食材
        foreach (var ingredient in recipe.ingredients)
        {
            InventoryManager.Instance.RemoveItem(ingredient.itemId, ingredient.count);
        }

        // 产出成品
        if (!string.IsNullOrEmpty(recipe.resultItemId))
            InventoryManager.Instance.AddItem(recipe.resultItemId, recipe.resultCount);

        // 应用增益
        ApplyBuff(recipe.buff);

        // 社交熟练度 XP（烹饪属于社交类，GDD 未单列）
        if (ProficiencyManager.Instance != null)
            ProficiencyManager.Instance.AddXP(ProficiencySkill.Social, 5);

        EventBus.Publish(GameEvent.DishCooked, recipeId);
        Debug.Log($"烹饪完成: {recipe.recipeName}");
        return true;
    }

    // ==================== 增益 ====================

    /// <summary>应用料理增益（持续 buffDuration 分钟）</summary>
    public void ApplyBuff(FoodBuff buff)
    {
        if (buff.durationMinutes <= 0) return;
        _activeBuff = buff;
        _buffRemainingMinutes = buff.durationMinutes;
    }

    /// <summary>获取当前激活增益</summary>
    public FoodBuff GetActiveBuff() => _activeBuff;

    /// <summary>增益是否生效</summary>
    public bool HasActiveBuff() => _buffRemainingMinutes > 0;

    /// <summary>获取当前攻击加成百分比（0.05 = +5%，供战斗系统消费）</summary>
    public float GetAttackBuffPercent() => HasActiveBuff() ? _activeBuff.attackBonus / 100f : 0f;

    /// <summary>获取当前防御加成百分比（供战斗系统消费）</summary>
    public float GetDefenseBuffPercent() => HasActiveBuff() ? _activeBuff.defenseBonus / 100f : 0f;

    /// <summary>获取当前暴击加成（供战斗系统消费）</summary>
    public float GetCritBuffPercent() => HasActiveBuff() ? _activeBuff.critBonus : 0f;

    /// <summary>获取当前生命上限加成绝对值</summary>
    public int GetHpMaxBuff() => HasActiveBuff() ? _activeBuff.hpMaxBonus : 0;

    /// <summary>获取当前灵力上限加成绝对值</summary>
    public int GetMpMaxBuff() => HasActiveBuff() ? _activeBuff.mpMaxBonus : 0;

    /// <summary>每小时推进（由 TimeManager 或 HourChanged 驱动）</summary>
    void OnHourChanged(int hour)
    {
        if (_buffRemainingMinutes > 0)
        {
            _buffRemainingMinutes -= 60;
            if (_buffRemainingMinutes <= 0)
            {
                _activeBuff = default;
                Debug.Log("料理增益已失效");
            }
        }
    }

    void Start()
    {
        EventBus.Subscribe<int>(GameEvent.HourChanged, OnHourChanged);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.HourChanged, OnHourChanged);
    }

    // ==================== 存档 ====================

    [System.Serializable]
    public class CookingSaveData
    {
        public List<string> knownRecipes;
    }

    public CookingSaveData GetSaveData()
    {
        return new CookingSaveData { knownRecipes = new List<string>(_knownRecipes) };
    }

    public void LoadSaveData(CookingSaveData data)
    {
        if (data?.knownRecipes == null) return;
        _knownRecipes = new HashSet<string>(data.knownRecipes);
    }
}
