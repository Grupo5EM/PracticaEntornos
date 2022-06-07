using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System;


public class GameManager : MonoBehaviour
{
    [SerializeField] NetworkManager networkManager;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] int skinID;
    [SerializeField] Text playerName;
    int connectedPlayers;
    private void OnEnable()
    {
        //networkManager.OnClientConnectedCallback += OnClientConnected;
        



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
        return playerName;
    }

    public void connectionManager() 
    {
        
    }
}
