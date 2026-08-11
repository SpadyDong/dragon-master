using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 统一输入管理器（基于 Unity Input System）
/// 8方向移动 + 按钮 + 输入锁定
/// </summary>
[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private PlayerInputActions _inputActions;

    [Header("移动")]
    public Vector2 MoveDirection { get; private set; }

    [Header("按钮状态（持续）")]
    public bool RunHeld { get; private set; }

    [Header("按钮状态（单帧）")]
    public bool InteractPressed { get; private set; }
    public bool OpenMapPressed { get; private set; }
    public bool InventoryPressed { get; private set; }
    public bool MenuPressed { get; private set; }
    public bool ConfirmPressed { get; private set; }      // Space
    public bool EquipmentPressed { get; private set; }    // Q
    public bool DragonBagPressed { get; private set; }    // R

    /// <summary>输入是否被锁定（对话/菜单时屏蔽移动）</summary>
    public bool IsInputLocked { get; set; }

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

        _inputActions.Player.Move.performed += OnMove;
        _inputActions.Player.Move.canceled += OnMove;
        _inputActions.Player.Run.performed += ctx => RunHeld = true;
        _inputActions.Player.Run.canceled += ctx => RunHeld = false;
        _inputActions.Player.Interact.performed += ctx => InteractPressed = true;
        _inputActions.Player.OpenMap.performed += ctx => OpenMapPressed = true;
        _inputActions.Player.Inventory.performed += ctx => InventoryPressed = true;
        _inputActions.Player.Menu.performed += ctx => MenuPressed = true;
        _inputActions.Player.Confirm.performed += ctx => ConfirmPressed = true;
        _inputActions.Player.Equipment.performed += ctx => EquipmentPressed = true;
        _inputActions.Player.DragonBag.performed += ctx => DragonBagPressed = true;
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

    /// <summary>获取移动输入（内部处理锁定）</summary>
    public Vector2 GetMoveInput()
    {
        return IsInputLocked ? Vector2.zero : MoveDirection;
    }

    /// <summary>消费单帧按钮</summary>
    public bool ConsumeInteract()
    {
        if (IsInputLocked) return false;
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

    public bool ConsumeConfirm()
    {
        if (IsInputLocked) return false;
        bool val = ConfirmPressed;
        ConfirmPressed = false;
        return val;
    }

    public bool ConsumeEquipment()
    {
        if (IsInputLocked) return false;
        bool val = EquipmentPressed;
        EquipmentPressed = false;
        return val;
    }

    public bool ConsumeDragonBag()
    {
        if (IsInputLocked) return false;
        bool val = DragonBagPressed;
        DragonBagPressed = false;
        return val;
    }
}
