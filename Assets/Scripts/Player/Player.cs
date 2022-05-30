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
    public List<Transform> startPositions; 


    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        NetworkManager.OnClientConnectedCallback += ConfigurePlayer;

        State = new NetworkVariable<PlayerState>();
        characterColor = new NetworkVariable<CharachterColor>();
    }

    private void OnEnable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        State.OnValueChanged += OnPlayerStateValueChanged;
       
    }

    private void OnDisable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        

        State.OnValueChanged -= OnPlayerStateValueChanged;
        
    }

    #endregion

    #region Config Methods

    void ConfigurePlayer(ulong clientID)
    {
        if (IsLocalPlayer)
        {
            ConfigurePlayer();
            ConfigureCamera();
            ConfigurePositions();
            ConfigureControls();
        }
    }

    void ConfigurePlayer()
    {
        //UpdatePlayerStateServerRpc(PlayerState.Grounded);

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

    #endregion
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
