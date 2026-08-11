using UnityEngine;

/// <summary>
/// 玩家控制器：8 方向移动 + 奔跑 + 动画
/// 挂到 Player GameObject 上
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

    private Rigidbody2D _rb;
    private Vector2 _movement;
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");

    void Awake()
    {
        Instance = this;
        _rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (InputManager.Instance == null) return;

        _movement = InputManager.Instance.MoveDirection;

        bool running = InputManager.Instance.RunHeld;

        // 更新动画参数
        if (animator != null)
        {
            animator.SetFloat(MoveX, _movement.x);
            animator.SetFloat(MoveY, _movement.y);
            animator.SetFloat(Speed, _movement.magnitude);
            animator.SetBool(IsRunning, running && _movement.magnitude > 0.1f);
        }
    }

    void FixedUpdate()
    {
        float speed = InputManager.Instance.RunHeld ? runSpeed : moveSpeed;
        Vector2 targetPos = _rb.position + _movement * speed * Time.fixedDeltaTime;
        _rb.MovePosition(targetPos);
    }

    /// <summary>
    /// 传送到指定位置
    /// </summary>
    public void Teleport(Vector3 position)
    {
        _rb.position = position;
    }
}
