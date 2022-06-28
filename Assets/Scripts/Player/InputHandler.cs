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
    [SerializeField] InputAction _ready;
    [SerializeField] InputAction _showMenu;

    // https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html
    public UnityEvent<Vector2> OnMove;
    public UnityEvent<Vector2> OnMoveFixedUpdate;
    public UnityEvent<Vector2> OnMousePosition;
    public UnityEvent<Vector2> OnHook;
    public UnityEvent<Vector2> OnHookRender;
    public UnityEvent OnJump;
    public UnityEvent OnReady;
    public UnityEvent OnShowMenu;
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
        _ready.AddBinding("<Keyboard>/r");
        _showMenu.AddBinding("<Keyboard>/e");
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
        _ready.Enable();
        _showMenu.Enable();
        _mousePosition.Enable();
    }

    private void OnDisable()
    {
        _move.Disable();
        _jump.Disable();
        _hook.Disable();
        _fire.Disable();
        _ready.Disable();
        _showMenu.Disable();
        _mousePosition.Disable();
    }

    private void Update()
    {
        //Se ejecuta primero el update, antes que el Fixed Update
        if (IsLocalPlayer)
        {
            CachedMoveInput = _move.ReadValue<Vector2>();
            var mousePosition = _mousePosition.ReadValue<Vector2>();

            var hookPerformed = _hook.WasPerformedThisFrame();
            var jumpPerformed = _jump.WasPerformedThisFrame();
            var readyPerformed = _ready.WasPerformedThisFrame();
            var showMenuPerformed = _showMenu.WasPerformedThisFrame();

            Move(CachedMoveInput);
            MousePosition(mousePosition);

            // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/Camera.ScreenToWorldPoint.html
            var screenPoint = Camera.main.ScreenToWorldPoint(mousePosition);
            if (hookPerformed) { Hook(screenPoint); }          
            
          //Como la tecla espacio ha sido presionada, se ejecuta el salto y por tanto su m?odo
            if (readyPerformed) { Debug.Log("Ready ha sido performed"); Ready();  }
            if (showMenuPerformed) { Debug.Log("Showmenu ha sido performed"); ShowMenu(); }
            if (jumpPerformed) {  Jump(); }
            if (_fire.WasPerformedThisFrame()) { Fire(screenPoint, (int)OwnerClientId); }

            HookRender(CachedMoveInput);
            //Termina la ejecuci? del Update, por lo tanto pasamos al Fixed Update. En este punto, el estado todav? sigue siendo jumping
        }
    }

    private void FixedUpdate()
    {
        //Pasamos al fixed Update, que es donde calcula las f?icas. Al ejecutar este m?odo se mete dentro del UpdatePlayerPositionServer del PlayerController
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

    void Ready()
    {
        Debug.Log("Pressed ready");
        OnReady?.Invoke();
    }

    void ShowMenu()
    {
        OnShowMenu?.Invoke();
    }
    #endregion

}