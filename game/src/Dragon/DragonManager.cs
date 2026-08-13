using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 全局驯龙管理器 — 简化版
/// 龙与普通动物仅两点不同：有元素属性 + 可参战
/// </summary>
public class DragonManager : MonoBehaviour
{
    public static DragonManager Instance { get; private set; }

    private List<DragonController> _allDragons = new();
    private List<DragonController> _tamedDragons = new();
    private List<DragonController> _wildDragons = new();
    private List<DragonController> _eggsInIncubator = new();

    void Awake() => Instance = this;

    void Start()
    {
        EventBus.Subscribe<int>(GameEvent.DayChanged, OnDayChanged);
        EventBus.Subscribe<int>(GameEvent.SeasonChanged, OnSeasonChanged);

        foreach (var dragon in FindObjectsByType<DragonController>(FindObjectsSortMode.None))
            RegisterDragon(dragon);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.DayChanged, OnDayChanged);
        EventBus.Unsubscribe<int>(GameEvent.SeasonChanged, OnSeasonChanged);
    }

    // ==================== 注册 ====================

    public void RegisterDragon(DragonController dragon)
    {
        if (_allDragons.Contains(dragon)) return;
        _allDragons.Add(dragon);

        if (dragon.Stage == DragonStage.Egg) _eggsInIncubator.Add(dragon);
        else if (dragon.IsWild) _wildDragons.Add(dragon);
        else _tamedDragons.Add(dragon);
    }

    public void UnregisterDragon(DragonController dragon)
    {
        _allDragons.Remove(dragon);
        _tamedDragons.Remove(dragon);
        _wildDragons.Remove(dragon);
        _eggsInIncubator.Remove(dragon);
    }

    // ==================== 跨天 ====================

    private void OnDayChanged(int day)
    {
        ApplyWeatherToDragons();

        foreach (var dragon in _allDragons)
            dragon.AdvanceDay();

        // 蛋孵化后从孵化间移出
        var hatched = _eggsInIncubator.FindAll(d => d.Stage != DragonStage.Egg);
        foreach (var dragon in hatched)
        {
            _eggsInIncubator.Remove(dragon);
            if (dragon.IsWild) _wildDragons.Add(dragon);
            else _tamedDragons.Add(dragon);
        }

        AutoFeedDragons();
    }

    private void OnSeasonChanged(int season) { }

    private void ApplyWeatherToDragons()
    {
        if (GameManager.Instance == null) return;
        int weather = GameManager.Instance.weatherIndex;
        foreach (var dragon in _allDragons)
        {
            if (dragon.Stage != DragonStage.Egg)
                dragon.ApplyWeatherEffects(weather);
        }
    }

    private void AutoFeedDragons()
    {
        foreach (var dragon in _tamedDragons)
        {
            if (dragon.Stage == DragonStage.Egg || dragon.FedToday) continue;
            if (GameManager.Instance != null && GameManager.Instance.SpendGold(3))
                dragon.Feed();
        }
    }

    // ==================== 繁育 ====================

    public BreedResultType TryBreedDragons(DragonController mother, DragonController father)
    {
        int season = GameManager.Instance != null ? GameManager.Instance.season : 0;
        return DragonBreeding.TryBreed(mother, father, season);
    }

    public bool ExecuteBreed(DragonController mother, DragonController father)
    {
        int season = GameManager.Instance != null ? GameManager.Instance.season : 0;
        return DragonBreeding.ExecuteBreed(mother, father, season);
    }

    // ==================== 创建龙 ====================

    public DragonController CreateDragonEgg(
        DragonData species, DragonGender gender,
        DragonElement primary, DragonElement? secondary,
        Vector3 position)
    {
        if (species == null) return null;

        var obj = new GameObject($"{species.speciesName}_Egg");
        obj.transform.position = position;

        var rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = species.eggSprite;
        sr.sortingOrder = 50;

        var controller = obj.AddComponent<DragonController>();
        controller.InitializeFromEgg(species, gender, primary, secondary);
        controller.SetHome(position);

        var col = obj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 0.6f);

        RegisterDragon(controller);
        return controller;
    }

    public bool PurchaseDragonEgg(DragonData species, Vector3 incubatorPosition)
    {
        if (species == null) return false;
        if (!GameManager.Instance.SpendGold(species.purchasePrice)) return false;

        var gender = Random.value < 0.5f ? DragonGender.Male : DragonGender.Female;

        var obj = new GameObject($"{species.speciesName}_Egg");
        obj.transform.position = incubatorPosition;

        var rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = species.eggSprite;
        sr.sortingOrder = 50;

        var controller = obj.AddComponent<DragonController>();
        controller.Initialize(species, gender, isWild: false);
        controller.SetHome(incubatorPosition);
        controller.SetInDen(true);

        var col = obj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 0.6f);

        RegisterDragon(controller);
        return true;
    }

    // ==================== 捕捉 ====================

    public void OnDragonCaptured(DragonController dragon)
    {
        if (!dragon.IsWild) return;
        _wildDragons.Remove(dragon);
        _tamedDragons.Add(dragon);
    }

    // ==================== 查询 ====================

    public List<DragonController> GetAllDragons() => _allDragons;
    public List<DragonController> GetTamedDragons() => _tamedDragons;
    public List<DragonController> GetWildDragons() => _wildDragons;

    public List<DragonController> GetDragonsByElement(DragonElement element)
    {
        var result = new List<DragonController>();
        foreach (var d in _allDragons)
            if (d.PrimaryElement == element || d.SecondaryElement == element)
                result.Add(d);
        return result;
    }

    public List<DragonController> GetDragonsByStage(DragonStage stage)
    {
        var result = new List<DragonController>();
        foreach (var d in _allDragons)
            if (d.Stage == stage) result.Add(d);
        return result;
    }

    public List<DragonController> GetHungryDragons()
    {
        var result = new List<DragonController>();
        foreach (var d in _tamedDragons)
            if (!d.FedToday && d.Stage != DragonStage.Egg) result.Add(d);
        return result;
    }

    // ==================== 存档 ====================

    public List<DragonController.DragonSaveData> GetAllSaveData()
    {
        var result = new List<DragonController.DragonSaveData>();
        foreach (var d in _allDragons) result.Add(d.GetSaveData());
        return result;
    }

    public void LoadAllSaveData(List<DragonController.DragonSaveData> data)
    {
        if (data == null) return;

        foreach (var dragon in _allDragons)
            if (dragon != null && dragon.gameObject != null)
                Destroy(dragon.gameObject);
        _allDragons.Clear();
        _tamedDragons.Clear();
        _wildDragons.Clear();
        _eggsInIncubator.Clear();

        foreach (var sd in data)
        {
            var species = Resources.Load<DragonData>($"Dragons/{sd.speciesId}");
            if (species == null) continue;

            var obj = new GameObject(species.speciesName);
            obj.transform.position = new Vector2(sd.homeX, sd.homeY);

            var rb = obj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 50;

            var controller = obj.AddComponent<DragonController>();
            controller.LoadSaveData(sd);

            var col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 0.6f);

            RegisterDragon(controller);
        }
    }
}
