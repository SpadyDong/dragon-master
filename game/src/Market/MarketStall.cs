using UnityEngine;

/// <summary>
/// 菜市场摊位 — GDD 5.9
/// 挂在菜市场摊位/公告板 GameObject 上，玩家按 F 交互买卖
/// </summary>
public class MarketStall : Interactable
{
    [Header("摊位设置")]
    [Tooltip("摊位名称")]
    [SerializeField] private string stallName = "菜市场摊位";
    [Tooltip("是否可收购（出售给摊位）")]
    [SerializeField] private bool canSell = true;
    [Tooltip("是否可购买种子")]
    [SerializeField] private bool canBuySeed = true;

    void Awake()
    {
        type = InteractType.Shop;
        promptText = $"按 F 与{stallName}交易";
    }

    public override void OnInteract(PlayerController player)
    {
        base.OnInteract(player);
        // 打开菜市场面板
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenMarket();
        }
    }

    /// <summary>出售物品到摊位</summary>
    public int Sell(string itemId, int count = 1)
    {
        return MarketManager.Instance?.Sell(itemId, count) ?? 0;
    }

    /// <summary>从摊位购买种子</summary>
    public bool BuySeed(string itemId, int count = 1)
    {
        return MarketManager.Instance?.BuySeed(itemId, count) ?? false;
    }
}
