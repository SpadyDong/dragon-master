using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 背包系统管理器
/// 管理物品添加、移除、堆叠、查询
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("设置")]
    [SerializeField] private int maxSlots = 36;

    private List<InventorySlot> _slots = new();

    [System.Serializable]
    public struct InventorySlot
    {
        public string itemId;
        public int count;
    }

    [System.Serializable]
    public class InventorySaveData
    {
        public List<InventorySlot> slots;
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 初始化空槽位
        for (int i = 0; i < maxSlots; i++)
            _slots.Add(new InventorySlot { itemId = null, count = 0 });
    }

    /// <summary>添加物品（返回实际成功添加的数量）</summary>
    public int AddItem(string itemId, int count = 1)
    {
        ItemData item = GetItemData(itemId);
        if (item == null) return 0;

        int added = 0;
        int remaining = count;

        // 先尝试堆叠到已有同类槽位
        for (int i = 0; i < _slots.Count && remaining > 0; i++)
        {
            if (_slots[i].itemId == itemId && _slots[i].count < item.maxStack)
            {
                int canAdd = Mathf.Min(remaining, item.maxStack - _slots[i].count);
                _slots[i] = new InventorySlot { itemId = itemId, count = _slots[i].count + canAdd };
                remaining -= canAdd;
                added += canAdd;
            }
        }

        // 放不下的放新空槽
        for (int i = 0; i < _slots.Count && remaining > 0; i++)
        {
            if (string.IsNullOrEmpty(_slots[i].itemId))
            {
                int canAdd = Mathf.Min(remaining, item.maxStack);
                _slots[i] = new InventorySlot { itemId = itemId, count = canAdd };
                remaining -= canAdd;
                added += canAdd;
            }
        }

        if (added > 0)
            EventBus.Publish(GameEvent.InventoryChanged, itemId);

        return added;
    }

    /// <summary>移除物品（返回是否成功）</summary>
    public bool RemoveItem(string itemId, int count = 1)
    {
        if (GetItemCount(itemId) < count) return false;

        int remaining = count;
        for (int i = 0; i < _slots.Count && remaining > 0; i++)
        {
            if (_slots[i].itemId == itemId)
            {
                int remove = Mathf.Min(remaining, _slots[i].count);
                _slots[i] = new InventorySlot
                {
                    itemId = _slots[i].count - remove <= 0 ? null : itemId,
                    count = _slots[i].count - remove
                };
                remaining -= remove;
            }
        }

        EventBus.Publish(GameEvent.InventoryChanged, itemId);
        return true;
    }

    /// <summary>获取物品数量</summary>
    public int GetItemCount(string itemId)
    {
        int total = 0;
        foreach (var slot in _slots)
        {
            if (slot.itemId == itemId)
                total += slot.count;
        }
        return total;
    }

    /// <summary>是否持有至少N个指定物品</summary>
    public bool HasItem(string itemId, int count = 1)
    {
        return GetItemCount(itemId) >= count;
    }

    /// <summary>获取所有非空槽位</summary>
    public List<InventorySlot> GetAllSlots() => _slots;

    /// <summary>获取保存数据</summary>
    public InventorySaveData GetSaveData()
    {
        return new InventorySaveData { slots = new List<InventorySlot>(_slots) };
    }

    /// <summary>加载保存数据</summary>
    public void LoadSaveData(InventorySaveData data)
    {
        _slots = new List<InventorySlot>(data.slots);
        while (_slots.Count < maxSlots)
            _slots.Add(new InventorySlot { itemId = null, count = 0 });
    }

    /// <summary>查找物品数据（后续可用 Addressables 或 Resource.Load）</summary>
    private ItemData GetItemData(string itemId)
    {
        // M1 简易版：Resources.Load
        return Resources.Load<ItemData>($"Items/{itemId}");
    }
}
