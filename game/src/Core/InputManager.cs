using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 统一输入管理器（基于 Unity Input System）
/// 读取 WASD / 手柄 / 箭头键等 8 方向输入
/// </summary>
[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private PlayerInputActions _inputActions;

    [Header("移动")]
    public Vector2 MoveDirection { get; private set; }

    [Header("按钮状态")]
    public bool RunHeld { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool OpenMapPressed { get; private set; }
    public bool InventoryPressed { get; private set; }
    public bool MenuPressed { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _inputActions = new PlayerInputActions();
    }

    void OnEnable()
    {
        _inputActions.Player.Enable();

        // 移动
        _inputActions.Player.Move.performed += OnMove;
        _inputActions.Player.Move.canceled += OnMove;

        // 奔跑
        _inputActions.Player.Run.performed += ctx => RunHeld = true;
        _inputActions.Player.Run.canceled += ctx => RunHeld = false;

        // 交互按钮（单帧检测）
        _inputActions.Player.Interact.performed += ctx => InteractPressed = true;
        _inputActions.Player.OpenMap.performed += ctx => OpenMapPressed = true;
        _inputActions.Player.Inventory.performed += ctx => InventoryPressed = true;
        _inputActions.Player.Menu.performed += ctx => MenuPressed = true;
    }

    void OnDisable()
    {
        _inputActions.Player.Move.performed -= OnMove;
        _inputActions.Player.Move.canceled -= OnMove;
        _inputActions.Player.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        MoveDirection = ctx.ReadValue<Vector2>().normalized;
    }

    /// <summary>
    /// 消费单帧按钮（调用后重置，避免同一帧多次触发）
    /// </summary>
    public bool ConsumeInteract()
    {
        bool val = InteractPressed;
        InteractPressed = false;
        return val;
    }

    public bool ConsumeOpenMap()
    {
        bool val = OpenMapPressed;
        OpenMapPressed = false;
        return val;
    }

    public bool ConsumeInventory()
    {
        bool val = InventoryPressed;
        InventoryPressed = false;
        return val;
    }

    public bool ConsumeMenu()
    {
        bool val = MenuPressed;
        MenuPressed = false;
        return val;
    }
}
