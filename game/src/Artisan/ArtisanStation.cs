using UnityEngine;

/// <summary>
/// 工匠设备交互点 — GDD 5.2.4
/// 挂在工匠设备 GameObject 上，玩家按 F 交互开始加工/收取成品
/// </summary>
public class ArtisanStation : Interactable
{
    [Header("设备数据")]
    [SerializeField] private ArtisanDeviceData deviceData;

    void Awake()
    {
        type = InteractType.Craft;
        promptText = $"按 F 使用{deviceData?.deviceName ?? "工匠设备"}";
    }

    public override void OnInteract(PlayerController player)
    {
        base.OnInteract(player);
        if (deviceData == null)
        {
            Debug.LogWarning("ArtisanStation: 未设置设备数据");
            return;
        }

        // 打开工匠设备面板并传入设备数据
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenArtisan(deviceData);
        }
    }

    /// <summary>开始加工（recipeIndex 对应设备配方索引）</summary>
    public bool StartProcess(int recipeIndex, MarketQuality inputQuality = MarketQuality.Normal)
    {
        return ArtisanManager.Instance?.StartProcess(deviceData, recipeIndex, inputQuality) ?? false;
    }

    /// <summary>收取成品（jobIndex 对应全局任务索引）</summary>
    public bool Collect(int jobIndex, out string outputItemId, out int outputCount)
    {
        if (ArtisanManager.Instance != null)
            return ArtisanManager.Instance.CollectJob(jobIndex, out outputItemId, out outputCount);
        outputItemId = null;
        outputCount = 0;
        return false;
    }

    /// <summary>设备数据</summary>
    public ArtisanDeviceData DeviceData => deviceData;
}
