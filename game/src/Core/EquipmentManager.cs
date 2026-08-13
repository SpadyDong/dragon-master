using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 装备元素匹配结果 — GDD 6.4.2
/// </summary>
public enum EquipmentMatchResult
{
    Match,           // 元素匹配：100%伤害 + 可触发元素反应
    NoElement,       // 玩家无主属性或装备无元素：可装备但无元素加成
    WeaponMismatch,  // 同类武器但元素不匹配：攻击 ×50%
    Incompatible     // 完全不符：不可装备
}

/// <summary>
/// 装备槽位类型
/// 主角可同时装备三种武器：单手剑、大剑、弓箭
/// </summary>
public enum EquipmentSlotType
{
    Sword,       // 单手剑
    Greatsword,  // 大剑/双手剑
    Bow,         // 弓箭
    Helmet,      // 头盔
    Armor,       // 护甲
    Boots,       // 鞋子
    Accessory1,  // 饰品1
    Accessory2   // 饰品2
}

/// <summary>
/// 装备管理器 — 管理玩家 8 个装备栏（3武器 + 3防具 + 2饰品）
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }
    public const int SLOT_COUNT = 8;

    /// <summary>装备槽数据</summary>
    [System.Serializable]
    public struct EquipmentSlot
    {
        public string itemId;   // null = 空槽
        public ItemData ItemData => string.IsNullOrEmpty(itemId) ? null : Resources.Load<ItemData>($"Items/{itemId}");
    }

    private EquipmentSlot[] _slots = new EquipmentSlot[SLOT_COUNT];

    /// <summary>龙装备槽数据（龙鞍/龙鳞甲/龙饰）</summary>
    private string[] _dragonSlots = new string[DragonEquipmentUtils.DRAGON_SLOT_COUNT];

    /// <summary>当前激活的武器槽（0=单手剑, 1=大剑, 2=弓箭）</summary>
    private int _currentWeaponIndex = 0;
    public int CurrentWeaponIndex
    {
        get => _currentWeaponIndex;
        set { _currentWeaponIndex = Mathf.Clamp(value, 0, 2); OnWeaponChanged(); }
    }

    [System.Serializable]
    public class EquipmentSaveData
    {
        public string[] slotItemIds;
        public int currentWeaponIndex;
        public string[] dragonSlotItemIds;
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 默认选中单手剑
        CurrentWeaponIndex = 0;
    }

    /// <summary>切换武器（滚轮或快捷键）</summary>
    public void SwitchWeapon(int direction)
    {
        // 跳过空槽位
        for (int attempt = 0; attempt < 3; attempt++)
        {
            _currentWeaponIndex = (_currentWeaponIndex + direction + 3) % 3;
            if (!string.IsNullOrEmpty(_slots[_currentWeaponIndex].itemId))
                break;
        }
        OnWeaponChanged();
    }

    /// <summary>直接切换到指定武器槽</summary>
    public void SwitchToWeapon(int index)
    {
        if (index < 0 || index > 2) return;
        _currentWeaponIndex = index;
        OnWeaponChanged();
    }

    private void OnWeaponChanged()
    {
        EventBus.Publish(GameEvent.WeaponSwitched, _currentWeaponIndex);
        Debug.Log($"切换武器: {GetSlotName((EquipmentSlotType)_currentWeaponIndex)} — {GetEquippedItemId((EquipmentSlotType)_currentWeaponIndex) ?? "空"}");
    }

    /// <summary>获取当前武器槽的槽位类型</summary>
    public EquipmentSlotType CurrentWeaponSlot => (EquipmentSlotType)_currentWeaponIndex;

    /// <summary>获取当前武器物品ID</summary>
    public string GetCurrentWeaponId() => _slots[_currentWeaponIndex].itemId;

    /// <summary>获取当前武器数据</summary>
    public ItemData GetCurrentWeaponData()
    {
        var id = _slots[_currentWeaponIndex].itemId;
        return string.IsNullOrEmpty(id) ? null : Resources.Load<ItemData>($"Items/{id}");
    }

    /// <summary>装备物品到指定槽位</summary>
    public bool Equip(EquipmentSlotType slotType, string itemId)
    {
        int idx = (int)slotType;
        if (idx < 0 || idx >= SLOT_COUNT) return false;

        var item = Resources.Load<ItemData>($"Items/{itemId}");
        if (item == null) return false;

        // 验证物品类型是否匹配槽位
        if (!IsValidForSlot(slotType, item)) return false;

        // 卸下旧装备
        string oldItem = _slots[idx].itemId;
        if (!string.IsNullOrEmpty(oldItem))
            InventoryManager.Instance.AddItem(oldItem, 1);

        // 从背包移除并装备
        if (!InventoryManager.Instance.RemoveItem(itemId, 1))
        {
            if (!string.IsNullOrEmpty(oldItem))
                InventoryManager.Instance.RemoveItem(oldItem, 1);
            return false;
        }

        _slots[idx].itemId = itemId;

        // 如果装备到当前武器槽，通知切换
        if (idx == _currentWeaponIndex)
            OnWeaponChanged();

        EventBus.Publish(GameEvent.EquipmentChanged);
        return true;
    }

    /// <summary>卸下指定槽位装备</summary>
    public bool Unequip(EquipmentSlotType slotType)
    {
        int idx = (int)slotType;
        if (idx < 0 || idx >= SLOT_COUNT) return false;
        if (string.IsNullOrEmpty(_slots[idx].itemId)) return false;

        string itemId = _slots[idx].itemId;
        InventoryManager.Instance.AddItem(itemId, 1);
        _slots[idx].itemId = null;

        // 如果卸下的是当前武器
        if (idx == _currentWeaponIndex)
            OnWeaponChanged();

        EventBus.Publish(GameEvent.EquipmentChanged);
        return true;
    }

    /// <summary>获取指定槽位装备ID</summary>
    public string GetEquippedItemId(EquipmentSlotType slotType)
    {
        int idx = (int)slotType;
        if (idx < 0 || idx >= SLOT_COUNT) return null;
        return _slots[idx].itemId;
    }

    /// <summary>获取指定槽位装备数据</summary>
    public ItemData GetEquippedItem(EquipmentSlotType slotType)
    {
        var id = GetEquippedItemId(slotType);
        return string.IsNullOrEmpty(id) ? null : Resources.Load<ItemData>($"Items/{id}");
    }

    /// <summary>获取所有槽位</summary>
    public EquipmentSlot[] GetAllSlots() => _slots;

    /// <summary>获取所有武器槽位（前3个）</summary>
    public EquipmentSlot[] GetWeaponSlots() => new[] { _slots[0], _slots[1], _slots[2] };

    /// <summary>获取装备提供的攻击力加成（含品质加成）</summary>
    public int GetAttackBonus()
    {
        int bonus = 0;
        foreach (var slot in _slots)
        {
            if (!string.IsNullOrEmpty(slot.itemId))
            {
                var item = slot.ItemData;
                if (item != null) bonus += item.EffectiveAttack;
            }
        }
        return bonus;
    }

    /// <summary>获取当前武器攻击力（含品质加成）</summary>
    public int GetCurrentWeaponAttack()
    {
        var item = GetCurrentWeaponData();
        return item?.EffectiveAttack ?? 0;
    }

    /// <summary>获取装备提供的防御力加成（含品质加成）</summary>
    public int GetDefenseBonus()
    {
        int bonus = 0;
        foreach (var slot in _slots)
        {
            if (!string.IsNullOrEmpty(slot.itemId))
            {
                var item = slot.ItemData;
                if (item != null) bonus += item.EffectiveDefense;
            }
        }
        return bonus;
    }

    // ==================== 元素绑定规则 — GDD 6.4.2 ====================

    /// <summary>
    /// 检查装备元素是否与玩家主属性匹配
    /// 匹配：100%伤害 + 可触发元素反应
    /// 不匹配但同类武器：50%攻击
    /// 完全不符：不可装备
    /// </summary>
    public static EquipmentMatchResult CheckElementMatch(ItemData item, DragonElement? playerElement)
    {
        // 无元素装备（如工具/普通防具）→ 始终可装备
        if (!item.element.HasValue) return EquipmentMatchResult.Match;

        // 玩家无主属性 → 可装备但无法触发元素反应
        if (!playerElement.HasValue) return EquipmentMatchResult.NoElement;

        // 元素匹配
        if (item.element.Value == playerElement.Value) return EquipmentMatchResult.Match;

        // 同类武器但元素不匹配
        if (item.type == ItemType.Weapon) return EquipmentMatchResult.WeaponMismatch;

        // 完全不符
        return EquipmentMatchResult.Incompatible;
    }

    /// <summary>装备时验证元素匹配</summary>
    public bool CanEquipWithElement(ItemData item, DragonElement? playerElement)
    {
        var result = CheckElementMatch(item, playerElement);
        return result != EquipmentMatchResult.Incompatible;
    }

    /// <summary>获取当前武器的元素属性</summary>
    public DragonElement? GetCurrentWeaponElement()
    {
        var item = GetCurrentWeaponData();
        return item?.element;
    }

    /// <summary>获取当前武器的有效伤害倍率（元素匹配时100%，不匹配50%）</summary>
    public float GetCurrentWeaponDamageMultiplier(DragonElement? playerElement)
    {
        var item = GetCurrentWeaponData();
        if (item == null) return 1.0f;

        var match = CheckElementMatch(item, playerElement);
        return match switch
        {
            EquipmentMatchResult.Match           => 1.0f,
            EquipmentMatchResult.WeaponMismatch  => 0.5f,
            EquipmentMatchResult.NoElement       => 0.7f,
            _ => 0f
        };
    }

    /// <summary>验证物品类型是否匹配槽位</summary>
    public static bool IsValidForSlot(EquipmentSlotType slot, ItemData item)
    {
        if (item.type == ItemType.Weapon)
        {
            return slot switch
            {
                EquipmentSlotType.Sword => item.weaponSubType == WeaponSubType.Sword,
                EquipmentSlotType.Greatsword => item.weaponSubType == WeaponSubType.Greatsword,
                EquipmentSlotType.Bow => item.weaponSubType == WeaponSubType.Bow,
                _ => false
            };
        }

        if (item.type == ItemType.Tool && slot == EquipmentSlotType.Sword)
            return true; // 工具也可装单手剑槽

        return slot switch
        {
            EquipmentSlotType.Helmet => item.type == ItemType.Armor,
            EquipmentSlotType.Armor => item.type == ItemType.Armor,
            EquipmentSlotType.Boots => item.type == ItemType.Armor,
            EquipmentSlotType.Accessory1 or EquipmentSlotType.Accessory2 => item.type == ItemType.Accessory,
            _ => false
        };
    }

    /// <summary>根据武器子类型获取对应槽位</summary>
    public static EquipmentSlotType GetSlotForWeapon(WeaponSubType subType)
    {
        return subType switch
        {
            WeaponSubType.Sword => EquipmentSlotType.Sword,
            WeaponSubType.Greatsword => EquipmentSlotType.Greatsword,
            WeaponSubType.Bow => EquipmentSlotType.Bow,
            _ => EquipmentSlotType.Sword
        };
    }

    /// <summary>槽位中文名</summary>
    public static string GetSlotName(EquipmentSlotType slot)
    {
        return slot switch
        {
            EquipmentSlotType.Sword => "单手剑",
            EquipmentSlotType.Greatsword => "大剑",
            EquipmentSlotType.Bow => "弓箭",
            EquipmentSlotType.Helmet => "头盔",
            EquipmentSlotType.Armor => "护甲",
            EquipmentSlotType.Boots => "鞋子",
            EquipmentSlotType.Accessory1 => "饰品1",
            EquipmentSlotType.Accessory2 => "饰品2",
            _ => "未知"
        };
    }

    /// <summary>武器槽索引列表</summary>
    public static readonly EquipmentSlotType[] WeaponSlots = {
        EquipmentSlotType.Sword, EquipmentSlotType.Greatsword, EquipmentSlotType.Bow
    };

    // ==================== 龙装备管理 — GDD 6.4.2 ====================

    /// <summary>装备龙装备到指定槽位</summary>
    public bool EquipDragonEquipment(DragonEquipmentSubType slot, string itemId, DragonElement dragonElement)
    {
        int idx = DragonEquipmentUtils.GetSlotIndex(slot);
        if (idx < 0) return false;

        var item = Resources.Load<ItemData>($"Items/{itemId}");
        if (item == null) return false;

        // 验证物品类型
        if (!DragonEquipmentUtils.IsValidForSlot(slot, item)) return false;

        // 卸下旧装备
        if (!string.IsNullOrEmpty(_dragonSlots[idx]))
            InventoryManager.Instance.AddItem(_dragonSlots[idx], 1);

        // 从背包移除并装备
        if (!InventoryManager.Instance.RemoveItem(itemId, 1))
        {
            if (!string.IsNullOrEmpty(_dragonSlots[idx]))
                InventoryManager.Instance.RemoveItem(_dragonSlots[idx], 1);
            return false;
        }

        _dragonSlots[idx] = itemId;
        EventBus.Publish(GameEvent.EquipmentChanged);
        return true;
    }

    /// <summary>卸下龙装备</summary>
    public bool UnequipDragonEquipment(DragonEquipmentSubType slot)
    {
        int idx = DragonEquipmentUtils.GetSlotIndex(slot);
        if (idx < 0 || string.IsNullOrEmpty(_dragonSlots[idx])) return false;

        InventoryManager.Instance.AddItem(_dragonSlots[idx], 1);
        _dragonSlots[idx] = null;
        EventBus.Publish(GameEvent.EquipmentChanged);
        return true;
    }

    /// <summary>获取龙装备ID</summary>
    public string GetDragonEquipmentId(DragonEquipmentSubType slot)
    {
        int idx = DragonEquipmentUtils.GetSlotIndex(slot);
        return (idx >= 0) ? _dragonSlots[idx] : null;
    }

    /// <summary>获取龙装备数据</summary>
    public ItemData GetDragonEquipment(DragonEquipmentSubType slot)
    {
        var id = GetDragonEquipmentId(slot);
        return string.IsNullOrEmpty(id) ? null : Resources.Load<ItemData>($"Items/{id}");
    }

    /// <summary>获取龙装备防御加成（龙鳞甲）</summary>
    public int GetDragonDefenseBonus()
    {
        int bonus = 0;
        foreach (var id in _dragonSlots)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var item = Resources.Load<ItemData>($"Items/{id}");
                if (item != null) bonus += item.EffectiveDefense;
            }
        }
        return bonus;
    }

    /// <summary>获取龙装备攻击加成（龙鞍/龙饰）</summary>
    public int GetDragonAttackBonus()
    {
        int bonus = 0;
        foreach (var id in _dragonSlots)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var item = Resources.Load<ItemData>($"Items/{id}");
                if (item != null) bonus += item.EffectiveAttack;
            }
        }
        return bonus;
    }

    /// <summary>获取所有龙装备槽位ID</summary>
    public string[] GetDragonSlots() => _dragonSlots;

    public EquipmentSaveData GetSaveData()
    {
        var data = new EquipmentSaveData
        {
            slotItemIds = new string[SLOT_COUNT],
            currentWeaponIndex = _currentWeaponIndex,
            dragonSlotItemIds = new string[DragonEquipmentUtils.DRAGON_SLOT_COUNT]
        };
        for (int i = 0; i < SLOT_COUNT; i++)
            data.slotItemIds[i] = _slots[i].itemId ?? "";
        for (int i = 0; i < DragonEquipmentUtils.DRAGON_SLOT_COUNT; i++)
            data.dragonSlotItemIds[i] = _dragonSlots[i] ?? "";
        return data;
    }

    public void LoadSaveData(EquipmentSaveData data)
    {
        if (data?.slotItemIds == null) return;
        for (int i = 0; i < Mathf.Min(SLOT_COUNT, data.slotItemIds.Length); i++)
            _slots[i].itemId = string.IsNullOrEmpty(data.slotItemIds[i]) ? null : data.slotItemIds[i];
        _currentWeaponIndex = Mathf.Clamp(data.currentWeaponIndex, 0, 2);

        // 加载龙装备（兼容旧存档：dragonSlotItemIds 为 null 时跳过）
        if (data.dragonSlotItemIds != null)
        {
            for (int i = 0; i < Mathf.Min(DragonEquipmentUtils.DRAGON_SLOT_COUNT, data.dragonSlotItemIds.Length); i++)
                _dragonSlots[i] = string.IsNullOrEmpty(data.dragonSlotItemIds[i]) ? null : data.dragonSlotItemIds[i];
        }
    }
}
