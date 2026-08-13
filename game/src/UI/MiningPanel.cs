using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 矿洞面板 — 楼层显示 + 上下楼 + 电梯 + 挖矿
/// </summary>
public class MiningPanel : MonoBehaviour
{
    [Header("矿洞信息")]
    [SerializeField] private TextMeshProUGUI mineNameText;
    [SerializeField] private TextMeshProUGUI floorText;

    [Header("操作按钮")]
    [SerializeField] private Button mineButton;
    [SerializeField] private Button descendButton;
    [SerializeField] private Button ascendButton;
    [SerializeField] private Button exitButton;

    [Header("电梯")]
    [SerializeField] private Transform elevatorContainer;
    [SerializeField] private GameObject elevatorButtonPrefab;

    [Header("日志")]
    [SerializeField] private TextMeshProUGUI logText;

    private List<GameObject> _elevatorButtons = new();

    void Start()
    {
        if (mineButton != null) mineButton.onClick.AddListener(OnMine);
        if (descendButton != null) descendButton.onClick.AddListener(OnDescend);
        if (ascendButton != null) ascendButton.onClick.AddListener(OnAscend);
        if (exitButton != null) exitButton.onClick.AddListener(OnExit);
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (MiningManager.Instance == null) return;

        if (mineNameText != null)
            mineNameText.text = MiningManager.Instance.CurrentMine?.mineName ?? "未在矿洞";
        if (floorText != null)
            floorText.text = MiningManager.Instance.IsInMine
                ? $"第 {MiningManager.Instance.CurrentFloor} 层"
                : "";

        // 刷新电梯按钮
        foreach (var obj in _elevatorButtons) Destroy(obj);
        _elevatorButtons.Clear();

        if (elevatorContainer != null && elevatorButtonPrefab != null && MiningManager.Instance.IsInMine)
        {
            foreach (var checkpoint in MiningManager.Instance.GetElevatorCheckpoints())
            {
                var btnObj = Instantiate(elevatorButtonPrefab, elevatorContainer);
                _elevatorButtons.Add(btnObj);

                var btn = btnObj.GetComponent<Button>();
                var text = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.text = $"电梯·第{checkpoint}层";

                if (btn != null)
                {
                    int floor = checkpoint;
                    btn.interactable = MiningManager.Instance.GetMaxReachedFloor(MiningManager.Instance.CurrentMine.mineId) >= floor;
                    btn.onClick.AddListener(() => MiningManager.Instance.UseElevator(floor));
                }
            }
        }
    }

    private void OnMine()
    {
        var ores = MiningManager.Instance?.Mine();
        if (logText != null)
        {
            if (ores == null || ores.Count == 0)
                logText.text = "没挖到矿石...";
            else
            {
                string names = "";
                foreach (var ore in ores) names += ore.oreName + " ";
                logText.text = $"挖到: {names}";
            }
        }
    }

    private void OnDescend()
    {
        MiningManager.Instance?.Descend();
        Refresh();
    }

    private void OnAscend()
    {
        MiningManager.Instance?.Ascend();
        Refresh();
    }

    private void OnExit()
    {
        MiningManager.Instance?.ExitMine();
        UIManager.Instance?.CloseStandalonePanels();
    }
}
