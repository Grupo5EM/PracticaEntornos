using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using Unity.Netcode;

public class InputHandler : NetworkBehaviour
{

    #region Variables

    // https://docs.unity3d.com/Packages/com.unity.inputsystem@1.3/manual/index.html
    [SerializeField] InputAction _move;
    [SerializeField] InputAction _jump;
    [SerializeField] InputAction _hook;
    [SerializeField] InputAction _fire;
    [SerializeField] InputAction _mousePosition;

    // https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html
    public UnityEvent<Vector2> OnMove;
    public UnityEvent<Vector2> OnMoveFixedUpdate;
    public UnityEvent<Vector2> OnMousePosition;
    public UnityEvent<Vector2> OnHook;
    public UnityEvent<Vector2> OnHookRender;
    public UnityEvent OnJump;
    public UnityEvent<Vector2, int> OnFire;

    Vector2 CachedMoveInput { get; set; }

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        _move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Left", "<Keyboard>/a")
            .With("Down", "<Keyboard>/s")
            .With("Right", "<Keyboard>/d");

        _jump.AddBinding("<Keyboard>/space");
        _hook.AddBinding("<Mouse>/rightButton");
        _fire.AddBinding("<Mouse>/leftButton");
        _mousePosition.AddBinding("<Mouse>/position");
    }

    private void OnEnable()
    {
        _move.Enable();
        _jump.Enable();
        _hook.Enable();
        _fire.Enable();
        _mousePosition.Enable();
    }

    private void OnDisable()
    {
        _move.Disable();
        _jump.Disable();
        _hook.Disable();
        _fire.Disable();
        _mousePosition.Disable();
    }

    private void Update()
    {
        if (IsLocalPlayer)
        {
            CachedMoveInput = _move.ReadValue<Vector2>();
            var mousePosition = _mousePosition.ReadValue<Vector2>();

            var hookPerformed = _hook.WasPerformedThisFrame();
            var jumpPerformed = _jump.WasPerformedThisFrame();

            Move(CachedMoveInput);
            MousePosition(mousePosition);

            // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/Camera.ScreenToWorldPoint.html
            var screenPoint = Camera.main.ScreenToWorldPoint(mousePosition);
            if (hookPerformed) { Hook(screenPoint); }
            print(OwnerClientId);
            if (jumpPerformed) { Jump(); }
            if (_fire.WasPerformedThisFrame()) { Fire(screenPoint, (int)OwnerClientId); }

            HookRender(CachedMoveInput);
        }
    }

    private void FixedUpdate()
    {
        MoveFixedUpdate(CachedMoveInput);
    }

    #endregion

    #region InputSystem Related Methods

    void Move(Vector2 input)
    {
        OnMove?.Invoke(input);
    }

    void MoveFixedUpdate(Vector2 input)
    {
        OnMoveFixedUpdate?.Invoke(input);
    }

    void Jump()
    {
        OnJump?.Invoke();
    }

    void Hook(Vector2 input)
    {
        OnHook?.Invoke(input);
    }

    void HookRender(Vector2 input)
    {
        OnHookRender?.Invoke(input);
    }

    void Fire(Vector2 input, int playerID)
    {
        OnFire?.Invoke(input, playerID);
    }

    void MousePosition(Vector2 input)
    {
        OnMousePosition?.Invoke(input);
    }

    #endregion

}