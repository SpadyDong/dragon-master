using System;
using System.Collections.Generic;

/// <summary>
/// 全局游戏事件枚举
/// </summary>
public enum GameEvent
{
    // 时间
    MinuteChanged, HourChanged, DayChanged, SeasonChanged, YearChanged,
    // 天气
    WeatherChanged,
    // 玩家
    PlayerMoved, PlayerInteracted, PlayerStatsChanged, PlayerGoldChanged,
    PlayerStaminaChanged, PlayerHPChanged, PlayerHungerChanged,
    // NPC
    NPCDialogueStarted, NPCDialogueEnded, NPCAffectionChanged,
    // 物品
    ItemPickedUp, ItemUsed, InventoryChanged, EquipmentChanged, WeaponSwitched,
    // 游戏状态
    GamePaused, GameResumed, GameStateChanged,
    // 种植
    FarmTileStateChanged,    // int tileIndex
    CropPlanted,             // string cropId
    CropHarvested,            // string cropId
    SprinklerActivated,       // int sprinklerIndex
    FruitTreeHarvested,       // string treeType
    BeehiveHarvested,         // string honeyType
    // 畜牧
    AnimalFed,                // string animalId
    AnimalPetted,             // string animalId
    AnimalProductCollected,   // string productId
    AnimalMoodChanged,        // string animalId
    // 驯龙
    DragonStageChanged,      // string dragonUid
    DragonFed,                // string dragonUid
    DragonPetted,             // string dragonUid
    DragonEggLaid,            // string dragonUid
    DragonHatched,            // string dragonUid
    DragonCaptured,           // string dragonSpeciesId
    // 钓鱼
    FishCaught,               // string fishId
    FishingStarted,            // int spotId
    FishingEnded,              // bool success
    FishPondHarvested,         // string fishId
    // 地形
    TerrainChanged,            // int terrainIndex
    // 元素反应
    ElementReactionTriggered,  // string reactionName
    // 玩家元素
    PlayerElementChanged,      // int elementIndex (-1=null)
    // 商店
    ShopOpened,                // string shopId
    ShopClosed,                // string shopId
    ItemBought,                // string itemId
    ItemSold,                  // string itemId
    EquipmentForged,           // string resultItemId
    // 战斗 — M9
    BattleStarted,             // string battleType
    BattleEnded,               // string result ("victory"/"defeat")
    TurnChanged,               // int turnCount
    UnitDamaged,               // string logMessage
    UnitDefeated,              // string unitId
    CaptureAttempted,          // string unitId
    CaptureSuccess,            // string unitId
    // 熟练度 — M10
    ProficiencyLeveledUp,      // int skillIndex
    ProficiencyXPChanged,      // int skillIndex
    // 烹饪 — M10
    RecipeLearned,             // string recipeId
    DishCooked,                // string recipeId
    // 矿洞 — M10
    OreMined,                  // string oreId
    FloorDescended,            // int floor
    // 成就 — M10
    AchievementUnlocked,       // string achievementId
    // 温泉 — M10
    HotSpringUsed,             // 无参数
    // 菜市场 — M10.5
    MarketPriceUpdated,        // 无参数
    // 工匠设备 — M10.5
    ArtisanJobCompleted,       // string outputItemId
    // 结婚 — M6
    MarriageProposed,          // string npcId
    Married,                   // string npcId
    // 节日 — M10
    FestivalStarted,           // string festivalId
    // 社区中心 — M10
    BundleCompleted,           // string bundleId
    // 博物馆 — M10
    MuseumDonated,             // string itemId
    // 秘密纸条 — M10
    NoteFound,                 // int noteId
    // 主线剧情 — M11
    StoryNodeCompleted,        // string nodeName
    RelicCollected,            // string relicName
    StoryActChanged,           // int actIndex
    EndingResolved             // string endingName
}

/// <summary>
/// 全局事件总线 — 跨系统解耦通信
/// 用法：EventBus.Subscribe(GameEvent.DayChanged, OnDayChanged);
///       EventBus.Publish(GameEvent.DayChanged, 3);
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<GameEvent, Delegate> _events = new();

    /// <summary>订阅无参数事件</summary>
    public static void Subscribe(GameEvent eventType, Action handler)
    {
        if (_events.ContainsKey(eventType))
            _events[eventType] = Delegate.Combine(_events[eventType], handler);
        else
            _events[eventType] = handler;
    }

    /// <summary>订阅带参数事件</summary>
    public static void Subscribe<T>(GameEvent eventType, Action<T> handler)
    {
        if (_events.ContainsKey(eventType))
            _events[eventType] = Delegate.Combine(_events[eventType], handler);
        else
            _events[eventType] = handler;
    }

    /// <summary>取消订阅无参数事件</summary>
    public static void Unsubscribe(GameEvent eventType, Action handler)
    {
        if (!_events.ContainsKey(eventType)) return;
        _events[eventType] = Delegate.Remove(_events[eventType], handler);
        if (_events[eventType] == null) _events.Remove(eventType);
    }

    /// <summary>取消订阅带参数事件</summary>
    public static void Unsubscribe<T>(GameEvent eventType, Action<T> handler)
    {
        if (!_events.ContainsKey(eventType)) return;
        _events[eventType] = Delegate.Remove(_events[eventType], handler);
        if (_events[eventType] == null) _events.Remove(eventType);
    }

    /// <summary>发布无参数事件</summary>
    public static void Publish(GameEvent eventType)
    {
        if (!_events.ContainsKey(eventType)) return;
        (_events[eventType] as Action)?.Invoke();
    }

    /// <summary>发布带参数事件</summary>
    public static void Publish<T>(GameEvent eventType, T arg)
    {
        if (!_events.ContainsKey(eventType)) return;
        (_events[eventType] as Action<T>)?.Invoke(arg);
    }

    /// <summary>清空所有事件订阅（仅用于场景卸载）</summary>
    public static void ClearAll()
    {
        _events.Clear();
    }
}
