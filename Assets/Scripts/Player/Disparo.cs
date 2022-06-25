using UnityEngine;
using Unity.Netcode;

public class Disparo : NetworkBehaviour
{
    InputHandler handler;
    Transform playerTransform;
    [SerializeField] Material material;
    Player thisPlayer;
    LineRenderer bulletRenderer;
    [SerializeField] GameManager gameManager;
    float espera = 1f;


    private void Awake()
    {
        handler = GetComponent<InputHandler>();
        thisPlayer = GetComponent<Player>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        bulletRenderer = gameObject.GetComponent<LineRenderer>();
        bulletRenderer.startWidth = .03f;
        bulletRenderer.endWidth = .03f;
        bulletRenderer.material = material;
        bulletRenderer.sortingOrder = 3;
        bulletRenderer.enabled = false;

        playerTransform = transform;
    }


    private void OnEnable()
    {
        handler.OnFire.AddListener(ShootWeaponServerRpc);
    }

    private void OnDisable()
    {
        handler.OnFire.RemoveListener(ShootWeaponServerRpc);
    }


    [ServerRpc]

    void ShootWeaponServerRpc(Vector2 input, int playerShooting)
    {
        var direction = (input - (Vector2)playerTransform.position).normalized;
        RaycastHit2D[] hits = Physics2D.RaycastAll(playerTransform.position, direction);

        if (hits[1].collider.gameObject.GetComponent<CapsuleCollider2D>())
        {
            Debug.DrawRay(playerTransform.position, direction, Color.green);
            var anchor = hits[1].centroid;
            bulletRenderer.SetPosition(1, anchor);

            Player hiteao = hits[1].collider.gameObject.GetComponent<Player>();
            hiteao.vida.Value += 1;

            checkDeath(hiteao);

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { hiteao.OwnerClientId }
                }
            };
            hiteao.UpdateVidaClientRpc(clientRpcParams);

        }
        UpdateBulletClientRpc(hits[1].centroid);
        while (espera >= 0)
        {
            espera -= Time.deltaTime;
        }
        espera = 1;
        //RemoveBulletClientRpc();
    }

    [ClientRpc]
    void UpdateBulletClientRpc(Vector2 bullet)
    {
        bulletRenderer.enabled = true;
        bulletRenderer.SetPosition(1, bullet);
    }

    [ClientRpc]
    void RemoveBulletClientRpc()
    {
        bulletRenderer.enabled = false;
    }


    void checkDeath(Player shotPlayer)
    {
        if (shotPlayer.vida.Value == 6)
        {
            //Cambios en el jugador que ha recibido el disparo 
            shotPlayer.deaths.Value += 1;
            shotPlayer.vida.Value = 0;
            shotPlayer.ConfigurePositions();

            //Cambios en el jugador que ha disparado 
            thisPlayer.kills.Value += 1;

            //Lanzar JUGADOR 1 ha matado a JUGADOR 2
            var player1 = thisPlayer.playerNameValue.Value.ToString();
            var player2 = shotPlayer.playerNameValue.Value.ToString();

            gameManager.showKillServer(player1, player2);
        }
    }
}
