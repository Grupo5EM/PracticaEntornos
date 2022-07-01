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
        //Cuando sucede el evento OnFire (cuando se dispara, en este caso con el clic izquierdo), se lanza el ShootWeaponServerRpc. Se suscribe al evento. 
        handler.OnFire.AddListener(ShootWeaponServerRpc);
    }

    private void OnDisable()
    {
        //Se desuscribe del evento
        handler.OnFire.RemoveListener(ShootWeaponServerRpc);
    }

    #endregion

    #region Methods

    //CheckDeath es un método que comprueba si un jugador ha muerto y si lo ha hecho, ejecuta las acciones necesarias
    void CheckDeath(Player shotPlayer)
    {
        //Si la vida llega al valor 6, que son todos los corazones que se puede quitar, este muere y por tanto se ejecuta lo siguiente
        if (shotPlayer.vida.Value == 6)
        {
            //Cambios en el jugador que ha recibido el disparo: se suma una muerte, se resetea la vida al valor inicial y se respawnea (se cambia de posición) 
            shotPlayer.deaths.Value += 1;
            shotPlayer.vida.Value = 0;
            shotPlayer.ConfigurePositions();

            //Cambios en el jugador que ha disparado: se suma una baja al jugador 
            player.kills.Value += 1;

            //Lanza el mensaje JUGADOR 1 ha matado a JUGADOR 2
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
        //Recogemos la dirección de la jugador a la posición donde hemos hecho click con el ratón
        var direction = (input - (Vector2)playerTransform.position).normalized;
        //Devuelve un array con todos los gameobjects con los que ha colisionado
        RaycastHit2D[] hits = Physics2D.RaycastAll(playerTransform.position, direction);

        //Si el segundo GameObject es un player...
        if (hits[1].collider.gameObject.GetComponent<CapsuleCollider2D>())
        {
            //Renderiza la bala/laser
            Debug.DrawRay(playerTransform.position, direction, Color.green);
            var anchor = hits[1].centroid;
            bulletRenderer.SetPosition(1, anchor);
            //Cogemos el jugador que ha sido dañado y se baja la vida tanto al personaje como a la UI del Cliente correspondiente
            Player hiteao = hits[1].collider.gameObject.GetComponent<Player>();
            hiteao.vida.Value += 1;

            //Comprueba si al actualizar la vida el jugador dañado ha muerto
            CheckDeath(hiteao);

            //Comprueba el cliente específico del jugador dañado...
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { hiteao.OwnerClientId }
                }
            };
            //...para que podamos actualizar únicamente su cliente y su UI
            hiteao.UpdateLifeClientRpc(hiteao.vida.Value, clientRpcParams);

        }
        //Y hace el renderizado de la bala laser en los clientes
        UpdateBulletClientRpc(hits[1].centroid);
        StartCoroutine(nameof(WaitingTime),.3f);
    }

    #endregion

    #region ClientRPC
    //Método que renderiza la bala laser en los clientes
    [ClientRpc]
    void UpdateBulletClientRpc(Vector2 input)
    {
        bulletRenderer.SetPosition(0, playerTransform.position);
        bulletRenderer.SetPosition(1, input);

        bulletRenderer.enabled = true;
        
        
    }
    //Desactiva la bala en los clientes
    [ClientRpc]
    void RemoveBulletClientRpc()
    {
        bulletRenderer.enabled = false;
    }

    //Corrutina que se usa para esperar un tiempo específico y luego desactivar la bala para que no se vea. 
    IEnumerator WaitingTime(float time)
    {
        
        yield return new WaitForSeconds(time);

        RemoveBulletClientRpc();
    }
    #endregion

    #endregion
}
