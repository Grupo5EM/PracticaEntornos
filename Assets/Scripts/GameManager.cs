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
    [SerializeField]  public Player clientPlayer;
    [SerializeField] PlayerController clientPlayerController;
    public int connectedPlayers = 0;

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


    private void Awake()
    {            
        networkManager.OnClientConnectedCallback += ConnectionManagerServerRpc;

        
    }


    private void OnEnable()
    {        
       



    }    
    private void OnDisable()
    {
        //networkManager.OnClientConnectedCallback -= OnClientConnected;
        
    }


    /*private void OnClientConnected(ulong clientID)
    {
        if (networkManager.IsServer)
        {
            var player = Instantiate(playerPrefab);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID);
            
        }
        
    } */

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
        if (playerName == null)
        {
            playerName.text = "Player";
        }
        return playerName;
    }

    [ServerRpc (RequireOwnership = false)]
    private void ConnectionManagerServerRpc(ulong clientID) 
    {
        CheckDisconnectedPlayersServerRpc();
        connectedPlayers++;
        Debug.Log(connectedPlayers);
        if(connectedPlayers <= maxPlayers) {
            NetworkObject newPlayer = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject;
            var player = newPlayer.GetComponent<Player>();
            clientPlayer = player;
            clientPlayerController = newPlayer.GetComponent<PlayerController>();
            clientPlayer.isConnected = true;
            clientPlayer.playerID = clientID;
            playerList.Add(clientPlayer);            
        }
        else
        {
            networkManager.DisconnectClient(clientID);            
            connectedPlayers--;
        }
        
    }

    [ServerRpc (RequireOwnership = false)]
  
    
    private void CheckDisconnectedPlayersServerRpc()
    {
        List<int> disconnectedID = new List<int>(); 
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
                Debug.Log(disconnected);
                disconnectedID.Add(disconnected);
            }
        }

        Debug.Log("Llega aqui");
        if (disconnectedID.Count != 0)
        {
            Debug.Log("Comprobacion de disconnected");
            for(int j = 0; j < disconnectedID.Count; j++)
            {
                playerList.RemoveAt(disconnectedID[j]);
                connectedPlayers--;
            }
        }
        
    }
   

    public void SetReady()
    {
        Debug.Log("Listo");        
        if (CheckReady() == true)
        {
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
