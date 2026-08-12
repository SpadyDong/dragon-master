using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 畜牧全局管理器
/// 维护所有 AnimalController，监听跨天事件推进产出/心情衰减
/// 天气联动：雨天回舍、雷暴受惊不产出、大雪需保暖
/// </summary>
public class LivestockManager : MonoBehaviour
{
    public static LivestockManager Instance { get; private set; }

    private List<AnimalController> _allAnimals = new();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        EventBus.Subscribe<int>(GameEvent.DayChanged, OnDayChanged);

        // 注册场景中所有动物
        _allAnimals = new List<AnimalController>(FindObjectsByType<AnimalController>(FindObjectsSortMode.None));
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.DayChanged, OnDayChanged);
    }

    /// <summary>注册新购买的动物</summary>
    public void RegisterAnimal(AnimalController animal)
    {
        if (!_allAnimals.Contains(animal))
            _allAnimals.Add(animal);
    }

    /// <summary>取消注册（出售/屠宰后）</summary>
    public void UnregisterAnimal(AnimalController animal)
    {
        _allAnimals.Remove(animal);
    }

    // ==================== 跨天推进 ====================

    private void OnDayChanged(int day)
    {
        // 1. 天气联动（在跨天推进前应用）
        ApplyWeatherToAnimals();

        // 2. 所有动物跨天
        foreach (var animal in _allAnimals)
            animal.AdvanceDay();
    }

    /// <summary>天气联动：影响所有动物</summary>
    private void ApplyWeatherToAnimals()
    {
        if (GameManager.Instance == null) return;
        int weather = GameManager.Instance.weatherIndex;

        // 仅极端天气影响
        if (weather == 2 || weather == 4 || weather == 6) // 小雨/雷暴/大雪
        {
            foreach (var animal in _allAnimals)
                animal.ApplyWeatherEffects(weather);
        }
    }

    // ==================== 查询 ====================

    /// <summary>获取所有动物</summary>
    public List<AnimalController> GetAllAnimals() => _allAnimals;

    /// <summary>获取有产出就绪的动物</summary>
    public List<AnimalController> GetAnimalsWithProduct()
    {
        var result = new List<AnimalController>();
        foreach (var animal in _allAnimals)
            if (animal.ProductReady)
                result.Add(animal);
        return result;
    }

    /// <summary>获取未喂食的动物</summary>
    public List<AnimalController> GetHungryAnimals()
    {
        var result = new List<AnimalController>();
        foreach (var animal in _allAnimals)
            if (!animal.FedToday)
                result.Add(animal);
        return result;
    }

    /// <summary>获取指定类型的动物</summary>
    public List<AnimalController> GetAnimalsByType(AnimalType type)
    {
        var result = new List<AnimalController>();
        foreach (var animal in _allAnimals)
            if (animal.Data != null && animal.Data.type == type)
                result.Add(animal);
        return result;
    }

    /// <summary>购买动物</summary>
    public bool PurchaseAnimal(AnimalData animalData, Vector2 homePosition)
    {
        if (animalData == null) return false;
        if (!GameManager.Instance.SpendGold(animalData.purchasePrice)) return false;

        // 创建动物 GameObject
        GameObject animalObj = new GameObject(animalData.animalName);
        animalObj.transform.position = homePosition;

        var rb = animalObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        var sr = animalObj.AddComponent<SpriteRenderer>();
        sr.sprite = animalData.idleSprite;
        sr.sortingOrder = 50;

        var controller = animalObj.AddComponent<AnimalController>();
        controller.SetData(animalData);
        controller.SetHome(homePosition);

        var col = animalObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 0.6f);

        RegisterAnimal(controller);
        return true;
    }

    /// <summary>获取所有动物存档数据</summary>
    public List<AnimalController.AnimalSaveData> GetAllSaveData()
    {
        var result = new List<AnimalController.AnimalSaveData>();
        foreach (var animal in _allAnimals)
            result.Add(animal.GetSaveData());
        return result;
    }

    /// <summary>加载存档数据</summary>
    public void LoadAllSaveData(List<AnimalController.AnimalSaveData> data)
    {
        // 存档加载后由各 AnimalController.LoadSaveData 恢复
        // 实际加载逻辑在场景初始化时调用
        foreach (var saveData in data)
        {
            var animalData = Resources.Load<AnimalData>($"Animals/{saveData.animalId}");
            if (animalData == null) continue;

            // 查找已有动物或创建新实例
            AnimalController existing = _allAnimals.Find(a => a.Data != null && a.Data.animalId == saveData.animalId);
            if (existing != null)
            {
                existing.LoadSaveData(saveData);
            }
            else
            {
                // 创建新实例
                GameObject animalObj = new GameObject(animalData.animalName);
                animalObj.transform.position = new Vector2(saveData.homeX, saveData.homeY);

                var rb = animalObj.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0;
                rb.freezeRotation = true;

                var sr = animalObj.AddComponent<SpriteRenderer>();
                sr.sprite = animalData.idleSprite;

                var controller = animalObj.AddComponent<AnimalController>();
                controller.LoadSaveData(saveData);

                RegisterAnimal(controller);
            }
        }
    }
}
