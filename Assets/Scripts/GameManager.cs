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
    [SerializeField] Player clientPlayer;
    [SerializeField] PlayerController clientPlayerController;
    public int connectedPlayers = 0;


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
}
