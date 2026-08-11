using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 背包 UI 面板
/// E 键打开/关闭，36 格网格
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("面板")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("网格")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("详情")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    private List<GameObject> _slotObjects = new();
    private bool _isOpen;

    void Start()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    void Update()
    {
        // E 键切换背包
        if (Input.GetKeyDown(KeyCode.E))
        {
            Toggle();
        }

        if (_isOpen && GameManager.Instance.CurrentState != GameState.Menu && inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            _isOpen = false;
        }
    }

    public void Toggle()
    {
        _isOpen = !_isOpen;
        if (inventoryPanel != null)
            inventoryPanel.SetActive(_isOpen);

        if (_isOpen)
        {
            GameManager.Instance.SetState(GameState.Menu);
            InputManager.Instance.IsInputLocked = true;
            RefreshSlots();
        }
        else
        {
            GameManager.Instance.SetState(GameState.Playing);
            InputManager.Instance.IsInputLocked = false;
        }
    }

    private void RefreshSlots()
    {
        if (InventoryManager.Instance == null) return;
        if (slotContainer == null || slotPrefab == null) return;

        // 清除旧对象
        foreach (var obj in _slotObjects)
            Destroy(obj);
        _slotObjects.Clear();

        var slots = InventoryManager.Instance.GetAllSlots();
        for (int i = 0; i < slots.Count; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            _slotObjects.Add(slotObj);

            var slotUI = slotObj.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                slotUI.Setup(slots[i], i, OnSlotClicked);
            }
        }
    }

    private void OnSlotClicked(int index)
    {
        var slots = InventoryManager.Instance.GetAllSlots();
        if (index < 0 || index >= slots.Count) return;

        var slot = slots[index];
        if (!string.IsNullOrEmpty(slot.itemId))
        {
            if (itemNameText != null)
                itemNameText.text = slot.itemId;
            if (itemDescriptionText != null)
                itemDescriptionText.text = $"数量: {slot.count}";
        }
    }
}

/// <summary>单个背包格子 UI</summary>
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button button;

    private int _index;

    public void Setup(InventoryManager.InventorySlot slot, int index, System.Action<int> onClick)
    {
        _index = index;
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick(_index));
        }
    }
}
