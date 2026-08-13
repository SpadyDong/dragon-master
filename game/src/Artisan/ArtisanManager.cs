using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 工匠加工任务
/// </summary>
[System.Serializable]
public class ArtisanJob
{
    public string jobId;
    public string deviceId;
    public string recipeId;
    public string inputItemId;       // 主要原料（用于定价）
    public string outputItemId;
    public int outputCount;
    public int startDay;
    public int processDays;
    public MarketQuality inputQuality; // 原料品质（影响定价）

    /// <summary>是否已完成（当前天数 >= startDay + processDays）</summary>
    public bool IsFinished(int currentDay) => currentDay >= startDay + processDays;
}

/// <summary>
/// 工匠设备管理器 — GDD 5.2.4
/// 8 种工匠设备的加工任务管理、成品收取、定价联动菜市场
/// </summary>
public class ArtisanManager : MonoBehaviour
{
    public static ArtisanManager Instance { get; private set; }

    /// <summary>进行中的加工任务</summary>
    private List<ArtisanJob> _jobs = new();

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

    void Start()
    {
        EventBus.Subscribe<int>(GameEvent.DayChanged, OnDayChanged);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.DayChanged, OnDayChanged);
    }

    void OnDayChanged(int day)
    {
        // 检查是否有新完成的加工任务
        var finished = GetFinishedJobs();
        foreach (var job in finished)
        {
            EventBus.Publish(GameEvent.ArtisanJobCompleted, job.outputItemId);
            Debug.Log($"工匠加工完成: {job.outputItemId}");
        }
    }

    // ==================== 加工 ====================

    /// <summary>开始加工（检查原料 + 消耗 + 创建任务）</summary>
    public bool StartProcess(ArtisanDeviceData device, int recipeIndex, MarketQuality inputQuality = MarketQuality.Normal)
    {
        if (device == null || device.recipes == null) return false;
        if (recipeIndex < 0 || recipeIndex >= device.recipes.Length) return false;

        var recipe = device.recipes[recipeIndex];

        // 季节检查
        int currentSeason = GameManager.Instance != null ? GameManager.Instance.season : 0;
        if (recipe.requiredSeason >= 0 && recipe.requiredSeason != currentSeason)
            return false;

        // 检查主要原料
        if (InventoryManager.Instance == null) return false;
        if (!InventoryManager.Instance.HasItem(recipe.inputItemId, recipe.inputCount)) return false;

        // 检查额外材料
        if (recipe.extraMaterials != null)
        {
            foreach (var mat in recipe.extraMaterials)
            {
                if (!InventoryManager.Instance.HasItem(mat.itemId, mat.count)) return false;
            }
        }

        // 消耗原料
        InventoryManager.Instance.RemoveItem(recipe.inputItemId, recipe.inputCount);
        if (recipe.extraMaterials != null)
        {
            foreach (var mat in recipe.extraMaterials)
                InventoryManager.Instance.RemoveItem(mat.itemId, mat.count);
        }

        // 创建加工任务
        int currentDay = GameManager.Instance != null ? GameManager.Instance.day : 1;
        var job = new ArtisanJob
        {
            jobId = System.Guid.NewGuid().ToString("N").Substring(0, 8),
            deviceId = device.deviceId,
            recipeId = recipe.recipeId,
            inputItemId = recipe.inputItemId,
            outputItemId = recipe.outputItemId,
            outputCount = recipe.outputCount,
            startDay = currentDay,
            processDays = recipe.processDays,
            inputQuality = inputQuality
        };
        _jobs.Add(job);

        Debug.Log($"开始加工: {recipe.recipeName}（{recipe.processDays}天后完成）");
        return true;
    }

    /// <summary>收取成品（返回产出的物品ID）</summary>
    public bool CollectJob(int jobIndex, out string outputItemId, out int outputCount)
    {
        outputItemId = null;
        outputCount = 0;

        if (jobIndex < 0 || jobIndex >= _jobs.Count) return false;
        var job = _jobs[jobIndex];

        int currentDay = GameManager.Instance != null ? GameManager.Instance.day : 1;
        if (!job.IsFinished(currentDay)) return false;

        // 产出成品
        InventoryManager.Instance?.AddItem(job.outputItemId, job.outputCount);

        outputItemId = job.outputItemId;
        outputCount = job.outputCount;

        _jobs.RemoveAt(jobIndex);
        EventBus.Publish(GameEvent.ArtisanJobCompleted, job.outputItemId);
        return true;
    }

    // ==================== 查询 ====================

    /// <summary>获取所有进行中的任务</summary>
    public List<ArtisanJob> GetPendingJobs() => new(_jobs);

    /// <summary>获取已完成的任务</summary>
    public List<ArtisanJob> GetFinishedJobs()
    {
        int currentDay = GameManager.Instance != null ? GameManager.Instance.day : 1;
        return _jobs.FindAll(job => job.IsFinished(currentDay));
    }

    /// <summary>获取进行中的任务（未完成）</summary>
    public List<ArtisanJob> GetInProgressJobs()
    {
        int currentDay = GameManager.Instance != null ? GameManager.Instance.day : 1;
        return _jobs.FindAll(job => !job.IsFinished(currentDay));
    }

    // ==================== 定价 ====================

    /// <summary>获取配方最终售价（GDD 5.2.4 定价公式）</summary>
    public int GetFinalPrice(ArtisanDeviceData device, ArtisanRecipe recipe, MarketQuality inputQuality)
    {
        // 原料菜市场公告价
        int inputPrice = MarketManager.Instance != null
            ? MarketManager.Instance.GetPrice(recipe.inputItemId, inputQuality)
            : 10;

        // 品质加成
        float qualityMult = MarketCategoryUtils.GetQualityMultiplier(inputQuality);

        // 当日市场波动（产物品类）
        float marketMult = 1.0f;
        if (MarketManager.Instance != null)
            marketMult = MarketManager.Instance.GetCategoryMultiplier(device.outputCategory);

        return Mathf.RoundToInt(inputPrice * device.deviceMultiplier * qualityMult * marketMult);
    }

    // ==================== 存档 ====================

    [System.Serializable]
    public class ArtisanSaveData
    {
        public List<ArtisanJob> jobs;
    }

    public ArtisanSaveData GetSaveData()
    {
        return new ArtisanSaveData { jobs = new List<ArtisanJob>(_jobs) };
    }

    public void LoadSaveData(ArtisanSaveData data)
    {
        if (data?.jobs == null) return;
        _jobs = new List<ArtisanJob>(data.jobs);
    }
}
