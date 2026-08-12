using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 储物箱 — 扩展背包容量，物品存取
/// F键打开后可双向转移物品
/// </summary>
[RequireComponent(typeof(Interactable))]
public class StorageChest : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private int maxSlots = 72;
    [SerializeField] private string chestName = "储物箱";

    private List<InventoryManager.InventorySlot> _storedItems = new();
    private bool _isOpen;

    [System.Serializable]
    public class StorageSaveData
    {
        public List<InventoryManager.InventorySlot> storedItems;
        public string chestName;
    }

    void Awake()
    {
        // 初始化空槽位
        for (int i = 0; i < maxSlots; i++)
            _storedItems.Add(new InventoryManager.InventorySlot { itemId = null, count = 0 });
    }

    /// <summary>打开/关闭储物箱</summary>
    public void Toggle()
    {
        _isOpen = !_isOpen;

        if (_isOpen)
        {
            GameManager.Instance?.SetState(GameState.Menu);
            if (InputManager.Instance != null)
                InputManager.Instance.IsInputLocked = true;
        }
        else
        {
            GameManager.Instance?.SetState(GameState.Playing);
            if (InputManager.Instance != null)
                InputManager.Instance.IsInputLocked = false;
        }
    }

    /// <summary>从背包存入物品到储物箱</summary>
    public bool Deposit(string itemId, int count)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0) return false;

        // 先尝试堆叠到已有槽位
        int remaining = count;
        for (int i = 0; i < _storedItems.Count && remaining > 0; i++)
        {
            if (_storedItems[i].itemId == itemId && _storedItems[i].count < 99)
            {
                int canAdd = Mathf.Min(remaining, 99 - _storedItems[i].count);
                _storedItems[i] = new InventoryManager.InventorySlot
                {
                    itemId = itemId,
                    count = _storedItems[i].count + canAdd
                };
                remaining -= canAdd;
            }
        }

        // 放入空槽
        for (int i = 0; i < _storedItems.Count && remaining > 0; i++)
        {
            if (string.IsNullOrEmpty(_storedItems[i].itemId))
            {
                int canAdd = Mathf.Min(remaining, 99);
                _storedItems[i] = new InventoryManager.InventorySlot
                {
                    itemId = itemId,
                    count = canAdd
                };
                remaining -= canAdd;
            }
        }

        // 从背包移除
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.RemoveItem(itemId, count - remaining);

        EventBus.Publish(GameEvent.InventoryChanged, itemId);
        return remaining < count;
    }

    /// <summary>从储物箱取出物品到背包</summary>
    public bool Withdraw(string itemId, int count)
    {
        // 检查储物箱中是否有足够数量
        int available = 0;
        foreach (var slot in _storedItems)
            if (slot.itemId == itemId)
                available += slot.count;

        if (available < count) return false;

        // 从储物箱移除
        int remaining = count;
        for (int i = 0; i < _storedItems.Count && remaining > 0; i++)
        {
            if (_storedItems[i].itemId == itemId)
            {
                int remove = Mathf.Min(remaining, _storedItems[i].count);
                _storedItems[i] = new InventoryManager.InventorySlot
                {
                    itemId = _storedItems[i].count - remove <= 0 ? null : itemId,
                    count = _storedItems[i].count - remove
                };
                remaining -= remove;
            }
        }

        // 添加到背包
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddItem(itemId, count - remaining);

        EventBus.Publish(GameEvent.InventoryChanged, itemId);
        return remaining < count;
    }

    /// <summary>获取储物箱所有槽位</summary>
    public List<InventoryManager.InventorySlot> GetStoredItems() => _storedItems;

    /// <summary>获取指定物品数量</summary>
    public int GetItemCount(string itemId)
    {
        int total = 0;
        foreach (var slot in _storedItems)
            if (slot.itemId == itemId)
                total += slot.count;
        return total;
    }

    /// <summary>是否打开</summary>
    public bool IsOpen => _isOpen;

    /// <summary>储物箱名称</summary>
    public string ChestName => chestName;

    /// <summary>最大槽位数</summary>
    public int MaxSlots => maxSlots;

    // ==================== 存档 ====================

    public StorageSaveData GetSaveData()
    {
        return new StorageSaveData
        {
            storedItems = new List<InventoryManager.InventorySlot>(_storedItems),
            chestName = chestName
        };
    }

    public void LoadSaveData(StorageSaveData data)
    {
        if (data?.storedItems == null) return;
        _storedItems = new List<InventoryManager.InventorySlot>(data.storedItems);
        while (_storedItems.Count < maxSlots)
            _storedItems.Add(new InventoryManager.InventorySlot { itemId = null, count = 0 });
        chestName = data.chestName;
    }
}
