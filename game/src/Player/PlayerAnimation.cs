using UnityEngine;

/// <summary>
/// 玩家 8 方向动画控制器
/// 通过 SpriteRenderer 切换精灵，支持上下左右 + 四斜向（斜向复用主轴精灵+翻转）
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimation : MonoBehaviour
{
    [Header("8 方向 Idle 精灵（斜向可不填，自动复用主轴精灵）")]
    [SerializeField] private Sprite idleDown;      // 0
    [SerializeField] private Sprite idleUp;        // 1
    [SerializeField] private Sprite idleLeft;      // 2
    [SerializeField] private Sprite idleRight;     // 3

    [Header("移动帧（每方向2帧，斜向复用主轴帧）")]
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

        if (dir != _lastDirection) { _walkFrame = 0; _frameTimer = 0f; _lastDirection = dir; }
        if (isMoving != _wasMoving) { _walkFrame = 0; _frameTimer = 0f; _wasMoving = isMoving; }

        if (isMoving)
        {
            _frameTimer += Time.deltaTime;
            if (_frameTimer >= walkFrameInterval) { _frameTimer -= walkFrameInterval; _walkFrame = (_walkFrame + 1) % 2; }

            (_sr.sprite, _sr.flipX) = dir switch
            {
                0 => (PickWalk(walkDown1, walkDown2, idleDown), false),
                1 => (PickWalk(walkUp1, walkUp2, idleUp), false),
                2 => (PickWalk(walkLeft1, walkLeft2, idleLeft) ?? PickWalk(walkRight1, walkRight2, idleRight), walkLeft1 == null),
                3 => (PickWalk(walkRight1, walkRight2, idleRight), false),
                // 斜向：复用主轴精灵 + 翻转（左向斜向 flipX=true）
                4 => (PickWalk(walkDown1, walkDown2, idleDown), true),          // 左下 → 复用下、翻转
                5 => (PickWalk(walkDown1, walkDown2, idleDown), false),         // 右下 → 复用下
                6 => (PickWalk(walkUp1, walkUp2, idleUp), false),               // 右上 → 复用上
                7 => (PickWalk(walkUp1, walkUp2, idleUp), true),                // 左上 → 复用上、翻转
                _ => (idleDown, false)
            };
        }
        else
        {
            (_sr.sprite, _sr.flipX) = dir switch
            {
                0 => (idleDown, false),
                1 => (idleUp, false),
                2 => (idleLeft ?? idleRight, idleLeft == null),
                3 => (idleRight, false),
                4 => (idleDown, true),
                5 => (idleDown, false),
                6 => (idleUp, false),
                7 => (idleUp, true),
                _ => (idleDown, false)
            };
        }
    }

    private Sprite PickWalk(Sprite w1, Sprite w2, Sprite fallback)
    {
        if (_walkFrame == 0 && w1 != null) return w1;
        if (_walkFrame == 1 && w2 != null) return w2;
        return fallback;
    }
}
