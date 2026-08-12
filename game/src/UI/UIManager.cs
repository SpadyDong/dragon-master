using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 主菜单标签页枚举
/// </summary>
public enum MenuTab
{
    Inventory,   // 背包
    Equipment,   // 装备
    Map,         // 地图
    Quests,      // 任务
    Relations,   // 好感度
    Settings     // 设置
}

/// <summary>
/// UI 管理器 — 统一管理所有界面面板
/// 负责标签页切换、面板开关、输入锁定
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("主菜单")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private TextMeshProUGUI[] tabLabels;

    [Header("子面板")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject questPanel;
    [SerializeField] private GameObject relationshipPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("标签按钮高亮色")]
    [SerializeField] private Color tabActiveColor = new(1f, 0.85f, 0.4f);
    [SerializeField] private Color tabInactiveColor = new(0.35f, 0.3f, 0.25f);

    private MenuTab _currentTab = MenuTab.Inventory;
    private bool _isMenuOpen;
    private GameObject[] _panels;
    private Image[] _tabButtonImages;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 初始化面板数组
        _panels = new[] { inventoryPanel, equipmentPanel, mapPanel, questPanel, relationshipPanel, settingsPanel };

        // 缓存 Tab 按钮的 Image 组件
        if (tabButtons != null)
        {
            _tabButtonImages = new Image[tabButtons.Length];
            for (int i = 0; i < tabButtons.Length; i++)
                _tabButtonImages[i] = tabButtons[i].GetComponent<Image>();
        }

        // 初始隐藏所有面板
        CloseAllPanels();
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    void Update()
    {
        // Tab 键打开/关闭主菜单
        if (Input.GetKeyDown(KeyCode.Tab) && !InputManager.Instance.IsInputLocked)
        {
            if (!_isMenuOpen) OpenMenu();
            else CloseMenu();
            return;
        }

        // Esc 关闭
        if (_isMenuOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMenu();
            return;
        }

        // 标签页快捷键 (Q/E 左右切换)
        if (_isMenuOpen)
        {
            if (Input.GetKeyDown(KeyCode.Q))
                SwitchTab((MenuTab)(((int)_currentTab - 1 + 6) % 6));
            if (Input.GetKeyDown(KeyCode.E))
                SwitchTab((MenuTab)(((int)_currentTab + 1) % 6));
        }

        // 快捷打开独立面板（菜单未开时）
        if (!_isMenuOpen && !InputManager.Instance.IsInputLocked)
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                OpenMenu();
                SwitchTab(MenuTab.Inventory);
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                OpenMenu();
                SwitchTab(MenuTab.Map);
            }
        }
    }

    /// <summary>打开主菜单</summary>
    public void OpenMenu()
    {
        _isMenuOpen = true;
        if (menuPanel != null) menuPanel.SetActive(true);
        GameManager.Instance.SetState(GameState.Menu);
        InputManager.Instance.IsInputLocked = true;
        SwitchTab(_currentTab);
    }

    /// <summary>关闭主菜单</summary>
    public void CloseMenu()
    {
        _isMenuOpen = false;
        CloseAllPanels();
        if (menuPanel != null) menuPanel.SetActive(false);
        GameManager.Instance.SetState(GameState.Playing);
        InputManager.Instance.IsInputLocked = false;
    }

    /// <summary>切换标签页</summary>
    public void SwitchTab(MenuTab tab)
    {
        _currentTab = tab;
        int idx = (int)tab;

        // 切换面板
        for (int i = 0; i < _panels.Length; i++)
        {
            if (_panels[i] != null)
                _panels[i].SetActive(i == idx);
        }

        // 更新 Tab 按钮颜色
        if (_tabButtonImages != null)
        {
            for (int i = 0; i < _tabButtonImages.Length; i++)
            {
                if (_tabButtonImages[i] != null)
                    _tabButtonImages[i].color = (i == idx) ? tabActiveColor : tabInactiveColor;
            }
        }

        // 通知面板刷新
        RefreshCurrentPanel();
    }

    /// <summary>通过整数索引切换（供 Button onClick 调用）</summary>
    public void SwitchTabByIndex(int index)
    {
        if (index >= 0 && index < 6)
            SwitchTab((MenuTab)index);
    }

    private void RefreshCurrentPanel()
    {
        switch (_currentTab)
        {
            case MenuTab.Inventory:
                var inv = inventoryPanel?.GetComponent<InventoryPanel>();
                if (inv != null) inv.Refresh();
                break;
            case MenuTab.Equipment:
                var eq = equipmentPanel?.GetComponent<EquipmentPanel>();
                if (eq != null) eq.Refresh();
                break;
            case MenuTab.Map:
                var map = mapPanel?.GetComponent<MapPanel>();
                if (map != null) map.RefreshMap();
                break;
            case MenuTab.Quests:
                var qp = questPanel?.GetComponent<QuestPanel>();
                if (qp != null) qp.Refresh();
                break;
            case MenuTab.Relations:
                var rp = relationshipPanel?.GetComponent<RelationshipPanel>();
                if (rp != null) rp.Refresh();
                break;
            case MenuTab.Settings:
                var sp = settingsPanel?.GetComponent<SettingsPanel>();
                if (sp != null) sp.Refresh();
                break;
        }
    }

    private void CloseAllPanels()
    {
        foreach (var panel in _panels)
            if (panel != null) panel.SetActive(false);
    }

    /// <summary>菜单是否打开</summary>
    public bool IsMenuOpen => _isMenuOpen;

    /// <summary>获取当前标签页</summary>
    public MenuTab CurrentTab => _currentTab;
}
