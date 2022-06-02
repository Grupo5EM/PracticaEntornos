using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    #region Variables

    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    public NetworkVariable<PlayerState> State;

    public NetworkVariable<CharachterColor> characterColor;

    public NetworkVariable<int> vida;
    [SerializeField] private int playerID;
    public int PlayerID => playerID;
    private UIManager uiVida;
    public List<Transform> startPositions; 
    //Variable para el modo DeatMatch
    public int kills=0;
    public List<Transform> startPositions;


    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        Debug.Log("Despert?);
        Debug.Log(IsLocalPlayer);
        NetworkManager.OnClientConnectedCallback += ConfigurePlayer;

        State = new NetworkVariable<PlayerState>();
        vida = new NetworkVariable<int>(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
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



    void ConfigureInitialPlayerState()
    {

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
        int nextPosition = Random.Range(0, startPositions.Count);
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

    public void UpdateCharacterColorServerRpc(CharachterColor color)
    {
        characterColor.Value = color;
    }

    [ServerRpc]
    public void UpdatePlayerLifeServerRpc(int vida)
    {

        this.vida.Value += vida;
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


    void OnCharachterColorValueChanged(CharachterColor previous, CharachterColor current)
    {
        characterColor.Value = current;

    }
    
    void OnPlayerLifeValueChanged(int previous, int current)
    {
        vida.Value = current;
        if(!IsLocalPlayer)
            this.uiVida.UpdateLifeUI(vida.Value);
    }

    public enum PlayerState
    {
        Grounded = 0,
        Jumping = 1,
        Hooked = 2
    }

    public enum CharachterColor
    {
        Verde = 0,
        Azul = 1,
        Rosa = 2,
        Naranja = 3
    }
}
