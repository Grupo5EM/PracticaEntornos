using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class Disparo : NetworkBehaviour
{
    #region Variables
    InputHandler handler;
    Transform playerTransform;
    [SerializeField] Material material;
    Player player;
    LineRenderer bulletRenderer;
    [SerializeField] GameManager gameManager;

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        handler = GetComponent<InputHandler>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        bulletRenderer = new GameObject().AddComponent<LineRenderer>();

        bulletRenderer.startWidth = .02f;
        bulletRenderer.endWidth = .02f;
        bulletRenderer.material = material;
        //ropeRenderer.sortingOrder = 3;
        bulletRenderer.enabled = false;

        player = GetComponent<Player>();
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

    #endregion

    #region Methods
    void CheckDeath(Player shotPlayer)
    {
        if (shotPlayer.vida.Value == 6)
        {
            //Cambios en el jugador que ha recibido el disparo 
            shotPlayer.deaths.Value += 1;
            shotPlayer.vida.Value = 0;
            shotPlayer.ConfigurePositions();

            //Cambios en el jugador que ha disparado 
            player.kills.Value += 1;

            //Lanzar JUGADOR 1 ha matado a JUGADOR 2
            var player1 = player.playerName.Value.ToString();
            var player2 = shotPlayer.playerName.Value.ToString();

            gameManager.ShowKillServer(player1, player2);
        }
    }

    #endregion

    #region RPC

    #region ServerRPC
    [ServerRpc]

    void ShootWeaponServerRpc(Vector2 input)
    {
        //devuelve un array con todos los gameobjects con los que ha colisionado
        var direction = (input - (Vector2)playerTransform.position).normalized;
        RaycastHit2D[] hits = Physics2D.RaycastAll(playerTransform.position, direction);
        //si el segundo gameobject es un player hace la logica

        if (hits[1].collider.gameObject.GetComponent<CapsuleCollider2D>())
        {
            //renderizado de bala
            Debug.DrawRay(playerTransform.position, direction, Color.green);
            var anchor = hits[1].centroid;
            bulletRenderer.SetPosition(1, anchor);
            //cogemos el jugador que ha sido hiteao y le baja la vida en la ui y en el player
            Player hiteao = hits[1].collider.gameObject.GetComponent<Player>();
            hiteao.vida.Value += 1;

            CheckDeath(hiteao);

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { hiteao.OwnerClientId }
                }
            };
            hiteao.UpdateLifeClientRpc(hiteao.vida.Value, clientRpcParams);

        }

        UpdateBulletClientRpc(hits[1].centroid);
        StartCoroutine(nameof(WaitingTime),.3f);
    }

    #endregion

    #region ClientRPC
    [ClientRpc]
    void UpdateBulletClientRpc(Vector2 input)
    {
        bulletRenderer.SetPosition(0, playerTransform.position);
        bulletRenderer.SetPosition(1, input);

        bulletRenderer.enabled = true;
        
        

    }

    [ClientRpc]
    void RemoveBulletClientRpc()
    {
        bulletRenderer.enabled = false;
    }

    IEnumerator WaitingTime(float time)
    {
        
        yield return new WaitForSeconds(time);

        RemoveBulletClientRpc();
    }
    #endregion

    #endregion
}
