using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 装备槽位类型
/// </summary>
public enum EquipmentSlotType
{
    Weapon,     // 武器
    Helmet,     // 头盔
    Armor,      // 护甲
    Boots,      // 鞋子
    Accessory1, // 饰品1
    Accessory2  // 饰品2
}

/// <summary>
/// 装备管理器 — 管理玩家装备栏
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }
    public const int SLOT_COUNT = 6;

    /// <summary>装备槽数据</summary>
    [System.Serializable]
    public struct EquipmentSlot
    {
        public string itemId;   // null = 空槽
        public ItemData ItemData => string.IsNullOrEmpty(itemId) ? null : Resources.Load<ItemData>($"Items/{itemId}");
    }

    private EquipmentSlot[] _slots = new EquipmentSlot[SLOT_COUNT];

    [System.Serializable]
    public class EquipmentSaveData
    {
        public string[] slotItemIds;
    }

    void Awake()
    {
        Instance = this;
    }

    /// <summary>装备物品到指定槽位</summary>
    public bool Equip(EquipmentSlotType slotType, string itemId)
    {
        int idx = (int)slotType;
        if (idx < 0 || idx >= SLOT_COUNT) return false;

        var item = Resources.Load<ItemData>($"Items/{itemId}");
        if (item == null) return false;

        // 验证物品类型是否匹配槽位
        if (!IsValidForSlot(slotType, item.type)) return false;

        // 卸下旧装备
        string oldItem = _slots[idx].itemId;
        if (!string.IsNullOrEmpty(oldItem))
            InventoryManager.Instance.AddItem(oldItem, 1);

        // 从背包移除并装备
        if (!InventoryManager.Instance.RemoveItem(itemId, 1))
        {
            // 无法移除则放回去
            if (!string.IsNullOrEmpty(oldItem))
                InventoryManager.Instance.RemoveItem(oldItem, 1);
            return false;
        }

        _slots[idx].itemId = itemId;
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
    public static bool IsValidForSlot(EquipmentSlotType slot, ItemType itemType)
    {
        return slot switch
        {
            EquipmentSlotType.Weapon => itemType == ItemType.Weapon || itemType == ItemType.Tool,
            EquipmentSlotType.Helmet => itemType == ItemType.Armor,
            EquipmentSlotType.Armor => itemType == ItemType.Armor,
            EquipmentSlotType.Boots => itemType == ItemType.Armor,
            EquipmentSlotType.Accessory1 or EquipmentSlotType.Accessory2 => itemType == ItemType.Accessory,
            _ => false
        };
    }

    /// <summary>槽位中文名</summary>
    public static string GetSlotName(EquipmentSlotType slot)
    {
        return slot switch
        {
            EquipmentSlotType.Weapon => "武器",
            EquipmentSlotType.Helmet => "头盔",
            EquipmentSlotType.Armor => "护甲",
            EquipmentSlotType.Boots => "鞋子",
            EquipmentSlotType.Accessory1 => "饰品1",
            EquipmentSlotType.Accessory2 => "饰品2",
            _ => "未知"
        };
    }

    public EquipmentSaveData GetSaveData()
    {
        var data = new EquipmentSaveData { slotItemIds = new string[SLOT_COUNT] };
        for (int i = 0; i < SLOT_COUNT; i++)
            data.slotItemIds[i] = _slots[i].itemId ?? "";
        return data;
    }

    public void LoadSaveData(EquipmentSaveData data)
    {
        if (data?.slotItemIds == null) return;
        for (int i = 0; i < Mathf.Min(SLOT_COUNT, data.slotItemIds.Length); i++)
            _slots[i].itemId = string.IsNullOrEmpty(data.slotItemIds[i]) ? null : data.slotItemIds[i];
    }
}
