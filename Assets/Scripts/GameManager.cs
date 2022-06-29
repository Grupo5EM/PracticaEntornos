using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System;


public class GameManager : NetworkBehaviour
{

    #region Variables
    [Header("Instances/Dependencies")]
    [SerializeField] NetworkManager networkManager;
    [SerializeField] UIManager uiManager;
    [SerializeField] public Player clientPlayer;

    [Header("Manager Properties")]
    [SerializeField] int skinID;
    [SerializeField] int maxPlayers;
    [SerializeField] int minPlayers;
    [SerializeField] ulong client;
    [SerializeField] Text newPlayerName;
    [SerializeField] List<Player> playerList;
    public int connectedPlayers = 0;

    [Header("NetworkVariables")]
    public NetworkVariable<float> time;
    public NetworkVariable<int> currentRound;
    public NetworkVariable<bool> matchStarted;

    [Header("In Game")]
    [SerializeField] Text player1;
    [SerializeField] Text player2;
    [SerializeField] GameObject killTexts;
    [SerializeField] GameObject listMenu;
    bool listMenuActive = false;
    [SerializeField] Text listMenuNames;
    [SerializeField] Text listMenuPing;
    [SerializeField] Text listMenuKills;
    [SerializeField] Text listMenuDeaths;

    [Header("Warm-Up Texts")]
    [SerializeField] GameObject calentamientoText;
    [SerializeField] GameObject pressR;
    [SerializeField] GameObject readyText;

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
      networkManager.OnClientConnectedCallback += ConnectionManager;
      networkManager.OnServerStarted += ServerStarted;

      time = new NetworkVariable<float>(60f);
      currentRound = new NetworkVariable<int>(1);
    }

    private void Update()
    {
        if (matchStarted.Value == true)
        {
            if (IsServer)
            {
                TimeManager();
            }
            if (IsClient)
            {
                uiManager.ShowTime();
            }
        }
    }

    private void OnEnable()
    {
        time.OnValueChanged += OnTimeValueChanged;
        currentRound.OnValueChanged += OnRoundValueChanged;
    }
    private void OnDisable()
    {
        time.OnValueChanged -= OnTimeValueChanged;
        currentRound.OnValueChanged -= OnRoundValueChanged;
    }

    #endregion

    #region Game Managing Methods


    public void ShowGameList()
    {
        if (listMenuActive == false)
        {
            listMenuActive = true;
            updatePlayerListServerRpc();
            listMenu.SetActive(true);
        }
        else
        {
            listMenu.SetActive(false);
            listMenuActive = false;
        }

    }

    public void SetSkinID(int skin)
    {
        skinID = skin;
    }

    public int CheckSkin()
    {
        return skinID;
    }

    public void SetName(Text newName)
    {
        newPlayerName = newName;
    }


    public Text CheckName()
    {
        try
        {
            if (newPlayerName == null)
            {
                newPlayerName.text = "Player";
            }
        }
        catch
        {
            newPlayerName = clientPlayer.playerNameText;
            newPlayerName.text = "Player";
        }
        return newPlayerName;
    }

    public void InitialText()
    {
        calentamientoText.SetActive(true);
        pressR.SetActive(true);
    }

    public void SetReadyText()
    {
        pressR.SetActive(false);
        readyText.SetActive(true);
    }

    public void SetReady()
    {

        if (CheckReady() == true)
        {

            SetReadyTextClientRpc();
            matchStarted.Value = true;
            for (int i = 0; i < playerList.Count; i++)
            {
                playerList[i].StartMatchPlayer();
            }
        }
    }

    private bool CheckReady()
    {
        CheckDisconnectedPlayersServerRpc();
        int playersConnected = playerList.Count;
        bool serverReady = false;
        if (playersConnected >= minPlayers)
        {
            serverReady = true;
            for (int i = 0; i < playersConnected; i++)
            {
                if (playerList[i].isReady == false)
                {
                    serverReady = false;
                }
            }
        }


        return serverReady;
    }

    public void TimeManager()
    {

        time.Value -= Time.deltaTime;
        
        if (time.Value == 0 || time.Value < 0)
        {
            currentRound.Value += 1;
            finRondaClientRpc();
            SetRoundServer();

            time.Value = 65f;
            if (currentRound.Value == 4)
            {
                MatchEndedClientRpc();
            }
        }
    }

    private void FreezePlayer()
    {
        if (clientPlayer.GetComponent<InputHandler>().enabled == false)
        {
            clientPlayer.GetComponent<InputHandler>().enabled = true;
        }
        else
        {
            clientPlayer.GetComponent<InputHandler>().enabled = false;
        }
    }

    public void ShowKillServer(string shootingPlayerS, string shotPlayerS)
    {
        player1.text = shootingPlayerS;
        player2.text = shotPlayerS;

        ShowKillClientRpc(shootingPlayerS, shotPlayerS);
    }

    void HideKill()
    {
        killTexts.SetActive(false);
    }

    #endregion

    #region Network Methods

    void ConnectionManager(ulong clientID)
    {
        if (!IsServer)
        {
            ConnectionManagerServerRpc(clientID);
        }
    }

    void ServerStarted()
    {
        if (IsHost)
        {
            HostConnectionManager(NetworkManager.ServerClientId);
        }
    }

    void HostConnectionManager(ulong clientID)
    {
        connectedPlayers++;
        NetworkObject newPlayer = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject;
        var player = newPlayer.GetComponent<Player>();
        player.playerID = clientID;
        playerList.Add(player);
    }
    public void SetRoundServer()
    {

        for (int i = 0; i < playerList.Count; i++)
        {
            playerList[i].StartRoundPlayer();
        }
    }

    void OrganisePlayerList()
    {
        for (int j = 1; j < playerList.Count; j++)
        {

            Player aux = playerList[j];

            int i = j - 1;
            while (i >= 0 && playerList[i].kills.Value < aux.kills.Value)
            {
                playerList[i + 1] = playerList[i];
                i = i - 1;
            }

            playerList[i + 1] = aux;
        }
    }

    #endregion

    #region RPC

    #region ServerRPC


    [ServerRpc(RequireOwnership = false)]
    private void ConnectionManagerServerRpc(ulong clientID)
    {
        if (matchStarted.Value == false)
        {
            CheckDisconnectedPlayersServerRpc();
            connectedPlayers++;
            if (connectedPlayers <= maxPlayers)
            {
                NetworkObject newPlayer = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject;
                var player = newPlayer.GetComponent<Player>();
                player.playerID = clientID;
                playerList.Add(player);
            }
            else
            {
                networkManager.DisconnectClient(clientID);
                connectedPlayers--;
            }
        }
        else
        {
            networkManager.DisconnectClient(clientID);
        }


    }


    [ServerRpc(RequireOwnership = false)]

    private void CheckDisconnectedPlayersServerRpc()
    {
        List<int> disconnectedID = new List<int>();
        List<Player> disconnectedPlayers = new List<Player>();
        for (int i = 0; i < playerList.Count; i++)
        {
            try
            {
                if (NetworkManager.Singleton.ConnectedClients[playerList[i].playerID] == null)
                {

                }
            }
            catch (KeyNotFoundException exception)
            {
                Debug.Log("ID no encontrado, jugador desconectado");
                int disconnected = i;
                Player disconnectedPlayer = playerList[i];
                disconnectedPlayers.Add(disconnectedPlayer);
                Debug.Log(disconnected);
                disconnectedID.Add(disconnected);
            }
        }

        if (disconnectedID.Count != 0)
        {
            Debug.Log("Comprobacion de disconnected");
            for (int j = 0; j < disconnectedPlayers.Count; j++)
            {
                playerList.Remove(disconnectedPlayers[j]);
                connectedPlayers--;
            }
        }

    }

    [ServerRpc(RequireOwnership = false)]
    void updatePlayerListServerRpc()
    {
        OrganisePlayerList();
        updatePingServerRpc();
        ResetPlayerListClientRpc();

        for (int i = 0; i < playerList.Count; i++)
        {
            string nameServer = playerList[i].playerName.Value.ToString();
            int pingServer = playerList[i].ping.Value;
            int killsServer = playerList[i].kills.Value;
            int deathsServer = playerList[i].deaths.Value;
            UpdatePlayerListClientRpc(nameServer, pingServer, killsServer, deathsServer);
        }

    }

    [ServerRpc]
    void updatePingServerRpc()
    {
        for (int i = 0; i < playerList.Count; i++)
        {
            try
            {
                int ping = (int)NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(playerList[i].playerID);
                playerList[i].ping.Value = ping;
            }
            catch (KeyNotFoundException keyException)
            {
                playerList[i].ping.Value = 0;
            }

        }
    }

    #endregion

    #region ClientRPC

    [ClientRpc]
    private void finRondaClientRpc()
    {
        FreezePlayer();
        ShowGameList();
        Invoke("FreezePlayer", 5.0f);
        Invoke("ShowGameList", 4.0f);
    }

    [ClientRpc]
    private void MatchEndedClientRpc()
    {

        uiManager.HideTime();
        ShowGameList();

        uiManager.finalText.SetActive(true);
        FreezePlayer();

    }

    [ClientRpc]
    public void SetReadyTextClientRpc()
    {
        pressR.SetActive(false);
        calentamientoText.SetActive(false);
        readyText.SetActive(false);
        uiManager.UpdateLifeUI(0);
    }

    [ClientRpc]
    void ShowKillClientRpc(string shootingPlayerC, string shotPlayerC)
    {
        player1.text = shootingPlayerC;
        player2.text = shotPlayerC;

        killTexts.SetActive(true);
        Invoke("HideKill", 7);
    }

    [ClientRpc]
    void ResetPlayerListClientRpc()
    {
        listMenuNames.text = "";
        listMenuPing.text = "";
        listMenuKills.text = "";
        listMenuDeaths.text = "";
    }

    [ClientRpc]
    void UpdatePlayerListClientRpc(string name, int ping, int kills, int deaths)
    {
        listMenuNames.text += name + "\n";
        listMenuPing.text += ping + "\n";
        listMenuKills.text += kills + "\n";
        listMenuDeaths.text += deaths + "\n";
    }
    #endregion

    #endregion

    #region Netcode Methods

    void OnTimeValueChanged(float previous, float current)
    {
        time.Value = current;
    }


    void OnRoundValueChanged(int previous, int current)
    {
        currentRound.Value = current;
    }

    #endregion
}
