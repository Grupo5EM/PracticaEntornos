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

    [SerializeField] private int playerID;
    public int PlayerID => playerID;
    [SerializeField] private UIManager uiVida;
    [SerializeField] GameManager gameManager;

    Animator playerAnimator;
    [SerializeField] List<RuntimeAnimatorController> listSkins;
    [SerializeField] Text playerName;

    public List<Transform> startPositions;
    //Variable para el modo DeatMatch
    public int kills = 0;


    #endregion

    #region Unity Event Functions

    private void Awake()
    {

        Debug.Log("Desperté");
        Debug.Log(IsLocalPlayer);
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        playerAnimator = GetComponent<Animator>();
        NetworkManager.OnClientConnectedCallback += ConfigurePlayer;
        NetworkManager.ConnectionApprovalCallback += StartPlayer;
        
        State = new NetworkVariable<PlayerState>();

        vida = new NetworkVariable<int> { Value = 0 };
        uiVida = GameObject.Find("UIManager").GetComponent<UIManager>();

    }

    

    private void OnEnable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        State.OnValueChanged += OnPlayerStateValueChanged;
        vida.OnValueChanged += OnPlayerLifeValueChanged;
    }

    private void OnDisable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate


        State.OnValueChanged -= OnPlayerStateValueChanged;
        vida.OnValueChanged -= OnPlayerLifeValueChanged;
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
        this.vida.Value += vida;

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

    }
    #endregion
}
    public enum PlayerState
    {
        Grounded = 0,
        Jumping = 1,
        Hooked = 2
    }

