using UnityEngine;

/// <summary>
/// 玩家控制器：8方向移动 + 奔跑 + 方向跟踪 + 动画参数
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("移动速度")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;

    [Header("动画")]
    [SerializeField] private Animator animator;

    [Header("交互")]
    [SerializeField] private float interactRadius = 2f;

    private Rigidbody2D _rb;
    private Vector2 _movement;
    private bool _movementLocked;

    // 动画参数哈希
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");
    private static readonly int FacingDir = Animator.StringToHash("FacingDir");

    /// <summary>玩家当前朝向（0=下,1=上,2=左,3=右）</summary>
    public int FacingDirection { get; private set; }

    /// <summary>移动是否被锁定</summary>
    public bool IsMovementLocked => _movementLocked;

    void Awake()
    {
        Instance = this;
        _rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Start()
    {
        // 订阅游戏状态变化
        EventBus.Subscribe<GameState>(GameEvent.GameStateChanged, OnGameStateChanged);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<GameState>(GameEvent.GameStateChanged, OnGameStateChanged);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Playing)
            UnlockMovement();
        else
            LockMovement();
    }

    void Update()
    {
        if (InputManager.Instance == null) return;

        // 使用 GetMoveInput 处理锁定
        _movement = InputManager.Instance.GetMoveInput();

        if (!_movementLocked && _movement.magnitude > 0.1f)
        {
            // 更新朝向
            UpdateFacingDirection(_movement);

            // 奔跑消耗体力
            if (InputManager.Instance.RunHeld && PlayerStats.Instance != null)
            {
                PlayerStats.Instance.TickRunning(Time.deltaTime);
            }
        }

        // 更新动画参数
        if (animator != null)
        {
            bool running = InputManager.Instance.RunHeld && !_movementLocked;
            animator.SetFloat(MoveX, _movement.x);
            animator.SetFloat(MoveY, _movement.y);
            animator.SetFloat(Speed, _movementLocked ? 0f : _movement.magnitude);
            animator.SetBool(IsRunning, running && _movement.magnitude > 0.1f);
            animator.SetInteger(FacingDir, FacingDirection);
        }
    }

    void FixedUpdate()
    {
        if (_movementLocked) return;

        // 体力不足时不能奔跑
        float speed = (InputManager.Instance.RunHeld && PlayerStats.Instance != null && PlayerStats.Instance.CurrentStamina > 0)
            ? runSpeed : moveSpeed;
        Vector2 targetPos = _rb.position + _movement * speed * Time.fixedDeltaTime;
        _rb.MovePosition(targetPos);
    }

    private void UpdateFacingDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
        {
            FacingDirection = dir.y > 0 ? 1 : 0; // 上=1, 下=0
        }
        else
        {
            FacingDirection = dir.x > 0 ? 3 : 2; // 右=3, 左=2
        }
    }

    /// <summary>锁定移动（对话/菜单时）</summary>
    public void LockMovement()
    {
        _movementLocked = true;
        _movement = Vector2.zero;
    }

    /// <summary>解锁移动</summary>
    public void UnlockMovement()
    {
        _movementLocked = false;
    }

    /// <summary>强制设置朝向（场景切换后出生点定位用）</summary>
    public void SetFacingDirection(int dir)
    {
        FacingDirection = Mathf.Clamp(dir, 0, 3);
    }

    /// <summary>传送到指定位置</summary>
    public void Teleport(Vector3 position)
    {
        _rb.position = position;
    }

    /// <summary>获取玩家前方一点（交互用）</summary>
    public Vector2 GetFrontPosition(float distance = 1f)
    {
        Vector2 dir = FacingDirection switch
        {
            0 => Vector2.down,
            1 => Vector2.up,
            2 => Vector2.left,
            3 => Vector2.right,
            _ => Vector2.down
        };
        return _rb.position + dir * distance;
    }
}
