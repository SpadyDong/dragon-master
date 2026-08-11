using UnityEngine;

/// <summary>
/// NPC 简易寻路
/// M1 版本：向目标直线移动 + TilemapCollider2D 碰撞回避
/// 后续升级为 A* 时替换此类
/// </summary>
public class SimplePathfinding : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private float avoidDistance = 0.5f;
    [SerializeField] private float avoidAngle = 45f;
    [SerializeField] private float stuckCheckInterval = 1f;

    private Rigidbody2D _rb;
    private Collider2D _selfCollider;
    private Vector2 _lastPosition;
    private float _stuckTimer;
    private bool _isStuck;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _selfCollider = GetComponent<Collider2D>();
    }

    /// <summary>获取向目标移动的方向（含碰撞回避）</summary>
    public Vector2 GetMovementDirection(Vector2 currentPos, Vector2 targetPos)
    {
        Vector2 toTarget = (targetPos - currentPos);
        float dist = toTarget.magnitude;

        if (dist < 0.1f) return Vector2.zero;

        Vector2 direction = toTarget.normalized;

        // 前方障碍检测
        if (_selfCollider != null && _rb != null)
        {
            RaycastHit2D hit = Physics2D.Raycast(currentPos, direction, avoidDistance);
            if (hit.collider != null && hit.collider != _selfCollider)
            {
                // 碰撞回避：尝试左右偏移
                Vector2 rightDir = new Vector2(direction.y, -direction.x);
                RaycastHit2D hitRight = Physics2D.Raycast(currentPos, rightDir, avoidDistance);
                Vector2 leftDir = new Vector2(-direction.y, direction.x);
                RaycastHit2D hitLeft = Physics2D.Raycast(currentPos, leftDir, avoidDistance);

                if (hitRight.collider == null)
                    return (direction + rightDir * 0.5f).normalized;
                else if (hitLeft.collider == null)
                    return (direction + leftDir * 0.5f).normalized;
                else
                    return Vector2.zero; // 卡住了
            }
        }

        // 卡住检测
        _stuckTimer += Time.deltaTime;
        if (_stuckTimer >= stuckCheckInterval)
        {
            _stuckTimer = 0f;
            if (Vector2.Distance(currentPos, _lastPosition) < 0.05f)
                _isStuck = true;
            else
                _isStuck = false;
            _lastPosition = currentPos;
        }

        // 卡住时尝试随机方向脱困
        if (_isStuck)
        {
            return new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }

        return direction;
    }

    /// <summary>是否到达目标</summary>
    public bool HasReachedTarget(Vector2 currentPos, Vector2 targetPos, float threshold = 0.2f)
    {
        return Vector2.Distance(currentPos, targetPos) < threshold;
    }

    void OnDrawGizmosSelected()
    {
        if (_rb != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(_rb.position, Vector2.up * avoidDistance);
        }
    }
}
