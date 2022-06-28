using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.Netcode;
using UnityEngine.UI;
using System;


public class Player : NetworkBehaviour
{
    #region Variables

    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    public NetworkVariable<PlayerState> State;
    public NetworkVariable<int> vida;
    [SerializeField] public NetworkVariable<bool> disparando;

    [SerializeField] private int playerID;
    public int PlayerID => playerID;


    [SerializeField] private UIManager uiVida;
    [SerializeField] GameManager gameManager;

    Animator playerAnimator;
    [SerializeField] List<RuntimeAnimatorController> listSkins;
    [SerializeField] Text playerName;

    
    //private UIManager uiVida;
    public List<Transform> startPositions; 
    //Variable para el modo DeatMatch
    public NetworkVariable<int> bajas;
    public NetworkVariable<int> muertes;


    #endregion

    #region Unity Event Functions

    private void Awake()
    {


        Debug.Log("Despert");
        Debug.Log(IsLocalPlayer);
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        playerAnimator = GetComponent<Animator>();
        NetworkManager.OnClientConnectedCallback += ConfigurePlayer;
        NetworkManager.ConnectionApprovalCallback += StartPlayer;
        
        State = new NetworkVariable<PlayerState>();
        
        vida = new NetworkVariable<int>(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
        bajas = new NetworkVariable<int>(0);
        muertes = new NetworkVariable<int>(0);

        disparando = new NetworkVariable<bool>(false);

        uiVida = GameObject.Find("UIManager").GetComponent<UIManager>();

    }

    

    private void OnEnable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        State.OnValueChanged += OnPlayerStateValueChanged;
        vida.OnValueChanged += OnPlayerLifeValueChanged;
        bajas.OnValueChanged += OnPlayerBajasValueChanged;
        muertes.OnValueChanged += OnPlayerMuertesValueChanged;
        disparando.OnValueChanged += OnPlayerDisparandoValueChanged;
    }

    private void OnDisable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate


        State.OnValueChanged -= OnPlayerStateValueChanged;
        vida.OnValueChanged -= OnPlayerLifeValueChanged;
        bajas.OnValueChanged -= OnPlayerBajasValueChanged;
        muertes.OnValueChanged -= OnPlayerMuertesValueChanged;
        disparando.OnValueChanged -= OnPlayerDisparandoValueChanged;
    }

    #endregion

    #region Config Methods

    public void ConfigurePlayer(ulong clientID)
    {

        Debug.Log("Se configura");
        Debug.Log(IsLocalPlayer);
        if (IsLocalPlayer)
        {
            Debug.Log("Configura jugador");
            ConfigureInitialPlayerState();
            ConfigureCamera();
            ConfigurePositions();
            ConfigureControls();
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

    private void StartPlayer(byte[] arg1, ulong arg2, NetworkManager.ConnectionApprovedDelegate arg3)
    {
        ConfigureCamera();
        ConfigurePositions();
        ConfigureControls();
    }

    public void StartPlayerNoCallback()
    {
        ConfigureCamera();
        ConfigurePositions();
        ConfigureControls();
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
        if (newName != null)
        {
            playerName.text = newName.text;
            ConfigureNameServerRpc(newName.text);

        }
        
    }

    void ConfigureInitialPlayerState()
    {
        ConfigureSkin();
        ConfigureName();
        UpdatePlayerStateServerRpc(PlayerState.Grounded);
        vida.Value = 0;
        uiVida.UpdateLifeUI(vida.Value);
        disparando.Value = false;

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

    void OnPlayerDisparandoValueChanged(bool previous, bool current)
    {
        disparando.Value = current;
    }

    void OnPlayerBajasValueChanged(int previous, int current)
    {
        bajas.Value = current;
        //this.uiVida.UpdateLifeUI(this.vida.Value);
    }

    void OnPlayerMuertesValueChanged(int previous, int current)
    {
        muertes.Value = current;
        //this.uiVida.UpdateLifeUI(this.vida.Value);
    }
    #endregion
}
    public enum PlayerState
    {
        Grounded = 0,
        Jumping = 1,
        Hooked = 2,
        OnAir= 3
    }


