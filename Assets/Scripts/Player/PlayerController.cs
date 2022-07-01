using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(InputHandler))]
public class PlayerController : NetworkBehaviour
{

    #region Variables

    readonly float speed = 3.4f;
    readonly float jumpHeight = 6.5f;
    readonly float gravity = 1.5f;
    readonly int maxJumps = 2;

    LayerMask _layer;
    int _jumpsLeft;

    // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/ContactFilter2D.html
    ContactFilter2D filter;
    public InputHandler handler;
    Player player;
    Rigidbody2D rb;
    new CapsuleCollider2D collider;
    public Animator anim;
    SpriteRenderer spriteRenderer;

    [SerializeField] GameManager gameManager;
    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    NetworkVariable<bool> FlipSprite;

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<CapsuleCollider2D>();
        handler = GetComponent<InputHandler>();
        player = GetComponent<Player>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        FlipSprite = new NetworkVariable<bool>();
    }

    private void OnEnable()
    {
        //Suscrpciones a eventos: si se mueve, si salta, si pulsa la tecla para estar listo...
        handler.OnMove.AddListener(UpdatePlayerVisualsServerRpc);       
        handler.OnMoveFixedUpdate.AddListener(UpdatePlayerPositionServerRpc);
        handler.OnJump.AddListener(PerformJumpServerRpc);
        handler.OnReady.AddListener(SetClientReady);
        handler.OnShowMenu.AddListener(gameManager.ShowGameList);
        FlipSprite.OnValueChanged += OnFlipSpriteValueChanged;
    }

    private void OnDisable()
    {
        //Desuscripción de eventos
        handler.OnMove.RemoveListener(UpdatePlayerVisualsServerRpc);
        handler.OnJump.RemoveListener(PerformJumpServerRpc);
        handler.OnReady.RemoveListener(SetClientReady);
        handler.OnShowMenu.RemoveListener(gameManager.ShowGameList);
        handler.OnMoveFixedUpdate.RemoveListener(UpdatePlayerPositionServerRpc);

        FlipSprite.OnValueChanged -= OnFlipSpriteValueChanged;
    }

    void Start()
    {
        // Configure Rigidbody2D
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = gravity;

        // Configure LayerMask
        _layer = LayerMask.GetMask("Obstacles");

        // Configure ContactFilter2D
        filter.minNormalAngle = 45;
        filter.maxNormalAngle = 135;
        filter.useNormalAngle = true;
        filter.layerMask = _layer;
    }

    #endregion

    #region RPC

    #region ServerRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    //Actualiza las partes visuales: el animator y la orientación del sprite
    [ServerRpc]
    void UpdatePlayerVisualsServerRpc(Vector2 input)
    {
        UpdateAnimatorStateServerRpc();
        UpdateSpriteOrientation(input);
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    //Actualiza las variables del animator para que cambie de animación
    [ServerRpc]
    void UpdateAnimatorStateServerRpc()
    {
        if (IsGrounded)
        {
            anim.SetBool("isGrounded", true);
            anim.SetBool("isJumping", false);
        }
        else
        {
            anim.SetBool("isGrounded", false);

        }

    }
    
    //Indica al servidor que el jugador está listo y llama al GameManager para que gestione este suceso
    [ServerRpc]
    void SetPlayerReadyServerRpc()
    {
        player.isReady = true;
        gameManager.SetReady();
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void PerformJumpServerRpc()
    {

        //Se ejecuta cada vez que presionas la tecla de salto 
        if (player.State.Value == PlayerState.Grounded)
        {
            //Si detecta estar en el suelo, resetea el número de saltos
            _jumpsLeft = maxJumps;           
        }
       
        else if (_jumpsLeft == 0)
        {
            return;
        }        
        
        //Cambia al estado de Jumping
        player.State.Value = PlayerState.Jumping; 
        anim.SetBool("isJumping", true);
        rb.velocity = new Vector2(rb.velocity.x, jumpHeight);
        //Y le resta el número de saltos
        _jumpsLeft--;                      
    }


    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void UpdatePlayerPositionServerRpc(Vector2 input)
    {
        //Dentro del Fixed Update, comprueba las físicas y por tanto, las colisiones. Como estamos colisionando con el suelo...
        if (IsGrounded)
        {                   
            //...actualiza la variable y  a Grounded
            player.State.Value = PlayerState.Grounded;
 
            if (_jumpsLeft == 0)
            {
                _jumpsLeft = maxJumps;
            }
           
        }

        //Si no está ni enganchado ni tocando el suelo...
        if (!IsGrounded && player.State.Value != PlayerState.Hooked)
        {
            //...eso significa que está saltando
            player.State.Value = PlayerState.Jumping;
        }
        //Si no está enganchado, usa las físicas "normales"
        if ((player.State.Value != PlayerState.Hooked))
        {
            
            rb.velocity = new Vector2(input.x * speed, rb.velocity.y);
        }
    }

    #endregion

    #endregion

    #region Methods

    //Cambia la el sprite del jugador según la orientación de este
    void UpdateSpriteOrientation(Vector2 input)
    {
        if (input.x < 0)
        {
            FlipSprite.Value = false;
        }
        else if (input.x > 0)
        {
            FlipSprite.Value = true;
        }
    }

    //Método para el cambio de la NetworkVariable
    void OnFlipSpriteValueChanged(bool previous, bool current)
    {
        spriteRenderer.flipX = current;
    }

    //Comprueba si el jugador está tocando el suelo con el ayuda del collider del jugador y la layer del suelo
    bool IsGrounded => collider.IsTouching(filter);

    //Este método hace lo necesario para que el cliente se ponga en modo listo y luego llama al Servidor para que se muestre como que está listo y lance todos los métodos relacionados
    void SetClientReady()
    {
        //Si la partida no ha empezado, es decir, si estamos en el modo de calentamiento...
        if (gameManager.matchStarted.Value != true)
        {
            //Cambiamos los textos en la interfaz para indicar que estamos listos
            gameManager.SetReadyText();
            //Llamamos al servidor para que gestione el estar listo
            SetPlayerReadyServerRpc();
        }
        
    }
    #endregion

}
