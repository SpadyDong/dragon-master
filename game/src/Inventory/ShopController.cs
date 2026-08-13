using UnityEngine;

/// <summary>
/// 商店控制器 — 挂在商店柜台/NPC GameObject 上
/// 继承 Interactable，玩家按 F 打开商店
/// 支持：购买 / 出售 / 锻造 — GDD 6.4.4
/// </summary>
public class ShopController : Interactable
{
    [Header("商店数据")]
    [SerializeField] private ShopData shopData;

    /// <summary>当前商店数据（运行时设置）</summary>
    public ShopData Data => shopData;

    void Awake()
    {
        type = InteractType.Shop;
        promptText = $"按 F 购物";
    }

    public override void OnInteract(PlayerController player)
    {
        base.OnInteract(player);
        OpenShop();
    }

    /// <summary>打开商店</summary>
    public void OpenShop()
    {
        if (shopData == null)
        {
            Debug.LogWarning("ShopController: 未设置 ShopData");
            return;
        }

        GameManager.Instance.SetState(GameState.Menu);
        InputManager.Instance.IsInputLocked = true;
        EventBus.Publish(GameEvent.ShopOpened, shopData.shopId);

        var panel = UIManager.Instance?.GetShopPanel();
        if (panel != null)
        {
            panel.gameObject.SetActive(true);
            panel.Setup(shopData);
        }
    }

    /// <summary>关闭商店</summary>
    public void CloseShop()
    {
        var panel = UIManager.Instance?.GetShopPanel();
        if (panel != null)
            panel.gameObject.SetActive(false);

        GameManager.Instance.SetState(GameState.Playing);
        InputManager.Instance.IsInputLocked = false;
        EventBus.Publish(GameEvent.ShopClosed, shopData?.shopId ?? "");
    }

    // ==================== 购买 ====================

    /// <summary>购买物品（返回是否成功）</summary>
    public bool BuyItem(string itemId, int count = 1)
    {
        if (shopData == null) return false;

        var entry = shopData.FindEntry(itemId);
        if (entry == null || entry.Value.buyPrice <= 0) return false;

        var item = Resources.Load<ItemData>($"Items/{itemId}");
        if (item == null) return false;

        int totalPrice = ShopData.GetFinalBuyPrice(entry.Value, item) * count;
        if (!GameManager.Instance.SpendGold(totalPrice)) return false;

        int added = InventoryManager.Instance.AddItem(itemId, count);
        if (added <= 0)
        {
            // 背包满了，退钱
            GameManager.Instance.AddGold(totalPrice);
            return false;
        }

        EventBus.Publish(GameEvent.ItemBought, itemId);
        return true;
    }

    // ==================== 出售 ====================

    /// <summary>出售物品（返回是否成功）</summary>
    public bool SellItem(string itemId, int count = 1)
    {
        if (shopData == null) return false;

        var item = Resources.Load<ItemData>($"Items/{itemId}");
        if (item == null || !item.isSellable) return false;

        if (!InventoryManager.Instance.RemoveItem(itemId, count)) return false;

        int sellPrice = ShopData.GetFinalSellPrice(item) * count;
        GameManager.Instance.AddGold(sellPrice);

        EventBus.Publish(GameEvent.ItemSold, itemId);
        return true;
    }

    // ==================== 锻造 ====================

    /// <summary>检查锻造材料是否足够</summary>
    public bool CanForge(int recipeIndex)
    {
        if (shopData?.forgeRecipes == null || recipeIndex < 0 || recipeIndex >= shopData.forgeRecipes.Length)
            return false;

        var recipe = shopData.forgeRecipes[recipeIndex];

        // 检查金币
        if (GameManager.Instance.gold < recipe.forgeCost) return false;

        // 检查原料装备
        if (!string.IsNullOrEmpty(recipe.materialItemId) &&
            !InventoryManager.Instance.HasItem(recipe.materialItemId, 1))
            return false;

        // 检查额外材料
        if (recipe.extraMaterials != null && recipe.extraMaterialCounts != null)
        {
            for (int i = 0; i < recipe.extraMaterials.Length; i++)
            {
                if (i < recipe.extraMaterialCounts.Length &&
                    !InventoryManager.Instance.HasItem(recipe.extraMaterials[i], recipe.extraMaterialCounts[i]))
                    return false;
            }
        }

        return true;
    }

    /// <summary>执行锻造（返回是否成功）</summary>
    public bool Forge(int recipeIndex)
    {
        if (!CanForge(recipeIndex)) return false;

        var recipe = shopData.forgeRecipes[recipeIndex];

        // 扣金币
        GameManager.Instance.SpendGold(recipe.forgeCost);

        // 扣原料装备
        if (!string.IsNullOrEmpty(recipe.materialItemId))
            InventoryManager.Instance.RemoveItem(recipe.materialItemId, 1);

        // 扣额外材料
        if (recipe.extraMaterials != null && recipe.extraMaterialCounts != null)
        {
            for (int i = 0; i < recipe.extraMaterials.Length; i++)
            {
                if (i < recipe.extraMaterialCounts.Length)
                    InventoryManager.Instance.RemoveItem(recipe.extraMaterials[i], recipe.extraMaterialCounts[i]);
            }
        }

        // 添加产物
        InventoryManager.Instance.AddItem(recipe.resultItemId, 1);

        EventBus.Publish(GameEvent.EquipmentForged, recipe.resultItemId);
        return true;
    }
}
