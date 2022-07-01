using UnityEngine;
using Unity.Netcode;

public class GrapplingHook : NetworkBehaviour
{
    #region Variables

    InputHandler handler;
    // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/DistanceJoint2D.html
    DistanceJoint2D rope;
    // // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/LineRenderer.html
    LineRenderer ropeRenderer;
    Transform playerTransform;
    [SerializeField] Material material;
    // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/LayerMask.html
    LayerMask layer;

    Player player;

    readonly float climbSpeed = 2f;
    readonly float swingForce = 80f;

    Rigidbody2D rb;

    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    NetworkVariable<float> ropeDistance;

    #endregion

    #region Unity Event Functions

    void Awake()
    {
        handler = GetComponent<InputHandler>();
        player = GetComponent<Player>();

        //Configure Rope Renderer
        ropeRenderer = gameObject.AddComponent<LineRenderer>();
        ropeRenderer.startWidth = .05f;
        ropeRenderer.endWidth = .05f;
        ropeRenderer.material = material;
        ropeRenderer.sortingOrder = 3;
        ropeRenderer.enabled = false;

        // Configure Rope
        rope = gameObject.AddComponent<DistanceJoint2D>();
        rope.enableCollision = true;
        rope.enabled = false;



        playerTransform = transform;
        layer = LayerMask.GetMask("Obstacles");

        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<Player>();

        ropeDistance = new NetworkVariable<float>();
    }

    private void OnEnable()
    {
        //Suscripción de eventos: engancharse, mostrar el gancho, saltos...
        handler.OnHookRender.AddListener(UpdateHookServerRpc);
        handler.OnMoveFixedUpdate.AddListener(SwingRopeServerRpc);
        handler.OnJump.AddListener(JumpPerformedServerRpc);
        handler.OnHook.AddListener(LaunchHookServerRpc);
        ropeDistance.OnValueChanged += OnRopeDistanceValueChanged;
    }

    private void OnDisable()
    {
        //Desuscripción a eventos
        handler.OnHookRender.RemoveListener(UpdateHookServerRpc);
        handler.OnMoveFixedUpdate.RemoveListener(SwingRopeServerRpc);
        handler.OnJump.RemoveListener(JumpPerformedServerRpc);
        handler.OnHook.RemoveListener(LaunchHookServerRpc);
        ropeDistance.OnValueChanged -= OnRopeDistanceValueChanged;
    }

    #endregion

    #region Netcode RPC

    #region ServerRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    //Actualiza todo lo relacionado al gancho en el servidor
    [ServerRpc]
    void UpdateHookServerRpc(Vector2 input)
    {
        //Si el jugador está enganchado...
        if (player.State.Value == PlayerState.Hooked)
        {
            //Actualiza la distancia/largo de la cuerda
            ClimbRope(input.y);
            //Envía la información actualizada a los clientes
            UpdateRopeClientRpc();
            ropeRenderer.SetPosition(0, playerTransform.position);
        }
        else if (player.State.Value == PlayerState.Grounded)
        {
            //Si el jugador pisa el suelo, quita/desactiva la cuerda tanto en cliente como en el servidor
            RemoveRopeClientRpc();
            rope.enabled = false;
            ropeRenderer.enabled = false;
        }
    }
    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    //Método para borrar la cuerda una vez el jugador salte después de estar enganchado
    [ServerRpc]
    void JumpPerformedServerRpc()
    {
        RemoveRopeClientRpc();
        rope.enabled = false;
        ropeRenderer.enabled = false;
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    //Este método lanza el gancho en el Servidor una vez se hace clic con el ratón, activando así el gancho
    [ServerRpc]
    void LaunchHookServerRpc(Vector2 input)
    {
        //Lanza un RayCast
        var hit = Physics2D.Raycast(playerTransform.position, input - (Vector2)playerTransform.position, Mathf.Infinity, layer);

        //Si el RayCast colisiona
        if (hit.collider)
        {
            //Actualiza las posiciones de la cuerda y el enganche
            var anchor = hit.centroid;
            rope.connectedAnchor = anchor;
            ropeRenderer.SetPosition(1, anchor);
            //Actualiza el punto de enganche en los clientes
            UpdateAnchorClientRpc(hit.centroid);
            //El estado del jugador pasa a estar enganchado y se muestra la cuerda en el Server
            player.State.Value = PlayerState.Hooked;
            rope.enabled = true;
            ropeRenderer.enabled = true;
        }
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    //Método para las físicas del balanceo del gancho
    [ServerRpc]
    void SwingRopeServerRpc(Vector2 input)
    {
        if (player.State.Value == PlayerState.Hooked)
        {
            // Player 2 hook direction
            var direction = (rope.connectedAnchor - (Vector2)playerTransform.position).normalized;

            // Perpendicular direction
            var forceDirection = new Vector2(input.x * direction.y, direction.x);

            var force = forceDirection * swingForce;

            rb.AddForce(force, ForceMode2D.Force);
        }
    }


    #endregion

    #region ClientRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc
    //Actualiza el enganche de la cuerda y la renderiza
    [ClientRpc]
    void UpdateAnchorClientRpc(Vector2 anchor)
    {
        rope.connectedAnchor = anchor;
        ShowRopeClientRpc();
        ropeRenderer.SetPosition(1, anchor);
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc
    //Actualiza las posiciones de la cuerda en los clientes
    [ClientRpc]
    void UpdateRopeClientRpc()
    {
        ropeRenderer.SetPosition(0, playerTransform.position);
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc
    //Muestra la cuerda en los clientes
    [ClientRpc]
    void ShowRopeClientRpc()
    {
        rope.enabled = true;
        ropeRenderer.enabled = true;
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc
    //Desactiva la cuerda en los clientes
    [ClientRpc]
    void RemoveRopeClientRpc()
    {
        rope.enabled = false;
        ropeRenderer.enabled = false;
    }

    #endregion

    #endregion

    #region Methods

    //Este método cambia el renderizado haciendola más grande o más pequeña dependiendo si vamos arriba o abajo;
    void ClimbRope(float input)
    {
        ropeDistance.Value = (input) * climbSpeed * Time.deltaTime;
    }

    void OnRopeDistanceValueChanged(float previous, float current)
    {
        rope.distance -= current;
    }

    #endregion
}

