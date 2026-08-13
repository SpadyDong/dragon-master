using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 背包面板 — 物品网格 + 物品详情 + 操作按钮
/// 替代旧版 InventoryUI，集成到 UIManager
/// </summary>
public class InventoryPanel : MonoBehaviour
{
    [Header("网格")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("详情")]
    [SerializeField] private Image detailIcon;
    [SerializeField] private TextMeshProUGUI detailName;
    [SerializeField] private TextMeshProUGUI detailType;
    [SerializeField] private TextMeshProUGUI detailDescription;
    [SerializeField] private TextMeshProUGUI detailStats;
    [SerializeField] private GameObject detailPanel;

    [Header("操作按钮")]
    [SerializeField] private Button useButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button dropButton;
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("排序/筛选")]
    [SerializeField] private Button sortButton;
    [SerializeField] private Button filterAllButton;
    [SerializeField] private Button filterEquipButton;
    [SerializeField] private Button filterConsumeButton;
    [SerializeField] private Button filterMaterialButton;

    private List<GameObject> _slotObjects = new();
    private int _selectedIndex = -1;
    private string _selectedItemId;
    private ItemType _filterType = ItemType.Consumable; // 0=all special flag

    private enum FilterMode { All, Equipment, Consumable, Material }
    private FilterMode _filterMode = FilterMode.All;

    void Start()
    {
        if (useButton != null) useButton.onClick.AddListener(OnUseClicked);
        if (equipButton != null) equipButton.onClick.AddListener(OnEquipClicked);
        if (dropButton != null) dropButton.onClick.AddListener(OnDropClicked);
        if (sortButton != null) sortButton.onClick.AddListener(() => { SortInventory(); Refresh(); });

        if (filterAllButton != null) filterAllButton.onClick.AddListener(() => { _filterMode = FilterMode.All; Refresh(); });
        if (filterEquipButton != null) filterEquipButton.onClick.AddListener(() => { _filterMode = FilterMode.Equipment; Refresh(); });
        if (filterConsumeButton != null) filterConsumeButton.onClick.AddListener(() => { _filterMode = FilterMode.Consumable; Refresh(); });
        if (filterMaterialButton != null) filterMaterialButton.onClick.AddListener(() => { _filterMode = FilterMode.Material; Refresh(); });

        if (detailPanel != null) detailPanel.SetActive(false);

        // 订阅背包变化
        EventBus.Subscribe<string>(GameEvent.InventoryChanged, _ => Refresh());
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<string>(GameEvent.InventoryChanged, _ => Refresh());
    }

    /// <summary>刷新背包网格</summary>
    public void Refresh()
    {
        if (InventoryManager.Instance == null) return;
        if (slotContainer == null || slotPrefab == null) return;

        // 清除旧对象
        foreach (var obj in _slotObjects) Destroy(obj);
        _slotObjects.Clear();

        var slots = InventoryManager.Instance.GetAllSlots();
        int visibleCount = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (string.IsNullOrEmpty(slot.itemId)) continue;

            // 应用筛选
            var item = Resources.Load<ItemData>($"Items/{slot.itemId}");
            if (item != null && !PassFilter(item)) continue;

            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            _slotObjects.Add(slotObj);
            visibleCount++;

            var slotUI = slotObj.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                int idx = i; // 闭包捕获
                slotUI.Setup(slot, idx, OnSlotClicked);
                // 设置图标和数量
                slotUI.SetIcon(item?.icon);
                slotUI.SetCount(slot.count);
            }
        }

        // 空槽位补齐
        int emptyCount = InventoryManager.Instance.GetAllSlots().Count - visibleCount;
        for (int i = 0; i < Mathf.Min(emptyCount, 12); i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            _slotObjects.Add(slotObj);
            var slotUI = slotObj.GetComponent<InventorySlotUI>();
            if (slotUI != null)
                slotUI.SetEmpty(i);
        }

        // 更新金币
        if (goldText != null && GameManager.Instance != null)
            goldText.text = GameManager.Instance.GetGoldString();

        // 刷新选中状态
        if (_selectedIndex >= 0)
            ShowItemDetail(_selectedIndex);
    }

    private bool PassFilter(ItemData item)
    {
        return _filterMode switch
        {
            FilterMode.All => true,
            FilterMode.Equipment => item.type == ItemType.Weapon || item.type == ItemType.Armor || item.type == ItemType.Accessory || item.type == ItemType.Tool || item.type == ItemType.DragonEquipment,
            FilterMode.Consumable => item.type == ItemType.Consumable || item.type == ItemType.Seed || item.type == ItemType.Gift,
            FilterMode.Material => item.type == ItemType.Material,
            _ => true
        };
    }

    private void OnSlotClicked(int index)
    {
        if (index < 0 || index >= InventoryManager.Instance.GetAllSlots().Count) return;
        var slot = InventoryManager.Instance.GetAllSlots()[index];
        if (string.IsNullOrEmpty(slot.itemId))
        {
            ClearDetail();
            return;
        }
        ShowItemDetail(index);
    }

    private void ShowItemDetail(int index)
    {
        _selectedIndex = index;
        var slot = InventoryManager.Instance.GetAllSlots()[index];
        _selectedItemId = slot.itemId;

        var item = Resources.Load<ItemData>($"Items/{slot.itemId}");
        if (item == null) { ClearDetail(); return; }

        if (detailPanel != null) detailPanel.SetActive(true);
        if (detailIcon != null) detailIcon.sprite = item.icon;
        if (detailName != null) detailName.text = item.itemName;
        if (detailType != null)
        {
            string typeStr = GetTypeName(item.type);
            if (item.type == ItemType.Weapon)
                typeStr += $" · {WeaponTypeStats.GetWeaponTypeName(item.weaponSubType)}";
            else if (item.type == ItemType.DragonEquipment)
                typeStr += $" · {DragonEquipmentUtils.GetSlotName(item.dragonEquipmentSubType)}";

            if (item.element.HasValue)
                typeStr += $" · {DragonElementUtils.GetElementName(item.element.Value)}元素";
            typeStr += $" · {ItemData.GetQualityName(item.quality)}";
            detailType.text = typeStr;
        }
        if (detailDescription != null) detailDescription.text = $"{item.description}\n\n持有: {slot.count}  |  单价: {item.basePrice}文";

        // 属性显示
        string stats = "";
        if (item.attackBonus > 0) stats += $"攻击 +{item.attackBonus}  ";
        if (item.defenseBonus > 0) stats += $"防御 +{item.defenseBonus}  ";
        if (item.speedBonus > 0) stats += $"速度 +{item.speedBonus}  ";
        if (item.magicBonus > 0) stats += $"灵力 +{item.magicBonus}  ";
        if (item.staminaRestore > 0) stats += $"体力 +{item.staminaRestore}  ";
        if (item.hpRestore > 0) stats += $"生命 +{item.hpRestore}  ";
        if (item.hungerRestore > 0) stats += $"饱食 +{item.hungerRestore}  ";
        if (detailStats != null) detailStats.text = stats.Trim();

        // 按钮显示
        bool isEquippable = item.type == ItemType.Weapon || item.type == ItemType.Armor || item.type == ItemType.Accessory || item.type == ItemType.Tool;
        bool isConsumable = item.type == ItemType.Consumable;

        if (useButton != null) useButton.gameObject.SetActive(isConsumable);
        if (equipButton != null) equipButton.gameObject.SetActive(isEquippable);
        if (dropButton != null) dropButton.gameObject.SetActive(true);
    }

    private void ClearDetail()
    {
        _selectedIndex = -1;
        _selectedItemId = null;
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    private void OnUseClicked()
    {
        if (string.IsNullOrEmpty(_selectedItemId)) return;
        var item = Resources.Load<ItemData>($"Items/{_selectedItemId}");
        if (item == null || item.type != ItemType.Consumable) return;

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.Eat(item.hungerRestore, item.staminaRestore);
            if (item.hpRestore > 0) PlayerStats.Instance.Heal(item.hpRestore);
        }

        InventoryManager.Instance.RemoveItem(_selectedItemId, 1);
        EventBus.Publish(GameEvent.ItemUsed, _selectedItemId);
        ClearDetail();
        Refresh();
    }

    private void OnEquipClicked()
    {
        if (string.IsNullOrEmpty(_selectedItemId)) return;
        var item = Resources.Load<ItemData>($"Items/{_selectedItemId}");
        if (item == null) return;

        // 根据物品类型自动选择槽位
        EquipmentSlotType slot;
        if (item.type == ItemType.Weapon)
        {
            // 武器按子类型路由到对应槽位
            slot = EquipmentManager.GetSlotForWeapon(item.weaponSubType);
        }
        else
        {
            slot = item.type switch
            {
                ItemType.Tool => EquipmentSlotType.Sword, // 工具→单手剑槽
                ItemType.Armor => DetermineArmorSlot(item),
                ItemType.Accessory => EquipmentSlotType.Accessory1,
                _ => EquipmentSlotType.Sword
            };
        }

        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.Equip(slot, _selectedItemId);
            ClearDetail();
            Refresh();
        }
    }

    private EquipmentSlotType DetermineArmorSlot(ItemData item)
    {
        // 根据物品名称判断防具类型（简单规则）
        string name = item.itemName;
        if (name.Contains("头盔") || name.Contains("帽子")) return EquipmentSlotType.Helmet;
        if (name.Contains("鞋") || name.Contains("靴")) return EquipmentSlotType.Boots;
        return EquipmentSlotType.Armor;
    }

    private void OnDropClicked()
    {
        if (string.IsNullOrEmpty(_selectedItemId)) return;
        InventoryManager.Instance.RemoveItem(_selectedItemId, 1);
        ClearDetail();
        Refresh();
    }

    private void SortInventory()
    {
        var slots = InventoryManager.Instance.GetAllSlots();
        // 简单排序：有物品的在前，按类型分，同类按ID排序
        slots.Sort((a, b) =>
        {
            bool aEmpty = string.IsNullOrEmpty(a.itemId);
            bool bEmpty = string.IsNullOrEmpty(b.itemId);
            if (aEmpty && bEmpty) return 0;
            if (aEmpty) return 1;
            if (bEmpty) return -1;

            var itemA = Resources.Load<ItemData>($"Items/{a.itemId}");
            var itemB = Resources.Load<ItemData>($"Items/{b.itemId}");
            int typeCmp = (itemA?.type ?? 0).CompareTo(itemB?.type ?? 0);
            if (typeCmp != 0) return typeCmp;
            return string.Compare(a.itemId, b.itemId);
        });
    }

    private string GetTypeName(ItemType type)
    {
        return type switch
        {
            ItemType.Weapon => "武器",
            ItemType.Armor => "防具",
            ItemType.Accessory => "饰品",
            ItemType.Tool => "工具",
            ItemType.Consumable => "消耗品",
            ItemType.Seed => "种子",
            ItemType.Material => "材料",
            ItemType.Gift => "礼物",
            ItemType.DragonEquipment => "龙装备",
            _ => "其他"
        };
    }
}
