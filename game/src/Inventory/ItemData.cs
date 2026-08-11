using UnityEngine;

/// <summary>
/// 物品类型
/// </summary>
public enum ItemType
{
    Consumable,     // 消耗品（食物、药水）
    Weapon,         // 武器
    Armor,          // 防具
    Seed,           // 种子
    Tool,           // 工具（锄头、洒水器等）
    Material,       // 材料（矿石、木材等）
    Currency,       // 货币
    Gift            // 礼物
}

/// <summary>
/// 物品数据 — Unity 中创建为 ScriptableObject
/// 用法：Assets → Create → Item → Item Data
/// </summary>
[CreateAssetMenu(menuName = "Item/Item Data", fileName = "NewItem")]
public class ItemData : ScriptableObject
{
    public string itemId;
    public string itemName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    public ItemType type;
    public int maxStack = 99;
    public int basePrice = 10;
    public bool isSellable = true;

    // 仅消耗品
    public int staminaRestore;
    public int hpRestore;
    public int hungerRestore;
}
