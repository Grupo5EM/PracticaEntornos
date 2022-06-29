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
    public NetworkVariable<PlayerState> State;
    public NetworkVariable<int> vida;
    public NetworkVariable<int> idSkin;
    public NetworkVariable<FixedString64Bytes> playerNameValue;
    public NetworkVariable<int> kills;
    public NetworkVariable<int> deaths;
    public NetworkVariable<int> ping;
    

    [SerializeField] public ulong playerID;   


    [SerializeField] private UIManager uiVida;
    [SerializeField] GameManager gameManager;

    Animator playerAnimator;
    [SerializeField] List<RuntimeAnimatorController> listSkins;
    public Text playerName;
    public bool isConnected = false;
    public bool isReady = false;



    public List<Transform> startPositions; 
    //Variable para el modo DeatMatch
    


    #endregion

    #region Unity Event Functions

    private void Awake()
    {

        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        playerAnimator = GetComponent<Animator>();
        NetworkManager.OnClientConnectedCallback += ConfigurePlayer;
        //NetworkManager.ConnectionApprovalCallback += StartPlayer;
        
        State = new NetworkVariable<PlayerState>();
        
        vida = new NetworkVariable<int>(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
        

        idSkin = new NetworkVariable<int>();

        playerNameValue = new NetworkVariable<FixedString64Bytes>();

        kills = new NetworkVariable<int>(0);
        deaths = new NetworkVariable<int>(0);
        ping = new NetworkVariable<int>(0);

        uiVida = GameObject.Find("UIManager").GetComponent<UIManager>();

    }

    

    private void OnEnable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
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

    public void ConfigurePlayer(ulong clientID)
    {
        
        if (IsLocalPlayer)
        {
            gameManager.clientPlayer = this;          
            ConfigureInitialPlayerState();
            ConfigureCamera();
            ConfigurePositions();
            ConfigureControls();
            playerID = clientID;

        } else
        {
            playerName.text = playerNameValue.Value.ToString();
            playerAnimator.runtimeAnimatorController = listSkins[idSkin.Value];
        }


        /*if (IsServer)
        {
            print("se conecto jugador");
            ConfigurePlayer();
            playerID = NetworkManager.Singleton.ConnectedClientsList.Count;
            ConfigureCamera();
            ConfigureControls();
        }*/
    }

   

    public void StartMatchPlayer()
    {
        ConfigurePositions();
        ResetValues();
    }
    public void StartRoundPlayer()
    {
        ConfigurePositions();
    }
    void ConfigureSkin()
    {
        var skinID = gameManager.checkSkin();
        playerAnimator.runtimeAnimatorController = listSkins[skinID];
        ConfigureSkinServerRpc(skinID);
    }

    void ConfigureName()
    {

        
        string newName = gameManager.checkName().text;        
        playerName.text = newName;           
        ConfigureNameServerRpc(newName);        
    }

    void ConfigureInitialPlayerState()
    {
        ConfigureSkin();
        ConfigureName();
        UpdatePlayerStateServerRpc(PlayerState.Grounded);
        vida.Value = 0;
        uiVida.UpdateLifeUI(vida.Value);

    }

    void ConfigureCamera()
    {
        // https://docs.unity3d.com/Packages/com.unity.cinemachine@2.6/manual/CinemachineBrainProperties.html
        var virtualCam = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera;

        virtualCam.LookAt = transform;
        virtualCam.Follow = transform;
    }

    void ConfigureControls()
    {
        GetComponent<InputHandler>().enabled = true;
    }

    public void ConfigurePositions()
    {
        int nextPosition = UnityEngine.Random.Range(0, startPositions.Count);
        Debug.Log("Spawn: " + nextPosition);
        this.transform.position = startPositions[nextPosition].position;
    }


  

    void ResetValues()
    {
        vida.Value = 0;

        kills.Value = 0;
        deaths.Value = 0;
        uiVida.UpdateLifeUI(0);

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

    [ClientRpc]
    public void UpdateVidaClientRpc(int vidaServer, ClientRpcParams clientRpcParams=default)
    {
        
        this.uiVida.UpdateLifeUI(vidaServer);
    }

    [ServerRpc]
    public void ConfigureSkinServerRpc(int skinID)
    {
        idSkin.Value = skinID;
        playerAnimator.runtimeAnimatorController = listSkins[idSkin.Value];
        ConfigureSkinClientRpc(skinID);
    }

    [ServerRpc]
    public void ConfigureNameServerRpc(string clientName)
    {
        playerNameValue.Value = clientName;
        playerName.text = playerNameValue.Value.ToString();
        ConfigureNameClientRpc(clientName);
    }

    #endregion


    #region ClientRPC

    [ClientRpc]
    public void ConfigureSkinClientRpc(int skinID)
    {
        playerAnimator.runtimeAnimatorController = listSkins[skinID];
    }
    [ClientRpc]
    public void ConfigureNameClientRpc(string serverName)
    {
        playerName.text = serverName;
    }


    #endregion

    #endregion

    #region Netcode Related Methods

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
    #endregion

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

}
    
    public enum PlayerState
    {
        Grounded = 0,
        Jumping = 1,
        Hooked = 2
    }


