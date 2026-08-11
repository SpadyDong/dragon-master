using UnityEngine;

/// <summary>
/// Y-sort 深度排序组件
/// 根据对象在世界中的 Y 坐标自动调整 SpriteRenderer.sortingOrder
/// Y 越大（屏幕上方 = 更远）→ sortingOrder 越小 → 渲染在后面
/// 
/// 挂到所有需要 2.5D 遮挡排序的 GameObject 上（玩家、树、建筑等）
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class YSortOrder : MonoBehaviour
{
    [Header("偏移")]
    [Tooltip("精灵脚底偏移，用于微调排序基准点")]
    [SerializeField] private float yOffset = 0f;

    [Header("精度")]
    [Tooltip("每世界单位对应的 sortingOrder 变化量")]
    [SerializeField] private float precision = 100f;

    private SpriteRenderer _sr;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (_sr != null)
        {
            _sr.sortingOrder = Mathf.RoundToInt((-transform.position.y + yOffset) * precision);
        }
    }
}
