using UnityEngine;
using System.Collections.Generic;

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

    /// <summary>获取装备提供的攻击力加成</summary>
    public int GetAttackBonus()
    {
        int bonus = 0;
        foreach (var slot in _slots)
        {
            if (!string.IsNullOrEmpty(slot.itemId))
            {
                var item = slot.ItemData;
                if (item != null) bonus += item.attackBonus;
            }
        }
        return bonus;
    }

    /// <summary>获取当前武器攻击力</summary>
    public int GetCurrentWeaponAttack()
    {
        var item = GetCurrentWeaponData();
        return item?.attackBonus ?? 0;
    }

    /// <summary>获取装备提供的防御力加成</summary>
    public int GetDefenseBonus()
    {
        int bonus = 0;
        foreach (var slot in _slots)
        {
            if (!string.IsNullOrEmpty(slot.itemId))
            {
                var item = slot.ItemData;
                if (item != null) bonus += item.defenseBonus;
            }
        }
        return bonus;
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

    public EquipmentSaveData GetSaveData()
    {
        var data = new EquipmentSaveData
        {
            slotItemIds = new string[SLOT_COUNT],
            currentWeaponIndex = _currentWeaponIndex
        };
        for (int i = 0; i < SLOT_COUNT; i++)
            data.slotItemIds[i] = _slots[i].itemId ?? "";
        return data;
    }

    public void LoadSaveData(EquipmentSaveData data)
    {
        if (data?.slotItemIds == null) return;
        for (int i = 0; i < Mathf.Min(SLOT_COUNT, data.slotItemIds.Length); i++)
            _slots[i].itemId = string.IsNullOrEmpty(data.slotItemIds[i]) ? null : data.slotItemIds[i];
        _currentWeaponIndex = Mathf.Clamp(data.currentWeaponIndex, 0, 2);
    }
}
