using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 传送面板 — 矿车/回程法杖/方尖碑/金色时钟状态
/// </summary>
public class TransportPanel : MonoBehaviour
{
    [Header("状态信息")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("回程法杖")]
    [SerializeField] private Button returnWandButton;
    [SerializeField] private TextMeshProUGUI returnWandButtonText;

    [Header("关闭")]
    [SerializeField] private Button closeButton;

    void Start()
    {
        if (returnWandButton != null) returnWandButton.onClick.AddListener(OnUseReturnWand);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
    }

    void OnEnable()
    {
        Refresh();
    }

    void Update()
    {
        // 回程法杖冷却实时更新
        if (returnWandButtonText != null && TransportManager.Instance != null)
        {
            if (TransportManager.Instance.HasReturnWand)
                returnWandButtonText.text = TransportManager.Instance.IsReturnWandOnCooldown ? "冷却中..." : "使用回程法杖";
        }
    }

    public void Refresh()
    {
        if (TransportManager.Instance == null) return;

        if (statusText != null)
        {
            string s = "";
            s += $"回程法杖: {(TransportManager.Instance.HasReturnWand ? "已拥有" : "未获得")}\n";
            s += $"金色时钟: {(TransportManager.Instance.HasGoldenClock ? "已建造" : "未建造")}\n";
            s += $"已解锁矿车: {CountUnlockedMinecarts()} 个\n";
            s += $"已建方尖碑: {CountBuiltObelisks()} 个";
            statusText.text = s;
        }

        if (returnWandButton != null)
            returnWandButton.interactable = TransportManager.Instance.HasReturnWand;
    }

    private int CountUnlockedMinecarts()
    {
        // 简化：通过 GetSaveData 统计（后续可暴露专门查询）
        return TransportManager.Instance.GetSaveData().unlockedMinecarts.Count;
    }

    private int CountBuiltObelisks()
    {
        return TransportManager.Instance.GetSaveData().builtObelisks.Count;
    }

    private void OnUseReturnWand()
    {
        TransportManager.Instance?.UseReturnWand();
        Refresh();
    }

    private void OnCloseClicked()
    {
        UIManager.Instance?.CloseStandalonePanels();
    }
}
