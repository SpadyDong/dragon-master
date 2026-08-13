using UnityEngine;

/// <summary>
/// 温泉/澡堂 — GDD 7.12
/// 交互后恢复体力/生命/灵力全满，消耗1小时
/// </summary>
public class HotSpring : Interactable
{
    [Header("温泉设置")]
    [Tooltip("泡澡消耗的游戏小时数")]
    [SerializeField] private int timeCostHours = 1;
    [Tooltip("是否仅冬季开放")]
    [SerializeField] private bool winterOnly = false;
    [Tooltip("泡澡好感提升（共浴NPC）")]
    [SerializeField] private int affectionBonus = 5;

    void Awake()
    {
        type = InteractType.Talk;
        promptText = "按 F 泡温泉（恢复全部状态）";
    }

    public override bool CanInteract(PlayerController player)
    {
        if (!base.CanInteract(player)) return false;

        // 冬季限定检查
        if (winterOnly && GameManager.Instance != null && GameManager.Instance.season != 3)
            return false;

        return true;
    }

    public override void OnInteract(PlayerController player)
    {
        base.OnInteract(player);

        // 恢复全部状态
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.RestoreFull();
            Debug.Log("泡温泉：体力/生命/灵力全满");
        }

        // 消耗时间
        if (GameManager.Instance != null && TimeManager.Instance != null)
        {
            int newHour = GameManager.Instance.hour + timeCostHours;
            if (newHour >= 24)
            {
                // 跨天
                TimeManager.Instance.SkipToNextDay();
            }
            else
            {
                GameManager.Instance.hour = newHour;
                EventBus.Publish(GameEvent.HourChanged, GameManager.Instance.hour);
            }
        }

        // 偶发共浴事件
        if (Random.value < 0.3f)
        {
            EventBus.Publish(GameEvent.NPCAffectionChanged, affectionBonus);
            Debug.Log("偶遇NPC共浴，好感提升");
        }

        EventBus.Publish(GameEvent.HotSpringUsed);
    }
}
