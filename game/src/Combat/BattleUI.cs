using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 战斗 UI — GDD 6.5
/// 指令菜单 + 目标选择 + HP面板 + 战斗日志 + 结算
/// </summary>
public class BattleUI : MonoBehaviour
{
    [Header("单位面板")]
    [SerializeField] private Transform playerUnitContainer;
    [SerializeField] private Transform enemyUnitContainer;
    [SerializeField] private GameObject unitPanelPrefab;

    [Header("指令菜单")]
    [SerializeField] private GameObject commandMenu;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button itemButton;
    [SerializeField] private Button captureButton;
    [SerializeField] private Button defendButton;
    [SerializeField] private Button switchButton;
    [SerializeField] private Button fleeButton;

    [Header("目标选择")]
    [SerializeField] private GameObject targetPanel;
    [SerializeField] private Transform targetContainer;
    [SerializeField] private GameObject targetButtonPrefab;

    [Header("战斗日志")]
    [SerializeField] private TextMeshProUGUI battleLogText;
    [SerializeField] private ScrollRect battleLogScroll;

    [Header("回合提示")]
    [SerializeField] private TextMeshProUGUI turnText;

    [Header("结算")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;

    private List<GameObject> _playerPanels = new();
    private List<GameObject> _enemyPanels = new();
    private List<GameObject> _targetButtons = new();

    private BattleCommandType _pendingCommandType;

    void Start()
    {
        if (attackButton != null) attackButton.onClick.AddListener(() => OnCommandSelected(BattleCommandType.Attack));
        if (skillButton != null) skillButton.onClick.AddListener(() => OnCommandSelected(BattleCommandType.Skill));
        if (itemButton != null) itemButton.onClick.AddListener(() => OnCommandSelected(BattleCommandType.Item));
        if (captureButton != null) captureButton.onClick.AddListener(() => OnCommandSelected(BattleCommandType.Capture));
        if (defendButton != null) defendButton.onClick.AddListener(() => OnCommandSelected(BattleCommandType.Defend));
        if (switchButton != null) switchButton.onClick.AddListener(() => OnCommandSelected(BattleCommandType.SwitchDragon));
        if (fleeButton != null) fleeButton.onClick.AddListener(() => OnCommandSelected(BattleCommandType.Flee));

        // 订阅战斗事件
        EventBus.Subscribe<string>(GameEvent.BattleStarted, OnBattleStarted);
        EventBus.Subscribe<string>(GameEvent.BattleEnded, OnBattleEnded);
        EventBus.Subscribe<int>(GameEvent.TurnChanged, OnTurnChanged);
        EventBus.Subscribe<string>(GameEvent.UnitDamaged, OnUnitDamaged);

        gameObject.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<string>(GameEvent.BattleStarted, OnBattleStarted);
        EventBus.Unsubscribe<string>(GameEvent.BattleEnded, OnBattleEnded);
        EventBus.Unsubscribe<int>(GameEvent.TurnChanged, OnTurnChanged);
        EventBus.Unsubscribe<string>(GameEvent.UnitDamaged, OnUnitDamaged);
    }

    // ==================== 事件回调 ====================

    void OnBattleStarted(string battleType)
    {
        gameObject.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (battleLogText != null) battleLogText.text = "";
        RefreshAll();
    }

    void OnBattleEnded(string result)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            if (resultText != null)
                resultText.text = result == "victory" ? "胜利!" : "失败...";
        }
    }

    void OnTurnChanged(int turn)
    {
        if (turnText != null)
            turnText.text = $"第 {turn} 回合";
        RefreshAll();
    }

    void OnUnitDamaged(string logMessage)
    {
        AppendLog(logMessage);
    }

    // ==================== 指令选择 ====================

    void OnCommandSelected(BattleCommandType type)
    {
        _pendingCommandType = type;

        switch (type)
        {
            case BattleCommandType.Attack:
            case BattleCommandType.Skill:
            case BattleCommandType.Capture:
                ShowTargetSelection();
                break;
            case BattleCommandType.Item:
                ShowTargetSelection();
                break;
            case BattleCommandType.Defend:
                BattleManager.Instance.ExecuteCommand(
                    BattleCommand.Defend(BattleManager.Instance.CurrentUnit));
                break;
            case BattleCommandType.Flee:
                BattleManager.Instance.ExecuteCommand(
                    BattleCommand.Flee(BattleManager.Instance.CurrentUnit));
                break;
            case BattleCommandType.SwitchDragon:
                ExecuteSwitchDragon();
                break;
        }
    }

    /// <summary>显示目标选择面板</summary>
    void ShowTargetSelection()
    {
        if (targetPanel != null) targetPanel.SetActive(true);
        if (commandMenu != null) commandMenu.SetActive(false);

        // 清空旧目标按钮
        foreach (var obj in _targetButtons) Destroy(obj);
        _targetButtons.Clear();

        var enemies = BattleManager.Instance.GetAliveEnemies();
        foreach (var enemy in enemies)
        {
            var btnObj = Instantiate(targetButtonPrefab, targetContainer);
            _targetButtons.Add(btnObj);

            var btn = btnObj.GetComponent<Button>();
            var text = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = $"{enemy.DisplayName} (HP:{enemy.CurrentHP}/{enemy.MaxHP})";

            if (btn != null)
            {
                var capturedEnemy = enemy;
                btn.onClick.AddListener(() => OnTargetSelected(capturedEnemy));
            }
        }
    }

    /// <summary>目标选中</summary>
    void OnTargetSelected(CombatUnit target)
    {
        if (targetPanel != null) targetPanel.SetActive(false);
        if (commandMenu != null) commandMenu.SetActive(true);

        var attacker = BattleManager.Instance.CurrentUnit;
        switch (_pendingCommandType)
        {
            case BattleCommandType.Attack:
                BattleManager.Instance.ExecuteCommand(BattleCommand.Attack(attacker, target));
                break;
            case BattleCommandType.Skill:
                BattleManager.Instance.ExecuteCommand(BattleCommand.Skill(attacker, target, "basic_skill"));
                break;
            case BattleCommandType.Capture:
                BattleManager.Instance.ExecuteCommand(
                    BattleCommand.Capture(attacker, target, target.CaptureItemId ?? "rope_net"));
                break;
            case BattleCommandType.Item:
                // 简化：使用第一个HP恢复道具
                BattleManager.Instance.ExecuteCommand(
                    BattleCommand.UseItem(attacker, attacker, "heal_potion"));
                break;
        }
    }

    /// <summary>换龙</summary>
    void ExecuteSwitchDragon()
    {
        // 简化：切换到第一个存活的龙
        var players = BattleManager.Instance.GetAlivePlayers();
        var current = BattleManager.Instance.CurrentUnit;
        foreach (var p in players)
        {
            if (p != current && p.UnitType == CombatUnitType.Dragon)
            {
                int idx = BattleManager.Instance.PlayerUnits.IndexOf(p);
                BattleManager.Instance.ExecuteCommand(BattleCommand.SwitchDragon(current, idx));
                return;
            }
        }
        AppendLog("没有可切换的龙");
    }

    // ==================== 刷新 ====================

    void RefreshAll()
    {
        if (BattleManager.Instance == null) return;
        RefreshUnitPanels();
        RefreshCommandButtons();
    }

    void RefreshUnitPanels()
    {
        // 清空旧面板
        foreach (var obj in _playerPanels) Destroy(obj);
        foreach (var obj in _enemyPanels) Destroy(obj);
        _playerPanels.Clear();
        _enemyPanels.Clear();

        // 玩家方
        foreach (var unit in BattleManager.Instance.PlayerUnits)
        {
            var obj = Instantiate(unitPanelPrefab, playerUnitContainer);
            _playerPanels.Add(obj);
            SetupUnitPanel(obj, unit, true);
        }

        // 敌方
        foreach (var unit in BattleManager.Instance.EnemyUnits)
        {
            var obj = Instantiate(unitPanelPrefab, enemyUnitContainer);
            _enemyPanels.Add(obj);
            SetupUnitPanel(obj, unit, false);
        }
    }

    void SetupUnitPanel(GameObject obj, CombatUnit unit, bool isPlayer)
    {
        var nameText = obj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        var hpText = obj.transform.Find("HPText")?.GetComponent<TextMeshProUGUI>();
        var elementText = obj.transform.Find("ElementText")?.GetComponent<TextMeshProUGUI>();
        var hpBar = obj.transform.Find("HPBar")?.GetComponent<Image>();
        var statusText = obj.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();

        if (nameText != null)
        {
            string marker = isPlayer ? "" : "▸ ";
            nameText.text = marker + unit.DisplayName;
            if (unit == BattleManager.Instance.CurrentUnit)
                nameText.text += " ◄";
        }
        if (hpText != null)
            hpText.text = $"{unit.CurrentHP}/{unit.MaxHP}";
        if (elementText != null)
            elementText.text = DragonElementUtils.GetElementName(unit.Element);
        if (hpBar != null)
            hpBar.fillAmount = unit.HPPercent;
        if (statusText != null)
            statusText.text = GetStatusString(unit);
    }

    void RefreshCommandButtons()
    {
        if (BattleManager.Instance.CurrentUnit == null) return;

        bool isPlayerTurn = BattleManager.Instance.CurrentUnit.UnitType == CombatUnitType.Player ||
                            BattleManager.Instance.CurrentUnit.UnitType == CombatUnitType.Dragon;

        if (commandMenu != null) commandMenu.SetActive(isPlayerTurn);
        if (captureButton != null) captureButton.interactable = HasCapturableTarget();
    }

    bool HasCapturableTarget()
    {
        foreach (var enemy in BattleManager.Instance.GetAliveEnemies())
            if (enemy.IsCapturable) return true;
        return false;
    }

    string GetStatusString(CombatUnit unit)
    {
        var parts = new List<string>();
        if (unit.HasStatus(CombatStatus.Frozen)) parts.Add("冻结");
        if (unit.HasStatus(CombatStatus.Stunned)) parts.Add("眩晕");
        if (unit.HasStatus(CombatStatus.Burning)) parts.Add("燃烧");
        if (unit.HasStatus(CombatStatus.Electro)) parts.Add("感电");
        if (unit.HasStatus(CombatStatus.DefDown)) parts.Add("减防");
        if (unit.HasStatus(CombatStatus.Shielded)) parts.Add("护盾");
        if (unit.HasStatus(CombatStatus.Defending)) parts.Add("防御");
        return string.Join(" ", parts);
    }

    // ==================== 日志 ====================

    void AppendLog(string message)
    {
        if (battleLogText == null) return;
        battleLogText.text += message + "\n";

        // 滚动到底部
        if (battleLogScroll != null)
            battleLogScroll.verticalNormalizedPosition = 0f;
    }
}
