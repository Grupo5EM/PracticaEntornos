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
            playerNameText.text = playerName.Value.ToString();
            playerAnimator.runtimeAnimatorController = listSkins[idSkin.Value];
        }

    }


    void ConfigureInitialPlayerState()
    {
        ConfigureSkin();
        ConfigureName();
        UpdatePlayerStateServerRpc(PlayerState.Grounded);
        vida.Value = 0;
        lifeUI.UpdateLifeUI(vida.Value);
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
        this.transform.position = startPositions[nextPosition].position;
    }

    void ConfigureSkin()
    {
        var skinID = gameManager.CheckSkin();
        playerAnimator.runtimeAnimatorController = listSkins[skinID];
        ConfigureSkinServerRpc(skinID);
    }

    void ConfigureName()
    {
        string newName = gameManager.CheckName().text;
        playerNameText.text = newName;
        ConfigureNameServerRpc(newName);
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
        playerName.Value = clientName;
        playerNameText.text = playerName.Value.ToString();
        ConfigureNameClientRpc(clientName);
    }

    #endregion


    #region ClientRPC


    [ClientRpc]
    public void UpdateLifeClientRpc(int vidaServer, ClientRpcParams clientRpcParams = default)
    {
        this.lifeUI.UpdateLifeUI(vidaServer);
    }
    [ClientRpc]
    public void ConfigureSkinClientRpc(int skinID)
    {
        playerAnimator.runtimeAnimatorController = listSkins[skinID];
    }
    [ClientRpc]
    public void ConfigureNameClientRpc(string serverName)
    {
        playerNameText.text = serverName;
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


