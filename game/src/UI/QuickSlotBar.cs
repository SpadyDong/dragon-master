using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 快捷装备栏 — 屏幕底部常驻 HUD
/// 最多 8 个快捷槽，数字键 1-8 切换当前选中
/// </summary>
public class QuickSlotBar : MonoBehaviour
{
    public static QuickSlotBar Instance { get; private set; }

    [Header("槽位")]
    [SerializeField] private QuickSlotUI[] quickSlots; // 8个（Inspector 拖入）
    [SerializeField] private int slotCount = 8;

    [Header("当前选中")]
    [SerializeField] private Image selectedHighlight;
    [SerializeField] private TextMeshProUGUI selectedItemName;

    [Header("槽位数据")]
    [SerializeField] private string[] assignedItemIds; // 序列化存储

    private int _selectedIndex = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (assignedItemIds == null || assignedItemIds.Length < slotCount)
            assignedItemIds = new string[slotCount];

        EventBus.Subscribe(GameEvent.EquipmentChanged, Refresh);
        EventBus.Subscribe<string>(GameEvent.InventoryChanged, _ => Refresh());

        Refresh();
    }

    void Update()
    {
        // 数字键 1-8 切换
        for (int i = 0; i < slotCount; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
            }
        }

        // 滚轮切换
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
            SelectSlot((_selectedIndex - 1 + slotCount) % slotCount);
        else if (scroll < 0f)
            SelectSlot((_selectedIndex + 1) % slotCount);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe(GameEvent.EquipmentChanged, Refresh);
    }

    /// <summary>选择槽位</summary>
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slotCount) return;
        _selectedIndex = index;

        // 移动高亮框
        if (selectedHighlight != null && quickSlots != null && index < quickSlots.Length)
        {
            var targetSlot = quickSlots[index];
            if (targetSlot != null)
            {
                selectedHighlight.rectTransform.position = targetSlot.transform.position;
            }
        }

        // 显示物品名
        string itemId = assignedItemIds[_selectedIndex];
        if (!string.IsNullOrEmpty(itemId))
        {
            var item = Resources.Load<ItemData>($"Items/{itemId}");
            if (selectedItemName != null)
                selectedItemName.text = item?.itemName ?? itemId;
        }
        else
        {
            if (selectedItemName != null) selectedItemName.text = "";
        }

        EventBus.Publish(GameEvent.ItemUsed, itemId ?? "");
    }

    /// <summary>设置槽位物品（从背包拖入或右键）</summary>
    public void SetSlotItem(int index, string itemId)
    {
        if (index < 0 || index >= slotCount) return;
        assignedItemIds[index] = itemId;
        Refresh();
    }

    /// <summary>获取当前选中物品ID</summary>
    public string GetSelectedItemId()
    {
        if (_selectedIndex < 0 || _selectedIndex >= slotCount) return null;
        return assignedItemIds[_selectedIndex];
    }

    /// <summary>消耗当前选中物品（使用后减少）</summary>
    public void ConsumeSelectedItem()
    {
        string itemId = GetSelectedItemId();
        if (string.IsNullOrEmpty(itemId)) return;
        if (!InventoryManager.Instance.HasItem(itemId, 1)) return;

        InventoryManager.Instance.RemoveItem(itemId, 1);
        if (!InventoryManager.Instance.HasItem(itemId, 1))
        {
            assignedItemIds[_selectedIndex] = null;
        }
        Refresh();
    }

    /// <summary>刷新显示</summary>
    public void Refresh()
    {
        if (quickSlots == null) return;

        for (int i = 0; i < quickSlots.Length && i < slotCount; i++)
        {
            if (quickSlots[i] == null) continue;

            string itemId = (assignedItemIds != null && i < assignedItemIds.Length)
                ? assignedItemIds[i] : null;

            // 检查物品是否还在背包中
            if (!string.IsNullOrEmpty(itemId) && !InventoryManager.Instance.HasItem(itemId, 1))
                itemId = null;

            var item = string.IsNullOrEmpty(itemId) ? null : Resources.Load<ItemData>($"Items/{itemId}");
            quickSlots[i].Setup(itemId, item?.icon, i == _selectedIndex);
        }

        // 更新选中物品名
        string selectedId = GetSelectedItemId();
        if (!string.IsNullOrEmpty(selectedId))
        {
            var selItem = Resources.Load<ItemData>($"Items/{selectedId}");
            if (selectedItemName != null)
                selectedItemName.text = selItem?.itemName ?? selectedId;
        }
    }

    public int SelectedIndex => _selectedIndex;
}

/// <summary>
/// 单个快捷槽 UI
/// </summary>
public class QuickSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Color selectedColor = new(1f, 0.9f, 0.5f);
    [SerializeField] private Color normalColor = new(0.2f, 0.2f, 0.2f);

    private int _index;

    public void Setup(string itemId, Sprite itemIcon, bool isSelected)
    {
        if (icon != null)
        {
            icon.sprite = itemIcon;
            icon.gameObject.SetActive(itemIcon != null);
        }

        if (background != null)
            background.color = isSelected ? selectedColor : normalColor;

        // 显示数量
        if (!string.IsNullOrEmpty(itemId) && InventoryManager.Instance != null)
        {
            int count = InventoryManager.Instance.GetItemCount(itemId);
            if (countText != null)
            {
                countText.text = count > 1 ? count.ToString() : "";
                countText.gameObject.SetActive(count > 1);
            }
        }
        else
        {
            if (countText != null) countText.gameObject.SetActive(false);
        }
    }
}
