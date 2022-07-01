using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.Netcode;
using UnityEngine.UI;
using System;
using Unity.Collections;

public class Player : NetworkBehaviour
{
    #region Variables

    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    [Header ("Network Variables")]
    public NetworkVariable<PlayerState> State;
    public NetworkVariable<int> vida;
    public NetworkVariable<int> idSkin;
    public NetworkVariable<FixedString64Bytes> playerName;
    public NetworkVariable<int> kills;
    public NetworkVariable<int> deaths;
    public NetworkVariable<int> ping;
    

    [Header ("Player Properties")]
    [SerializeField] public ulong playerID;
    Animator playerAnimator;
    [SerializeField] List<RuntimeAnimatorController> listSkins;
    public Text playerNameText;
    public bool isReady = false;
    public List<Transform> startPositions;

    [Header ("Instances/Dependencies")]
    [SerializeField] private UIManager lifeUI;
    [SerializeField] GameManager gameManager;

    #endregion

    #region Unity Event Functions

    private void Awake()
    {

        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        playerAnimator = GetComponent<Animator>();
        NetworkManager.OnClientConnectedCallback += ConfigurePlayer;
        
        State = new NetworkVariable<PlayerState>();
        
        vida = new NetworkVariable<int>(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
        

        idSkin = new NetworkVariable<int>();

        playerName = new NetworkVariable<FixedString64Bytes>();

        kills = new NetworkVariable<int>(0);
        deaths = new NetworkVariable<int>(0);
        ping = new NetworkVariable<int>(0);

        lifeUI = GameObject.Find("UIManager").GetComponent<UIManager>();

    }

    

    private void OnEnable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        //Suscripciones a cambios en las NetworkVariables
        State.OnValueChanged += OnPlayerStateValueChanged;
        vida.OnValueChanged += OnPlayerLifeValueChanged;

        idSkin.OnValueChanged += OnIDSkinValueChanged;

        kills.OnValueChanged += OnKillsValueChanged;
        deaths.OnValueChanged += OnDeathsValueChanged;
        ping.OnValueChanged += OnPingValueChanged;

    }

    private void OnDisable()
      {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        State.OnValueChanged -= OnPlayerStateValueChanged;
        vida.OnValueChanged -= OnPlayerLifeValueChanged;

        idSkin.OnValueChanged = OnIDSkinValueChanged;

        kills.OnValueChanged -= OnKillsValueChanged;
        deaths.OnValueChanged -= OnDeathsValueChanged;
        ping.OnValueChanged -= OnPingValueChanged;
    }

    #endregion

    #region Config Methods

    //Este método se llama cuando el cliente se conecta y configura todo lo que necesita el jugador
    public void ConfigurePlayer(ulong clientID)
    {
        //Si el jugador es local player
        if (IsLocalPlayer)
        {
            //Indicamos que es el jugador de este cliente en el game manager
            gameManager.clientPlayer = this;
            //Configura las variables iniciales del jugador
            ConfigureInitialPlayerState();
            //Configura la cámara, los controles y la posición inicial (spawn)
            ConfigureCamera();
            ConfigurePositions();
            ConfigureControls();
            //Asigna el ID del cliente al jugador
            playerID = clientID;

        } else
        {
            //Si no es el jugador local, asigna el nombre que tiene y la skin que ha seleccionado
            playerNameText.text = playerName.Value.ToString();
            playerAnimator.runtimeAnimatorController = listSkins[idSkin.Value];
        }

    }

    //Configura los valores iniciales del jugador
    void ConfigureInitialPlayerState()
    {
        //Configura la skin y el nombre elegidos
        ConfigureSkin();
        ConfigureName();
        //Pone por defecto el estado Grounded
        UpdatePlayerStateServerRpc(PlayerState.Grounded);
        //Configura la vida y la UI con sus valores iniciales
        vida.Value = 0;
        lifeUI.UpdateLifeUI(vida.Value);
    }

    //Configura la cámara
    void ConfigureCamera()
    {
        // https://docs.unity3d.com/Packages/com.unity.cinemachine@2.6/manual/CinemachineBrainProperties.html
        var virtualCam = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera;

        virtualCam.LookAt = transform;
        virtualCam.Follow = transform;
    }

    //Activa los controles
    void ConfigureControls()
    {
        GetComponent<InputHandler>().enabled = true;
    }

    //Configura el spawn. Elige aleatoriamente una de las 6 posiciones. Sirve también como respawn
    public void ConfigurePositions()
    {
        int nextPosition = UnityEngine.Random.Range(0, startPositions.Count);  
        this.transform.position = startPositions[nextPosition].position;
    }

    //Según la skin que se haya elegido en el menú de personalización, se asigna al jugador
    void ConfigureSkin()
    {
        //Coge la skin elegida que se ha guardado en el GameManager
        var skinID = gameManager.CheckSkin();
        //La asigna en el cliente
        playerAnimator.runtimeAnimatorController = listSkins[skinID];
        //Y actualiza los valores en el servidor
        ConfigureSkinServerRpc(skinID);
    }

    //Según el nombre que se haya elegido en el menú de personalización, se asigna al jugador
    void ConfigureName()
    {
        //Funciona de la misma manera que el ConfigureSkin, pero con textos
        string newName = gameManager.CheckName().text;
        playerNameText.text = newName;
        ConfigureNameServerRpc(newName);
    }


    //Si todos los jugadores están ready, se llama a este para que cada jugador cambie la posición y resetea los valores
    public void StartMatchPlayer()
    {
        ConfigurePositions();
        ResetValues();
    }

    //Para cuando acabe la ronda
    public void StartRoundPlayer()
    {
        ConfigurePositions();
    }


    //Resetea los valores a cero, se usa para cuando empieza la partida después del calentamiento
    void ResetValues()
    {
        vida.Value = 0;

        kills.Value = 0;
        deaths.Value = 0;

    }

    #endregion

    #region RPC

    #region ServerRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState state)
    {
        State.Value = state;
    }

    [ServerRpc]
    public void UpdatePlayerLifeServerRpc(int vida)
    {

        this.vida.Value = vida;
    }

 
    //Actualiza los valores de la skin en el servidor y luego lo retransmite a los demás clientes
    [ServerRpc]
    public void ConfigureSkinServerRpc(int skinID)
    {
        idSkin.Value = skinID;
        playerAnimator.runtimeAnimatorController = listSkins[idSkin.Value];
        ConfigureSkinClientRpc(skinID);
    }

    //Igual que el ConfigureSkinServer pero con los nombres
    [ServerRpc]
    public void ConfigureNameServerRpc(string clientName)
    {
        playerName.Value = clientName;
        playerNameText.text = playerName.Value.ToString();
        ConfigureNameClientRpc(clientName);
    }

    #endregion


    #region ClientRPC

    //Actualiza una la UI de las vidas para un cliente en específico
    [ClientRpc]
    public void UpdateLifeClientRpc(int vidaServer, ClientRpcParams clientRpcParams = default)
    {
        this.lifeUI.UpdateLifeUI(vidaServer);
    }
    //Actualiza la skin en todos los clientes
    [ClientRpc]
    public void ConfigureSkinClientRpc(int skinID)
    {
        playerAnimator.runtimeAnimatorController = listSkins[skinID];
    }
    //Actualiza el nombre en todos los clientes
    [ClientRpc]
    public void ConfigureNameClientRpc(string serverName)
    {
        playerNameText.text = serverName;
    }
    #endregion

    #endregion

    #region Netcode Related Methods
    //Métodos de Netcode para la actualización de los valores de las NetworkVariables

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    void OnPlayerStateValueChanged(PlayerState previous, PlayerState current)
    {
        Debug.Log("Previo: " + previous + ", Actual: " + current);
        State.Value = current;
    }

    void OnPlayerLifeValueChanged(int previous, int current)
    {
        vida.Value = current;
    }


    void OnIDSkinValueChanged(int previous, int current)
    {
        idSkin.Value = current;
    }
    

    void OnKillsValueChanged(int previous, int current)
    {
        kills.Value = current;
    }

    void OnDeathsValueChanged(int previous, int current)
    {
        deaths.Value = current;
    }

    void OnPingValueChanged(int previous, int current)
    {
        ping.Value = current;
    }
    #endregion
}


public enum PlayerState
    {
        Grounded = 0,
        Jumping = 1,
        Hooked = 2
    }


