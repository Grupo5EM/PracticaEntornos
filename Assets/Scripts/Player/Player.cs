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

    [SerializeField] public ulong playerID;   


    [SerializeField] private UIManager uiVida;
    [SerializeField] GameManager gameManager;

    Animator playerAnimator;
    [SerializeField] List<RuntimeAnimatorController> listSkins;
    [SerializeField] Text playerName;
    public bool isConnected = false;
    public bool isReady = false;


    public List<Transform> startPositions; 
    //Variable para el modo DeatMatch
    public int kills=0;


    #endregion

    #region Unity Event Functions

    private void Awake()
    {

        Debug.Log(IsLocalPlayer);
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        playerAnimator = GetComponent<Animator>();
        NetworkManager.OnClientConnectedCallback += ConfigurePlayer;
        //NetworkManager.ConnectionApprovalCallback += StartPlayer;
        
        State = new NetworkVariable<PlayerState>();
        
        vida = new NetworkVariable<int>(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);

        uiVida = GameObject.Find("UIManager").GetComponent<UIManager>();

    }

    

    private void OnEnable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        State.OnValueChanged += OnPlayerStateValueChanged;
        vida.OnValueChanged += OnPlayerLifeValueChanged;
        idSkin.OnValueChanged += OnIDSkinValueChanged;
    }

    private void OnDisable()
      {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate


        State.OnValueChanged -= OnPlayerStateValueChanged;
        vida.OnValueChanged -= OnPlayerLifeValueChanged;
        idSkin.OnValueChanged = OnIDSkinValueChanged;
    }

    #endregion

    #region Config Methods

    public void ConfigurePlayer(ulong clientID)
    {
        
        if (IsLocalPlayer)
        {            
            ConfigureInitialPlayerState();
            ConfigureCamera();
            ConfigurePositions();
            ConfigureControls();
            playerID = clientID;
            
        } else
        {
            
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
        //ConfigureSkin();
        //ConfigureName();
    }

    void ConfigureSkin()
    {
        int skinID = gameManager.checkSkin();
        playerAnimator.runtimeAnimatorController = listSkins[skinID];
        ConfigureSkinServerRpc(skinID);
    }

    void ConfigureName()
    {
        var newName = gameManager.checkName();
        playerName.text = newName.text;
        ConfigureNameServerRpc(newName.text);        
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

    void ConfigurePositions()
    {
        int nextPosition = UnityEngine.Random.Range(0, startPositions.Count);
        Debug.Log("Spawn: " + nextPosition);
        this.transform.position = startPositions[nextPosition].position;
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
    public void UpdateVidaClientRpc(ClientRpcParams clientRpcParams=default)
    {
        //this.vida.Value = this.vida.Value+1;
        this.uiVida.UpdateLifeUI(this.vida.Value);
    }

    [ServerRpc]
    public void ConfigureSkinServerRpc(int skinID)
    {
        playerAnimator.runtimeAnimatorController = listSkins[skinID];
        ConfigureSkinClientRpc(skinID);
    }

    [ServerRpc]
    public void ConfigureNameServerRpc(string clientName)
    {
        playerName.text = clientName;
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
        //this.uiVida.UpdateLifeUI(this.vida.Value);
    }

    void OnIDSkinValueChanged(int previous, int current)
    {
        idSkin.Value = current;
    }
    #endregion
}
    
    public enum PlayerState
    {
        Grounded = 0,
        Jumping = 1,
        Hooked = 2
    }


