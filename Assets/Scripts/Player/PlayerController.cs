using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

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
    InputHandler handler;
    Player player;
    Rigidbody2D rb;
    new CapsuleCollider2D collider;
    Animator anim;
    public RuntimeAnimatorController colorVerde;
    public RuntimeAnimatorController colorAzul;
    public RuntimeAnimatorController colorRosa;
    public RuntimeAnimatorController colorNaranja;
    SpriteRenderer spriteRenderer;
    bool isVerde;
    bool isAzul;
    bool isRosa;
    bool isNaranja;
    bool isJumping;

    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    NetworkVariable<bool> FlipSprite;

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
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
        handler.OnMove.AddListener(UpdatePlayerVisualsServerRpc);       
        handler.OnMoveFixedUpdate.AddListener(UpdatePlayerPositionServerRpc);
        handler.OnJump.AddListener(PerformJumpServerRpc);

        FlipSprite.OnValueChanged += OnFlipSpriteValueChanged;
    }

    private void OnDisable()
    {
        handler.OnMove.RemoveListener(UpdatePlayerVisualsServerRpc);
        handler.OnJump.RemoveListener(PerformJumpServerRpc);
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
    [ServerRpc]
    void UpdatePlayerVisualsServerRpc(Vector2 input)
    {
        UpdateAnimatorStateServerRpc();
        UpdateSpriteOrientation(input);
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
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



    [ServerRpc]
    void UpdateColorServerRpc()
    {
        if (isVerde)
        {
            anim.runtimeAnimatorController = colorVerde;
            player.characterColor.Value = CharachterColor.Verde;
        }
        else if (isAzul)
        {
            anim.runtimeAnimatorController = colorAzul;
            player.characterColor.Value = CharachterColor.Azul;
        } else if (isRosa)
        {
            anim.runtimeAnimatorController = colorRosa;
            player.characterColor.Value = CharachterColor.Rosa;
        } else if (isNaranja)
        {
            anim.runtimeAnimatorController = colorNaranja;
            player.characterColor.Value = CharachterColor.Naranja;
        }

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

        Debug.Log("Pre-Salto: " + _jumpsLeft + ", estado del jugador: " + player.State.Value);
        
        //Cambia al estado de Jumping
        player.State.Value = PlayerState.Jumping; 
        anim.SetBool("isJumping", true);
        rb.velocity = new Vector2(rb.velocity.x, jumpHeight);
        //Y le resta el número de saltos
        _jumpsLeft--;
        
        Debug.Log("Post-Salto: " + _jumpsLeft + ", estado del jugador: " + player.State.Value);
        
        //Ahora salta al update del InputHandler, que es donde se ejecuta este método
    }


    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void UpdatePlayerPositionServerRpc(Vector2 input)
    {
        //Dentro del Fixed Update, comprueba las físicas y por tanto, las colisiones. Como estamos colisionando con el suelo...
        if (IsGrounded)
        {                   
            //...actualiza la variable y pasa de Jumping a Grounded
            player.State.Value = PlayerState.Grounded;
            //Ahora el Jump no se vuelve a ejecutar hasta que pulsemos el salto
        }


        if ((player.State.Value != PlayerState.Hooked))
        {
            
            rb.velocity = new Vector2(input.x * speed, rb.velocity.y);
        }
    }

    #endregion

    #endregion

    #region Methods

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

    void OnFlipSpriteValueChanged(bool previous, bool current)
    {
        spriteRenderer.flipX = current;
    }

    bool IsGrounded => collider.IsTouching(filter);

    #endregion

}
