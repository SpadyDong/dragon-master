using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景名称常量
/// </summary>
public static class SceneNames
{
    public const string Town = "Town";           // 龙脊镇（和平区·主地图）
    public const string SnowMountain = "Snow";   // 雪山
    public const string Forest = "Forest";       // 密林
    public const string Volcano = "Volcano";     // 火山（预留 M8）
    public const string Desert = "Desert";       // 沙漠（预留 M8）
    public const string SkyIsland = "Sky";       // 天空浮岛（预留 M11）
}

/// <summary>
/// 场景传送门
/// 挂到地图出入口的 trigger collider 上，玩家走进即切换场景
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ScenePortal : MonoBehaviour
{
    [Header("目标场景")]
    [SerializeField] private string targetScene;

    [Header("目标出生点")]
    [SerializeField] private string spawnPointId = "default";

    [Header("入口方向（玩家必须从该方向进入才触发）")]
    [SerializeField] private bool requireDirection;
    [SerializeField] private Vector2 requiredDirection = Vector2.zero; // 归一化方向

    private bool _triggered;

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;

        // 记录当前传送门作为返回点
        SceneTransitionManager.Instance?.SetReturnPortal(gameObject.name, targetScene, spawnPointId);

        // 执行场景切换
        SceneTransitionManager.Instance?.TransitionToScene(targetScene, spawnPointId);

        // 通知事件
        string portalName = gameObject.name;
        EventBus.Publish(GameEvent.PlayerInteracted, $"portal_{portalName}");
    }
}
