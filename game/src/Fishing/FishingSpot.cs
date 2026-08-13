using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 钓点控制器 — 对应 GDD 5.7.1
/// 挂到钓点位置的 GameObject 上，玩家 F 键交互触发钓鱼
/// </summary>
[RequireComponent(typeof(Interactable))]
public class FishingSpot : MonoBehaviour
{
    [Header("钓点设置")]
    [SerializeField] private FishLocation location = FishLocation.Lake;
    [SerializeField] private int spotId = -1;
    [SerializeField] private float interactRange = 2f;

    [Header("视觉")]
    [SerializeField] private GameObject rippleEffect;

    private Interactable _interactable;
    private FishingQTE _activeQTE;
    private List<FishData> _availableFish = new();
    private bool _isFishing;

    /// <summary>当前是否正在钓鱼</summary>
    public bool IsFishing => _isFishing;
    public FishLocation Location => location;
    public int SpotId => spotId;
    public List<FishData> AvailableFish => _availableFish;

    void Awake()
    {
        _interactable = GetComponent<Interactable>();
        if (_interactable == null)
        {
            _interactable = gameObject.AddComponent<Interactable>();
            _interactable.type = InteractType.Talk;
            _interactable.promptText = "按 F 钓鱼";
            _interactable.interactRange = interactRange;
        }
    }

    void Start()
    {
        RefreshAvailableFish();
    }

    /// <summary>刷新可用鱼种（由 FishingManager 每日调用）</summary>
    public void RefreshAvailableFish()
    {
        _availableFish.Clear();

        if (FishingManager.Instance == null) return;

        // 获取该钓点类型的所有鱼种
        FishData[] allFish = FishingManager.Instance.GetAllFishData();
        int season = GameManager.Instance != null ? GameManager.Instance.season : 0;
        int weather = GameManager.Instance != null ? GameManager.Instance.weatherIndex : 0;
        int timeSlot = GetCurrentTimeSlot();

        foreach (var fish in allFish)
        {
            if (fish.location != location) continue;
            if (!fish.IsValidSeason(season)) continue;
            if (!fish.IsValidTimeSlot(timeSlot)) continue;
            if (!fish.IsValidWeather(weather)) continue;

            _availableFish.Add(fish);
        }
    }

    /// <summary>开始钓鱼</summary>
    public bool StartFishing(PlayerController player)
    {
        if (_isFishing) return false;
        if (_availableFish.Count == 0)
        {
            Debug.Log("这里没有鱼可钓...");
            return false;
        }

        // 天气检查：大风时海边禁止钓鱼（安全）
        int weather = GameManager.Instance != null ? GameManager.Instance.weatherIndex : 0;
        if (location == FishLocation.Sea && weather == 5)
        {
            Debug.Log("风浪太大，海边禁止钓鱼！");
            return false;
        }

        // 雪山钓点需要冬季
        if (location == FishLocation.SnowMountain && GameManager.Instance.season != 3)
        {
            Debug.Log("冰面未结冰，无法在此钓鱼。");
            return false;
        }

        // 火山钓点需要防火药水（预留检查）
        if (location == FishLocation.Volcano)
        {
            // TODO: 检查背包是否有防火药水 buff
        }

        // 消耗体力
        if (PlayerStats.Instance != null && !PlayerStats.Instance.ConsumeStamina(5))
        {
            Debug.Log("体力不足，无法钓鱼。");
            return false;
        }

        _isFishing = true;

        // 创建 QTE 实例
        GameObject qteObj = new GameObject("FishingQTE");
        _activeQTE = qteObj.AddComponent<FishingQTE>();
        _activeQTE.Initialize(this, player);

        EventBus.Publish(GameEvent.FishingStarted, spotId);
        return true;
    }

    /// <summary>钓鱼结束（由 FishingQTE 调用）</summary>
    public void EndFishing(bool success, FishData caughtFish)
    {
        _isFishing = false;

        if (_activeQTE != null)
        {
            Destroy(_activeQTE.gameObject);
            _activeQTE = null;
        }

        if (success && caughtFish != null)
        {
            // 添加到背包
            InventoryManager.Instance?.AddItem(caughtFish.fishId, 1);

            // 通知 FishingManager 记录
            FishingManager.Instance?.OnFishCaught(caughtFish);

            EventBus.Publish(GameEvent.FishCaught, caughtFish.fishId);
        }

        EventBus.Publish(GameEvent.FishingEnded, success);
    }

    /// <summary>获取当前时段索引</summary>
    private int GetCurrentTimeSlot()
    {
        if (GameManager.Instance == null) return 0;
        int h = GameManager.Instance.hour;

        if (h >= 6 && h < 8) return 0;   // 清晨
        if (h >= 8 && h < 12) return 1; // 上午
        if (h >= 12 && h < 14) return 2; // 中午
        if (h >= 14 && h < 18) return 3; // 下午
        if (h >= 18 && h < 20) return 4; // 傍晚
        if (h >= 20 && h < 24) return 5; // 夜晚
        return 6;                          // 深夜
    }

    /// <summary>设置钓点ID</summary>
    public void SetSpotId(int id) => spotId = id;
}
