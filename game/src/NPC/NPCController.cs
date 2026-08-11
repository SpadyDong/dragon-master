using UnityEngine;
using System;

/// <summary>
/// NPC 运行时控制器
/// 管理 NPC 移动、作息切换、对话触发
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class NPCController : MonoBehaviour
{
    [Header("NPC 数据")]
    public NPCDataContainer data;

    [Header("移动")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arriveThreshold = 0.2f;

    [Header("动画")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite idleDown;
    [SerializeField] private Sprite idleUp;
    [SerializeField] private Sprite idleLeft;
    [SerializeField] private Sprite idleRight;

    private Rigidbody2D _rb;
    private SimplePathfinding _pathfinding;
    private Interactable _interactable;

    private NPCScheduleEntry _currentActivity;
    private Vector2 _targetPosition;
    private int _patrolIndex;
    private bool _hasSchedule;
    private int _lastHour = -1;
    private int _facingDirection; // 0=下,1=上,2=左,3=右

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _pathfinding = GetComponent<SimplePathfinding>();

        // 自动添加 Interactable 组件
        _interactable = GetComponent<Interactable>();
        if (_interactable == null)
        {
            _interactable = gameObject.AddComponent<Interactable>();
            _interactable.type = InteractType.Talk;
            _interactable.promptText = $"按 F 与{data?.npcName ?? "NPC"}交谈";
            _interactable.interactRange = 1.5f;
        }

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        EventBus.Subscribe<int>(GameEvent.HourChanged, OnHourChanged);
        _hasSchedule = data?.defaultSchedule != null && data.defaultSchedule.Length > 0;
        RefreshActivity();
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<int>(GameEvent.HourChanged, OnHourChanged);
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        UpdateMovement();
        UpdateFacing();
    }

    private void OnHourChanged(int hour)
    {
        RefreshActivity();
    }

    /// <summary>查询当前时段的作息</summary>
    private void RefreshActivity()
    {
        if (!_hasSchedule || GameManager.Instance == null) return;

        int nowHour = GameManager.Instance.hour;
        int nowMinute = GameManager.Instance.minute;

        foreach (var entry in data.defaultSchedule)
        {
            int startTotal = entry.startHour * 60 + entry.startMinute;
            int endTotal = entry.endHour * 60 + entry.endMinute;
            int nowTotal = nowHour * 60 + nowMinute;

            if (nowTotal >= startTotal && nowTotal < endTotal)
            {
                _currentActivity = entry;
                _targetPosition = new Vector2(entry.targetX, entry.targetY);
                _patrolIndex = 0;

                if (entry.patrolX != null && entry.patrolX.Length > 0 && entry.patrolY != null)
                {
                    _targetPosition = new Vector2(entry.patrolX[0], entry.patrolY[0]);
                }
                return;
            }
        }

        // 找不到对应作息 → 默认站岗
        _currentActivity = default;
    }

    private void UpdateMovement()
    {
        if (_currentActivity.Equals(default(NPCScheduleEntry))) return;

        Vector2 currentPos = _rb.position;

        // 站岗模式
        if (!_currentActivity.isMoving)
        {
            return;
        }

        // 巡逻模式
        if (_currentActivity.patrolX != null && _currentActivity.patrolX.Length > 0)
        {
            if (_pathfinding.HasReachedTarget(currentPos, _targetPosition, arriveThreshold))
            {
                _patrolIndex = (_patrolIndex + 1) % _currentActivity.patrolX.Length;
                _targetPosition = new Vector2(
                    _currentActivity.patrolX[_patrolIndex],
                    _currentActivity.patrolY[_patrolIndex]
                );
            }
        }

        // 移动向目标
        Vector2 dir = _pathfinding != null
            ? _pathfinding.GetMovementDirection(currentPos, _targetPosition)
            : (_targetPosition - currentPos).normalized;

        if (dir.magnitude > 0.1f)
        {
            _rb.MovePosition(currentPos + dir * moveSpeed * Time.deltaTime);
        }
    }

    private void UpdateFacing()
    {
        Vector2 dir = _targetPosition - _rb.position;
        if (dir.magnitude < 0.1f) return;

        if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
        {
            _facingDirection = dir.y > 0 ? 1 : 0;
        }
        else
        {
            _facingDirection = dir.x > 0 ? 3 : 2;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = _facingDirection switch
            {
                0 => idleDown ?? spriteRenderer.sprite,
                1 => idleUp ?? spriteRenderer.sprite,
                2 => idleLeft ?? (idleRight ?? spriteRenderer.sprite),
                3 => idleRight ?? spriteRenderer.sprite,
                _ => spriteRenderer.sprite
            };
            spriteRenderer.flipX = (_facingDirection == 2 && idleLeft == null);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_targetPosition, 0.3f);
            Gizmos.DrawLine(transform.position, _targetPosition);
        }
    }
}
