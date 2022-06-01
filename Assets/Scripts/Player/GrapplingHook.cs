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
    LayerMask layerPlayer;
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
        layerPlayer = LayerMask.GetMask("Player");

        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<Player>();

        ropeDistance = new NetworkVariable<float>();
    }

    private void OnEnable()
    {
        handler.OnHookRender.AddListener(UpdateHookServerRpc);
        handler.OnHookRender.AddListener(UpdateBulletServerRpc);
        handler.OnMoveFixedUpdate.AddListener(SwingRopeServerRpc);
        handler.OnJump.AddListener(JumpPerformedServerRpc);
        handler.OnHook.AddListener(LaunchHookServerRpc);
        handler.OnFire.AddListener(ShootWeaponServerRpc);
        ropeDistance.OnValueChanged += OnRopeDistanceValueChanged;
    }

    private void OnDisable()
    {
        handler.OnHookRender.RemoveListener(UpdateHookServerRpc);
        handler.OnHookRender.RemoveListener(UpdateBulletServerRpc);
        handler.OnMoveFixedUpdate.RemoveListener(SwingRopeServerRpc);
        handler.OnJump.RemoveListener(JumpPerformedServerRpc);
        handler.OnHook.RemoveListener(LaunchHookServerRpc);
        handler.OnFire.RemoveListener(ShootWeaponServerRpc);
        ropeDistance.OnValueChanged -= OnRopeDistanceValueChanged;
    }

    #endregion

    #region Netcode RPC

    #region ServerRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void UpdateHookServerRpc(Vector2 input)
    {
        if (player.State.Value == PlayerState.Hooked)
        {
            ClimbRope(input.y);
            UpdateRopeClientRpc();
            ropeRenderer.SetPosition(0, playerTransform.position);

        }
        else if (player.State.Value == PlayerState.Grounded)
        {
            RemoveRopeClientRpc();
            rope.enabled = false;
            ropeRenderer.enabled = false;
        }
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void JumpPerformedServerRpc()
    {
        RemoveRopeClientRpc();
        rope.enabled = false;
        ropeRenderer.enabled = false;
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void LaunchHookServerRpc(Vector2 input)
    {
        var hit = Physics2D.Raycast(playerTransform.position, input - (Vector2)playerTransform.position, Mathf.Infinity, layer);

        if (hit.collider)
        {
            var anchor = hit.centroid;
            rope.connectedAnchor = anchor;
            ropeRenderer.SetPosition(1, anchor);
            UpdateAnchorClientRpc(hit.centroid);
            player.State.Value = PlayerState.Hooked;
            rope.enabled = true;
            ropeRenderer.enabled = true;
        }
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
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

    [ServerRpc]
    void UpdateBulletServerRpc(Vector2 input)
    {
        if (player.State.Value != PlayerState.Hooked)
        {
            UpdateBulletClientRpc();
            //WaitForSecondsRealtime w = new WaitForSecondsRealtime(1);
            //w.waitTime = 1;
            RemoveRopeClientRpc();
            rope.enabled = false;
            ropeRenderer.enabled = false;

        }
    }

    [ServerRpc]

    void ShootWeaponServerRpc(Vector2 input, int playerShooting)
    {
        var direction = (input - (Vector2)playerTransform.position).normalized;
        var hit = Physics2D.Raycast(playerTransform.position, direction, Mathf.Infinity, layerPlayer);
        
        if (hit.collider.gameObject.TryGetComponent(out Player otherPlayer) && otherPlayer.PlayerID != playerShooting)
        {
            print("playerShooting:" + playerShooting + " playerImpacting: " + otherPlayer.PlayerID);
            Debug.DrawRay(playerTransform.position, direction, Color.green);
            var anchor = hit.centroid;
            ropeRenderer.SetPosition(1, anchor);
            UpdateBulletClientRpc(hit.centroid);
            Debug.Log(hit.collider.gameObject);
            hit.collider.gameObject.SendMessage("UpdatePlayerLifeServerRpc", 1);
        }
        else
        {
            Debug.DrawRay(playerTransform.position, direction, Color.red);
            //Debug.Log("NADA");
        }
    }

    #endregion

    #region ClientRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc
    [ClientRpc]
    void UpdateAnchorClientRpc(Vector2 anchor)
    {
        rope.connectedAnchor = anchor;
        ShowRopeClientRpc();
        ropeRenderer.SetPosition(1, anchor);
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc
    [ClientRpc]
    void UpdateRopeClientRpc()
    {
        ropeRenderer.SetPosition(0, playerTransform.position);
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc
    [ClientRpc]
    void ShowRopeClientRpc()
    {
        rope.enabled = true;
        ropeRenderer.enabled = true;
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc
    [ClientRpc]
    void RemoveRopeClientRpc()
    {
        rope.enabled = false;
        ropeRenderer.enabled = false;
    }

    [ClientRpc]
    void UpdateBulletClientRpc(Vector2 anchor)
    {
        ropeRenderer.enabled = true;
        ropeRenderer.SetPosition(1, anchor);
    }

    [ClientRpc]
    void UpdateBulletClientRpc()
    {
        ropeRenderer.SetPosition(0, playerTransform.position);
    }

    [ClientRpc]
    void RemoveBulletClientRpc()
    {
        ropeRenderer.enabled = false;
    }

    #endregion

    #endregion

    #region Methods

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
