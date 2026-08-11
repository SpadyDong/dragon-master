using UnityEngine;

/// <summary>
/// 玩家方向动画控制器
/// 管理 4 方向 Sprite 切换（通过 Animator 参数或直接切换 Sprite）
/// 简洁版：不依赖 Animator Controller，直接通过 SpriteRenderer 切换
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimation : MonoBehaviour
{
    [Header("4 方向精灵")]
    [SerializeField] private Sprite idleDown;
    [SerializeField] private Sprite idleUp;
    [SerializeField] private Sprite idleLeft;
    [SerializeField] private Sprite idleRight;

    [Header("移动精灵（可选，不设置则用 idle + 翻转）")]
    [SerializeField] private Sprite walkDown1;
    [SerializeField] private Sprite walkDown2;
    [SerializeField] private Sprite walkUp1;
    [SerializeField] private Sprite walkUp2;
    [SerializeField] private Sprite walkLeft1;
    [SerializeField] private Sprite walkLeft2;
    [SerializeField] private Sprite walkRight1;
    [SerializeField] private Sprite walkRight2;

    [Header("设置")]
    [SerializeField] private float walkFrameInterval = 0.2f;

    private SpriteRenderer _sr;
    private PlayerController _player;
    private float _frameTimer;
    private int _walkFrame;
    private int _lastDirection;
    private bool _wasMoving;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _player = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (_player == null) return;

        int dir = _player.FacingDirection;
        bool isMoving = !_player.IsMovementLocked && InputManager.Instance != null
            && InputManager.Instance.GetMoveInput().magnitude > 0.1f;

        // 方向变化时重置
        if (dir != _lastDirection)
        {
            _walkFrame = 0;
            _frameTimer = 0f;
            _lastDirection = dir;
        }

        // 静止/移动切换时重置
        if (isMoving != _wasMoving)
        {
            _walkFrame = 0;
            _frameTimer = 0f;
            _wasMoving = isMoving;
        }

        if (isMoving)
        {
            _frameTimer += Time.deltaTime;
            if (_frameTimer >= walkFrameInterval)
            {
                _frameTimer -= walkFrameInterval;
                _walkFrame = (_walkFrame + 1) % 2;
            }

            _sr.sprite = dir switch
            {
                0 => (_walkFrame == 0 && walkDown1 != null) ? walkDown1 : (walkDown2 ?? idleDown),
                1 => (_walkFrame == 0 && walkUp1 != null) ? walkUp1 : (walkUp2 ?? idleUp),
                2 => (_walkFrame == 0 && walkLeft1 != null) ? walkLeft1 : (walkLeft2 ?? idleLeft),
                3 => (_walkFrame == 0 && walkRight1 != null) ? walkRight1 : (walkRight2 ?? idleRight),
                _ => idleDown
            };

            // 左方向翻转（当没有左向精灵时，翻转右向精灵）
            if (dir == 2)
            {
                _sr.flipX = (walkLeft1 == null);
            }
            else
            {
                _sr.flipX = false;
            }
        }
        else
        {
            _sr.sprite = dir switch
            {
                0 => idleDown,
                1 => idleUp,
                2 => idleLeft ?? idleRight,
                3 => idleRight,
                _ => idleDown
            };

            _sr.flipX = (dir == 2 && idleLeft == null);
        }
    }

    /// <summary>设置 Idle 精灵</summary>
    public void SetIdleSprites(Sprite down, Sprite up, Sprite left, Sprite right)
    {
        idleDown = down;
        idleUp = up;
        idleLeft = left;
        idleRight = right;
    }
}
