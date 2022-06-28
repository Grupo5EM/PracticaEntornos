using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System;


public class GameManager : NetworkBehaviour
{

    [SerializeField] NetworkManager networkManager;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] int skinID;
    [SerializeField] int maxPlayers;
    [SerializeField] int minPlayers;
    [SerializeField] ulong client;
    [SerializeField] Text playerName;
    [SerializeField] List<Player> playerList;




    [SerializeField] public Player clientPlayer;    
    public int connectedPlayers = 0;
    bool matchStarted = false;
    public bool isHost = false;

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
    [Header("Calentamiento")]
    [SerializeField] GameObject calentamientoText;
    [SerializeField] GameObject pulsaR;
    [SerializeField] GameObject listoText;

    private void Awake()
    {
      networkManager.OnClientConnectedCallback += connectionManager;
      networkManager.OnServerStarted += serverStarted;

    }


    private void OnEnable()
    {

    }
    private void OnDisable()
    {

    }

    public void setSkinID(int skin)
    {
        skinID = skin;
    }

    public int checkSkin()
    {
        return skinID;
    }

    public void setName(Text newName)
    {
        playerName = newName;
    }

    public Text checkName()
    {
        try
        {
            if (playerName == null)
            {
                playerName.text = "Player";
            }
        }
        catch
        {
            playerName = clientPlayer.playerName;
            playerName.text = "Player";
        }      
        return playerName;
    }


    void connectionManager(ulong clientID)
    {      
        if (!IsServer)
        {
            ConnectionManagerServerRpc(clientID);          
        }
    }

    void serverStarted()
    {
        if (IsHost)
        {
            hostConnectionManager(NetworkManager.ServerClientId);
        }
    }

    void hostConnectionManager(ulong clientID)
    {
        connectedPlayers++;
        NetworkObject newPlayer = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject;
        var player = newPlayer.GetComponent<Player>();
        player.isConnected = true;
        player.playerID = clientID;
        playerList.Add(player);
    }


    [ServerRpc(RequireOwnership = false)]
    private void ConnectionManagerServerRpc(ulong clientID)
    {
        if (matchStarted == false) {
            CheckDisconnectedPlayersServerRpc();
            connectedPlayers++;
            if (connectedPlayers <= maxPlayers)
            {
                NetworkObject newPlayer = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject;
                var player = newPlayer.GetComponent<Player>();                
                player.isConnected = true;
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
        for (int i = 0; i < playerList.Count; i++) {
            try
            {
                if (NetworkManager.Singleton.ConnectedClients[playerList[i].playerID] == null)
                {

                }
            } catch (KeyNotFoundException exception)
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

    public void initialText()
    {
        calentamientoText.SetActive(true);
        pulsaR.SetActive(true);
    }

    public void setReadyText()
    {
        pulsaR.SetActive(false);
        listoText.SetActive(true);
    }

    [ClientRpc]
    public void setReadyTextClientRpc()
    {
        pulsaR.SetActive(false);
        calentamientoText.SetActive(false);
        listoText.SetActive(false);
    }

   
    public void SetReady()
    {
        
        if (CheckReady() == true)
        {

            setReadyTextClientRpc();
            matchStarted = true;
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
    [ServerRpc(RequireOwnership = false)]
    public void setRoundServerRpc()
    {

        for (int i = 0; i < playerList.Count; i++)
        {
            playerList[i].StartRoundPlayer();
        }
    }

    public void showKillServer(string shootingPlayerS, string shotPlayerS)
    {
        player1.text = shootingPlayerS;
        player2.text = shotPlayerS;

        showKillClientRpc(shootingPlayerS, shotPlayerS);
    }

    [ClientRpc]
    void showKillClientRpc(string shootingPlayerC, string shotPlayerC)
    {
        player1.text = shootingPlayerC;
        player2.text = shotPlayerC;

        killTexts.SetActive(true);
        Invoke("hideKill", 7);
    }

    
    void hideKill()
    {
        killTexts.SetActive(false);
    }

    public void showGameList()
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

    [ServerRpc (RequireOwnership = false)]
    void updatePlayerListServerRpc()
    {
        organisePlayerList();
        updatePingServerRpc();
        resetPlayerListClientRpc();

        for (int i = 0; i < playerList.Count; i++)
        {
           string nameServer = playerList[i].playerNameValue.Value.ToString();
           int pingServer = playerList[i].ping.Value;
           int killsServer = playerList[i].kills.Value;
           int deathsServer = playerList[i].deaths.Value;
           updatePlayerListClientRpc(nameServer, pingServer, killsServer, deathsServer);
        }
            
    }

    [ClientRpc]
    void resetPlayerListClientRpc()
    {
        listMenuNames.text = "";
        listMenuPing.text = "";
        listMenuKills.text = "";
        listMenuDeaths.text = "";               
    }

    [ClientRpc]
    void updatePlayerListClientRpc(string name, int ping, int kills, int deaths)
    {
        listMenuNames.text += name + "\n";
        listMenuPing.text += ping + "\n";
        listMenuKills.text += kills + "\n";
        listMenuDeaths.text += deaths + "\n";
    }
      

    void organisePlayerList()
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

    [ServerRpc]
    void updatePingServerRpc()
    {
        for (int i = 0; i < playerList.Count; i++)
        {
            int ping = (int)NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(playerList[i].playerID);
            playerList[i].ping.Value = ping;
        }
    }
}
