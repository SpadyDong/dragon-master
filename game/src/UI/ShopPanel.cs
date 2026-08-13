using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 商店标签页枚举
/// </summary>
public enum ShopTab
{
    Buy,    // 购买
    Sell,   // 出售
    Forge   // 锻造
}

/// <summary>
/// 商店面板 — 购买/出售/锻造三标签页
/// 对应 GDD 6.4.4 获取与强化
/// </summary>
public class ShopPanel : MonoBehaviour
{
    [Header("标签按钮")]
    [SerializeField] private Button buyTabButton;
    [SerializeField] private Button sellTabButton;
    [SerializeField] private Button forgeTabButton;
    [SerializeField] private Image[] tabButtonImages;
    [SerializeField] private Color tabActiveColor = new(1f, 0.85f, 0.4f);
    [SerializeField] private Color tabInactiveColor = new(0.35f, 0.3f, 0.25f);

    [Header("内容区")]
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject shopEntryPrefab;

    [Header("信息栏")]
    [SerializeField] private TextMeshProUGUI shopNameText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI detailText;

    [Header("关闭")]
    [SerializeField] private Button closeButton;

    private ShopData _shopData;
    private ShopController _shopController;
    private ShopTab _currentTab = ShopTab.Buy;
    private readonly List<GameObject> _entryObjects = new();

    void Start()
    {
        if (buyTabButton != null) buyTabButton.onClick.AddListener(() => SwitchTab(ShopTab.Buy));
        if (sellTabButton != null) sellTabButton.onClick.AddListener(() => SwitchTab(ShopTab.Sell));
        if (forgeTabButton != null) forgeTabButton.onClick.AddListener(() => SwitchTab(ShopTab.Forge));
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);

        // 订阅金币变化刷新
        EventBus.Subscribe<int>(GameEvent.PlayerGoldChanged, _ => UpdateGold());
        EventBus.Subscribe<string>(GameEvent.InventoryChanged, _ => { if (_currentTab == ShopTab.Sell || _currentTab == ShopTab.Forge) Refresh(); });

        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.PlayerGoldChanged, _ => UpdateGold());
        EventBus.Unsubscribe<string>(GameEvent.InventoryChanged, _ => { if (_currentTab == ShopTab.Sell || _currentTab == ShopTab.Forge) Refresh(); });
    }

    void Update()
    {
        if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            OnCloseClicked();
    }

    /// <summary>设置商店数据并打开</summary>
    public void Setup(ShopData data)
    {
        _shopData = data;
        _shopController = FindShopController(data);

        if (shopNameText != null)
            shopNameText.text = data.shopName;

        UpdateGold();
        SwitchTab(ShopTab.Buy);
    }

    /// <summary>切换标签页</summary>
    public void SwitchTab(ShopTab tab)
    {
        _currentTab = tab;

        // 更新标签按钮颜色
        if (tabButtonImages != null && tabButtonImages.Length >= 3)
        {
            tabButtonImages[0].color = (tab == ShopTab.Buy) ? tabActiveColor : tabInactiveColor;
            tabButtonImages[1].color = (tab == ShopTab.Sell) ? tabActiveColor : tabInactiveColor;
            tabButtonImages[2].color = (tab == ShopTab.Forge) ? tabActiveColor : tabInactiveColor;
        }

        Refresh();
    }

    /// <summary>刷新当前标签页内容</summary>
    public void Refresh()
    {
        if (_shopData == null) return;
        ClearEntries();
        UpdateGold();

        switch (_currentTab)
        {
            case ShopTab.Buy: RefreshBuyTab(); break;
            case ShopTab.Sell: RefreshSellTab(); break;
            case ShopTab.Forge: RefreshForgeTab(); break;
        }
    }

    private void RefreshBuyTab()
    {
        if (_shopData.items == null) return;

        foreach (var entry in _shopData.items)
        {
            if (entry.buyPrice <= 0) continue;

            var item = Resources.Load<ItemData>($"Items/{entry.itemId}");
            if (item == null) continue;

            CreateEntry(
                item.itemName,
                $"{ItemData.GetQualityName(item.quality)} · {GetItemElementString(item)} · {GetItemTypeName(item)}",
                ShopData.GetFinalBuyPrice(entry, item),
                "购买",
                () => OnBuyClicked(entry.itemId)
            );
        }
    }

    private void RefreshSellTab()
    {
        if (InventoryManager.Instance == null) return;

        var slots = InventoryManager.Instance.GetAllSlots();
        var seen = new HashSet<string>();

        foreach (var slot in slots)
        {
            if (string.IsNullOrEmpty(slot.itemId) || seen.Contains(slot.itemId)) continue;

            var item = Resources.Load<ItemData>($"Items/{slot.itemId}");
            if (item == null || !item.isSellable) continue;

            seen.Add(slot.itemId);
            int sellPrice = ShopData.GetFinalSellPrice(item);

            CreateEntry(
                $"{item.itemName} ×{slot.count}",
                $"{ItemData.GetQualityName(item.quality)} · {GetItemTypeName(item)}",
                sellPrice,
                "出售",
                () => OnSellClicked(slot.itemId)
            );
        }
    }

    private void RefreshForgeTab()
    {
        if (_shopData.forgeRecipes == null) return;

        for (int i = 0; i < _shopData.forgeRecipes.Length; i++)
        {
            int idx = i; // 闭包捕获
            var recipe = _shopData.forgeRecipes[i];

            string materialInfo = "";
            if (!string.IsNullOrEmpty(recipe.materialItemId))
                materialInfo += $"原料: {recipe.materialItemId}";
            if (recipe.extraMaterials != null)
            {
                for (int j = 0; j < recipe.extraMaterials.Length; j++)
                {
                    if (j < recipe.extraMaterialCounts.Length)
                        materialInfo += $" + {recipe.extraMaterials[j]}×{recipe.extraMaterialCounts[j]}";
                }
            }

            bool canForge = _shopController != null && _shopController.CanForge(idx);

            CreateEntry(
                recipe.recipeName,
                $"产物: {recipe.resultItemId}  |  {materialInfo}",
                recipe.forgeCost,
                canForge ? "锻造" : "材料不足",
                canForge ? () => OnForgeClicked(idx) : null
            );
        }
    }

    // ==================== 事件处理 ====================

    private void OnBuyClicked(string itemId)
    {
        if (_shopController != null && _shopController.BuyItem(itemId))
        {
            UpdateGold();
            if (detailText != null)
                detailText.text = $"购买成功: {itemId}";
        }
        else
        {
            if (detailText != null)
                detailText.text = "金币不足或背包已满";
        }
    }

    private void OnSellClicked(string itemId)
    {
        if (_shopController != null && _shopController.SellItem(itemId))
        {
            UpdateGold();
            if (detailText != null)
                detailText.text = $"出售成功: {itemId}";
            Refresh();
        }
    }

    private void OnForgeClicked(int recipeIndex)
    {
        if (_shopController != null && _shopController.Forge(recipeIndex))
        {
            UpdateGold();
            if (detailText != null)
                detailText.text = $"锻造成功: {_shopData.forgeRecipes[recipeIndex].resultItemId}";
            Refresh();
        }
    }

    private void OnCloseClicked()
    {
        _shopController?.CloseShop();
    }

    // ==================== 辅助 ====================

    private void CreateEntry(string name, string info, int price, string buttonText, System.Action onClick)
    {
        if (shopEntryPrefab == null || itemContainer == null) return;

        GameObject obj = Instantiate(shopEntryPrefab, itemContainer);
        _entryObjects.Add(obj);

        var nameText = obj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        var infoText = obj.transform.Find("InfoText")?.GetComponent<TextMeshProUGUI>();
        var priceText = obj.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();
        var actionBtn = obj.transform.Find("ActionButton")?.GetComponent<Button>();
        var actionText = actionBtn?.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();

        if (nameText != null) nameText.text = name;
        if (infoText != null) infoText.text = info;
        if (priceText != null) priceText.text = $"{price} 文";
        if (actionText != null) actionText.text = buttonText;

        if (actionBtn != null)
        {
            actionBtn.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                actionBtn.interactable = true;
                actionBtn.onClick.AddListener(() => onClick());
            }
            else
            {
                actionBtn.interactable = false;
            }
        }
    }

    private void ClearEntries()
    {
        foreach (var obj in _entryObjects) Destroy(obj);
        _entryObjects.Clear();
    }

    private void UpdateGold()
    {
        if (goldText != null && GameManager.Instance != null)
            goldText.text = GameManager.Instance.GetGoldString();
    }

    private ShopController FindShopController(ShopData data)
    {
        var controllers = FindObjectsByType<ShopController>(FindObjectsSortMode.None);
        foreach (var c in controllers)
        {
            if (c.Data == data) return c;
        }
        return null;
    }

    private string GetItemElementString(ItemData item)
    {
        return item.element.HasValue ? DragonElementUtils.GetElementName(item.element.Value) + "元素" : "无元素";
    }

    private string GetItemTypeName(ItemData item)
    {
        return item.type switch
        {
            ItemType.Weapon => WeaponTypeStats.GetWeaponTypeName(item.weaponSubType),
            ItemType.Armor => "防具",
            ItemType.Accessory => "饰品",
            ItemType.Tool => "工具",
            ItemType.Consumable => "消耗品",
            ItemType.DragonEquipment => DragonEquipmentUtils.GetSlotName(item.dragonEquipmentSubType),
            _ => item.type.ToString()
        };
    }
}
