using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class GameManager : MonoBehaviour
{
    [SerializeField] NetworkManager networkManager;
    [SerializeField] GameObject playerPrefab;
    private void OnEnable()
    {
        //networkManager.OnClientConnectedCallback += OnClientConnected;
        



    }    
    private void OnDisable()
    {
        //networkManager.OnClientConnectedCallback -= OnClientConnected;
        
    }


    private void OnClientConnected(ulong clientID)
    {
        if (networkManager.IsServer)
        {
            var player = Instantiate(playerPrefab);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID);
            
        }
        
    }

    public void setSkin(GameObject skin)
    {
        playerPrefab = skin;
    }




    
}
