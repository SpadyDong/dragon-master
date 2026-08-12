using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 装备面板 — 6 个装备槽 + 属性总览
/// </summary>
public class EquipmentPanel : MonoBehaviour
{
    [Header("装备槽")]
    [SerializeField] private EquipmentSlotUI[] equipmentSlots; // 6个槽位（Inspector 拖入）

    [Header("属性总览")]
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI playerLevelText;

    [Header("玩家预览")]
    [SerializeField] private Image playerPortrait;

    void Start()
    {
        EventBus.Subscribe(GameEvent.EquipmentChanged, Refresh);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe(GameEvent.EquipmentChanged, Refresh);
    }

    /// <summary>刷新装备面板</summary>
    public void Refresh()
    {
        if (EquipmentManager.Instance == null) return;

        // 更新 6 个装备槽
        var slots = EquipmentManager.Instance.GetAllSlots();
        if (equipmentSlots != null)
        {
            for (int i = 0; i < equipmentSlots.Length && i < slots.Length; i++)
            {
                var slotType = (EquipmentSlotType)i;
                if (equipmentSlots[i] != null)
                    equipmentSlots[i].Setup(slotType, slots[i].itemId, OnSlotClicked);
            }
        }

        // 更新属性总览
        UpdateStats();
    }

    private void UpdateStats()
    {
        if (statsText == null) return;

        var eq = EquipmentManager.Instance;
        var ps = PlayerStats.Instance;

        int atk = eq.GetAttackBonus();
        int def = eq.GetDefenseBonus();
        int baseAtk = ps != null ? ps.Strength * 2 : 5;
        int baseDef = ps != null ? ps.Vitality : 5;

        string stats = $"<b>—— 属性总览 ——</b>\n\n";
        stats += $"攻击力:  {baseAtk}  <color=#FFAA00>+{atk}</color>  =  {baseAtk + atk}\n";
        stats += $"防御力:  {baseDef}  <color=#00AAFF>+{def}</color>  =  {baseDef + def}\n";

        // 从装备中采集速度和魔法加成
        int spd = 0, mag = 0;
        foreach (var slot in eq.GetAllSlots())
        {
            if (!string.IsNullOrEmpty(slot.itemId))
            {
                var item = slot.ItemData;
                if (item != null)
                {
                    spd += item.speedBonus;
                    mag += item.magicBonus;
                }
            }
        }

        if (ps != null)
        {
            stats += $"体力上限:  {ps.MaxStamina}\n";
            stats += $"生命上限:  {ps.MaxHP}\n";
            stats += $"灵力上限:  {ps.MaxMP}  <color=#AA66FF>+{mag}</color>  =  {ps.MaxMP + mag}\n";
        }
        if (spd > 0) stats += $"移动速度:  <color=#66FF66>+{spd}</color>\n";

        statsText.text = stats;

        if (playerLevelText != null && ps != null)
        {
            playerLevelText.text = $"力{ps.Strength}  敏{ps.Agility}  体{ps.Vitality}  智{ps.Intelligence}";
        }
    }

    private void OnSlotClicked(EquipmentSlotType slotType)
    {
        // 点击有装备的槽位：卸下
        var eq = EquipmentManager.Instance;
        if (eq == null) return;

        string itemId = eq.GetEquippedItemId(slotType);
        if (!string.IsNullOrEmpty(itemId))
        {
            eq.Unequip(slotType);
        }
    }
}

/// <summary>
/// 单个装备槽 UI
/// </summary>
public class EquipmentSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI slotNameText;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private Button button;
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Color equippedColor = new(1f, 0.85f, 0.4f);
    [SerializeField] private Color emptyColor = new(0.3f, 0.3f, 0.3f);

    private EquipmentSlotType _slotType;

    public void Setup(EquipmentSlotType slotType, string equippedItemId, System.Action<EquipmentSlotType> onClick)
    {
        _slotType = slotType;

        if (slotNameText != null)
            slotNameText.text = EquipmentManager.GetSlotName(slotType);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick(_slotType));
        }

        if (string.IsNullOrEmpty(equippedItemId))
        {
            // 空槽
            if (icon != null) icon.sprite = emptySlotSprite;
            if (itemNameText != null) itemNameText.text = "空";
            if (background != null) background.color = emptyColor;
        }
        else
        {
            // 已装备
            var item = Resources.Load<ItemData>($"Items/{equippedItemId}");
            if (icon != null) icon.sprite = item?.icon;
            if (itemNameText != null) itemNameText.text = item?.itemName ?? equippedItemId;
            if (background != null) background.color = equippedColor;
        }
    }
}
