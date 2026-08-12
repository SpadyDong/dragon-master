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
    GamePaused, GameResumed, GameStateChanged
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
