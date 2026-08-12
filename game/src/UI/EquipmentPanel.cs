using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 装备面板 — 8 个装备槽（3武器 + 3防具 + 2饰品）+ 属性总览
/// </summary>
public class EquipmentPanel : MonoBehaviour
{
    [Header("装备槽 — 武器")]
    [SerializeField] private EquipmentSlotUI swordSlot;
    [SerializeField] private EquipmentSlotUI greatswordSlot;
    [SerializeField] private EquipmentSlotUI bowSlot;

    [Header("装备槽 — 防具/饰品")]
    [SerializeField] private EquipmentSlotUI helmetSlot;
    [SerializeField] private EquipmentSlotUI armorSlot;
    [SerializeField] private EquipmentSlotUI bootsSlot;
    [SerializeField] private EquipmentSlotUI accessory1Slot;
    [SerializeField] private EquipmentSlotUI accessory2Slot;

    [Header("当前武器高亮")]
    [SerializeField] private Image weaponHighlight;

    [Header("属性总览")]
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI playerLevelText;
    [SerializeField] private TextMeshProUGUI currentWeaponText;

    private EquipmentSlotUI[] _allSlots;

    void Start()
    {
        _allSlots = new[] { swordSlot, greatswordSlot, bowSlot, helmetSlot, armorSlot, bootsSlot, accessory1Slot, accessory2Slot };

        EventBus.Subscribe(GameEvent.EquipmentChanged, Refresh);
        EventBus.Subscribe<int>(GameEvent.WeaponSwitched, OnWeaponSwitched);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe(GameEvent.EquipmentChanged, Refresh);
        EventBus.Unsubscribe<int>(GameEvent.WeaponSwitched, OnWeaponSwitched);
    }

    /// <summary>刷新装备面板</summary>
    public void Refresh()
    {
        if (EquipmentManager.Instance == null) return;

        var slots = EquipmentManager.Instance.GetAllSlots();

        // 武器槽（0-2）
        SetupSlot(swordSlot, EquipmentSlotType.Sword, 0);
        SetupSlot(greatswordSlot, EquipmentSlotType.Greatsword, 1);
        SetupSlot(bowSlot, EquipmentSlotType.Bow, 2);

        // 防具槽（3-5）
        SetupSlot(helmetSlot, EquipmentSlotType.Helmet, 3);
        SetupSlot(armorSlot, EquipmentSlotType.Armor, 4);
        SetupSlot(bootsSlot, EquipmentSlotType.Boots, 5);

        // 饰品槽（6-7）
        SetupSlot(accessory1Slot, EquipmentSlotType.Accessory1, 6);
        SetupSlot(accessory2Slot, EquipmentSlotType.Accessory2, 7);

        // 更新当前武器高亮
        UpdateWeaponHighlight();

        // 更新属性总览
        UpdateStats();
    }

    private void SetupSlot(EquipmentSlotUI slotUI, EquipmentSlotType type, int index)
    {
        if (slotUI == null) return;
        var slots = EquipmentManager.Instance.GetAllSlots();
        string itemId = (index < slots.Length) ? slots[index].itemId : null;
        slotUI.Setup(type, itemId, OnSlotClicked);
    }

    private void UpdateWeaponHighlight()
    {
        if (weaponHighlight == null || EquipmentManager.Instance == null) return;

        int currentIdx = EquipmentManager.Instance.CurrentWeaponIndex;
        EquipmentSlotUI targetSlot = currentIdx switch
        {
            0 => swordSlot,
            1 => greatswordSlot,
            2 => bowSlot,
            _ => null
        };

        if (targetSlot != null)
        {
            weaponHighlight.rectTransform.position = targetSlot.transform.position;
            weaponHighlight.gameObject.SetActive(true);
        }

        // 更新当前武器名
        if (currentWeaponText != null)
        {
            var weapon = EquipmentManager.Instance.GetCurrentWeaponData();
            string slotName = EquipmentManager.GetSlotName((EquipmentSlotType)currentIdx);
            currentWeaponText.text = weapon != null
                ? $"当前: {slotName} — {weapon.itemName}  ATK:{weapon.attackBonus}"
                : $"当前: {slotName} — 空";
        }
    }

    private void OnWeaponSwitched(int index)
    {
        UpdateWeaponHighlight();
        UpdateStats();
    }

    private void UpdateStats()
    {
        if (statsText == null) return;

        var eq = EquipmentManager.Instance;
        var ps = PlayerStats.Instance;

        int totalAtk = eq.GetAttackBonus();
        int currentAtk = eq.GetCurrentWeaponAttack();
        int def = eq.GetDefenseBonus();

        int baseAtk = ps != null ? ps.Strength * 2 : 5;
        int baseDef = ps != null ? ps.Vitality : 5;

        string stats = $"<b>—— 属性总览 ——</b>\n\n";
        stats += $"基础攻击力:  {baseAtk}\n";
        stats += $"当前武器:  <color=#FF6600>+{currentAtk}</color>\n";
        stats += $"全部装备攻击:  <color=#FFAA00>+{totalAtk}</color>\n";
        stats += $"防御力:  {baseDef}  <color=#00AAFF>+{def}</color>  =  {baseDef + def}\n";

        // 采集速度和魔法加成
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
[System.Serializable]
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
            if (icon != null) icon.sprite = emptySlotSprite;
            if (itemNameText != null) itemNameText.text = "空";
            if (background != null) background.color = emptyColor;
        }
        else
        {
            var item = Resources.Load<ItemData>($"Items/{equippedItemId}");
            if (icon != null) icon.sprite = item?.icon;
            if (itemNameText != null) itemNameText.text = item?.itemName ?? equippedItemId;
            if (background != null) background.color = equippedColor;
        }
    }
}
