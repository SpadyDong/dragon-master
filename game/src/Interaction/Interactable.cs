using UnityEngine;

/// <summary>
/// 交互类型枚举
/// </summary>
public enum InteractType { Talk, Pickup, Door, Craft, Shop, Sign, Chest, Bed }

/// <summary>
/// 可交互组件基类 — 挂到任何可被玩家 F 键交互的 GameObject 上
/// </summary>
public class Interactable : MonoBehaviour
{
    [Header("交互设置")]
    public InteractType type = InteractType.Talk;
    public string promptText = "按 F 交谈";
    public float interactRange = 1.5f;

    [Tooltip("是否需要玩家面朝目标")]
    public bool requireFacing = true;

    [Tooltip("是否只能交互一次")]
    public bool interactOnce;

    private bool _hasInteracted;

    /// <summary>是否可以交互</summary>
    public virtual bool CanInteract(PlayerController player)
    {
        if (interactOnce && _hasInteracted) return false;

        if (requireFacing && player != null)
        {
            Vector2 toTarget = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
            Vector2 playerDir = player.FacingDirection switch
            {
                0 => Vector2.down,
                1 => Vector2.up,
                2 => Vector2.left,
                3 => Vector2.right,
                _ => Vector2.down
            };
            float dot = Vector2.Dot(toTarget, playerDir);
            if (dot < 0.5f) return false; // 不在面前
        }

        return true;
    }

    /// <summary>执行交互 — 子类重写</summary>
    public virtual void OnInteract(PlayerController player)
    {
        _hasInteracted = true;
        EventBus.Publish(GameEvent.PlayerInteracted, gameObject.name);
    }
}
