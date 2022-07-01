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
    //Variables para definir las propiedades de cada jugador y de la sala 
    [Header("Manager Properties")]
    [SerializeField] int skinID;
    [SerializeField] int maxPlayers;
    [SerializeField] int minPlayers;
    [SerializeField] ulong client;
    [SerializeField] Text newPlayerName;
    [SerializeField] List<Player> playerList;
    public int connectedPlayers = 0;
    //variables para cuando los jugadores ya estan jugando la ronda 
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
    //variables para la ui de la sala de calentamiento
    [Header("Warm-Up Texts")]
    [SerializeField] GameObject calentamientoText;
    [SerializeField] GameObject pressR;
    [SerializeField] GameObject readyText;

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
     //Hace un callback para los clientes y otro para el servidor 
      networkManager.OnClientConnectedCallback += ConnectionManager;
      networkManager.OnServerStarted += ServerStarted;

      time = new NetworkVariable<float>(60f);
      currentRound = new NetworkVariable<int>(1);
    }

    //En Update tenemos los métodos para que si es el servidor vaya cambiando el tiempo y maneje las rondas y que en los clientes pinten los resultados del tiempo y las rondas
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

    //Mostramos la lista con todos los jugadores y sus bajas, vidas ping y nombre
    public void ShowGameList()
    {
        //Si la lista no está activada, se activa y viceversa
        if (listMenuActive == false)
        {
            //Si se va a activar/mostrar, actualiza algunos valores en el servidor y en el cliente se muestran los datos actualizados
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
    //Establecemos la skin del jugador 
    public void SetSkinID(int skin)
    {
        skinID = skin;
    }
    // Devuelve la skin del jugador 
    public int CheckSkin()
    {
        return skinID;
    }

    //Se establece el nombre del jugador
    public void SetName(Text newName)
    {
        newPlayerName = newName;
    }

    //Este metodo devuelve el nombre del jugador. Además comprueba el nombre del jugador está vacío y se asegura de ponerle uno si no lo ha hecho el jugador 
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
    //Actimamos la interfaz de la ronda de calentamiento
    public void InitialText()
    {
        calentamientoText.SetActive(true);
        pressR.SetActive(true);
    }

    //Activamos la interfaz de estar listo
    public void SetReadyText()
    {
        pressR.SetActive(false);
        readyText.SetActive(true);
    }

    //Una vez pulsamos el boton de que estamos listos en la sala de calentamiendo y están todos los jugadores  preparados se inicia la partida 
    public void SetReady()
    {
        //Mira si todos los jugadores están listos
        if (CheckReady() == true)
        {
            //Actualiza/Elimina los textos en los clientes
            SetReadyTextClientRpc();
            //Indica que la partida ha empezado
            matchStarted.Value = true;
            //Y respawnea a cada jugador y resetea sus valores
            for (int i = 0; i < playerList.Count; i++)
            {
                playerList[i].StartMatchPlayer();
            }
        }
    }
    //Comprueba que todos los jugadores están listos para jugar una vez han pulsado el botón
    private bool CheckReady()
    {
        //Primero comprobamos los jugadores que se han desconectado y los elimina de la playerList
        CheckDisconnectedPlayersServerRpc();
        int playersConnected = playerList.Count;
        bool serverReady = false;
        //Si los jugadores conectados son en mínimo o más
        if (playersConnected >= minPlayers)
        {
            serverReady = true;
            //Comprueba si hay algún jugador que no esté listo
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
    //Controlamos el tiempo y cuando llega a 0 actualiza la ronda y el tiempo lo pone a 5 segundos más para paralizar jugadores y mostrar los resultados
    public void TimeManager()
    {

        time.Value -= Time.deltaTime;
        //Si el tiempo acaba
        if (time.Value == 0 || time.Value < 0)
        {
            //Suma la ronda, y actualiza los clientes y el servidor
            currentRound.Value += 1;
            finRondaClientRpc();
            SetRoundServer();

            time.Value = 65f;
            //Cuando acaba la ultima ronda señalamos que acaba el juego
            if (currentRound.Value == 4)
            {
                MatchEndedClientRpc();
            }
        }
    }
    //Congela los movimientos del jugador para cuando finaliza la ronda o termina el juego
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
    //Mostramos por pantalla cuando hay un asesinato a dos jugadores 
    public void ShowKillServer(string shootingPlayerS, string shotPlayerS)
    {
        player1.text = shootingPlayerS;
        player2.text = shotPlayerS;

        ShowKillClientRpc(shootingPlayerS, shotPlayerS);
    }

    //Escondemos los nombres del asesinato una vez pasado un tiempo 
    void HideKill()
    {
        killTexts.SetActive(false);
    }

    #endregion

    #region Network Methods
    //Conecta el cliente y envía el ServerRPC
    void ConnectionManager(ulong clientID)
    {
        if (!IsServer)
        {
            ConnectionManagerServerRpc(clientID);
        }
    }
    //Inicia el servidor, pero solo se ejecuta si es un Host
    void ServerStarted()
    {
        if (IsHost)
        {
            HostConnectionManager(NetworkManager.ServerClientId);
        }
    }

    //Informamos cuando se conecta un host a la partida: se añade a la lista de jugadores y suma los jugadores conectados
    void HostConnectionManager(ulong clientID)
    {
        connectedPlayers++;
        NetworkObject newPlayer = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject;
        var player = newPlayer.GetComponent<Player>();
        player.playerID = clientID;
        playerList.Add(player);
    }

    //Respawnea a todos los jugadores una vez empieza la ronda
    public void SetRoundServer()
    {

        for (int i = 0; i < playerList.Count; i++)
        {
            playerList[i].StartRoundPlayer();
        }
    }
    //Este metodo organiza la lista de jugadores en función a sus bajas 
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

    //Actualiza en el servidor la conexión de un cliente
    [ServerRpc(RequireOwnership = false)]
    private void ConnectionManagerServerRpc(ulong clientID)
    {
        //Se conecta mientras la partida no haya empezado
        if (matchStarted.Value == false)
        {
            //Mira los jugadores desconectados y suma los jugadores conectados
            CheckDisconnectedPlayersServerRpc();
            connectedPlayers++;
            //Si no llegamos al máximo de jugadores, añadimos el jugador del cliente a la lista y le asigna el ID
            if (connectedPlayers <= maxPlayers)
            {
                NetworkObject newPlayer = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject;
                var player = newPlayer.GetComponent<Player>();
                player.playerID = clientID;
                playerList.Add(player);
            }
            else
            {
                //Sino, le desconecta
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
    //Recorre la lista de jugadores y comprueba si hay alguna desconexión
    private void CheckDisconnectedPlayersServerRpc()
    {
        List<int> disconnectedID = new List<int>();
        List<Player> disconnectedPlayers = new List<Player>();
        for (int i = 0; i < playerList.Count; i++)
        {
            //Por cada jugador de la lista de jugadores, mira si están dentro de los clientes conectados
            try
            {
                if (NetworkManager.Singleton.ConnectedClients[playerList[i].playerID] == null)
                {

                }
            }
            catch (KeyNotFoundException exception)
            {
                //Si no está en el diccionario, coge el jugador y lo mete en una lista
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
            //Con la lista de jugadores desconectados, los busca en la lista de players y los elimina
            Debug.Log("Comprobacion de disconnected");
            for (int j = 0; j < disconnectedPlayers.Count; j++)
            {
                playerList.Remove(disconnectedPlayers[j]);
                connectedPlayers--;
            }
        }

    }

    //Este metodo analiza los jugadores y sus estadísticas y los va actualizando y pasa la información al cliente para cuando enseña la lista de jugadores
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

    //Actualizamos el ping del jugador 
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
    //Cunado acaba la ronda se paraliza a los jugadores 5 segundos y se muestra la lista con los datos durante 4 
    [ClientRpc]
    private void finRondaClientRpc()
    {
        FreezePlayer();
        ShowGameList();
        Invoke("FreezePlayer", 5.0f);
        Invoke("ShowGameList", 4.0f);
    }
    //Enseña en cada cliente la UI/pantalla de fin de partida
    [ClientRpc]
    private void MatchEndedClientRpc()
    {

        uiManager.HideTime();
        ShowGameList();

        uiManager.finalText.SetActive(true);
        FreezePlayer();

    }

    //Resetea y elimina los elementos de la UI necesarios para cuando empiece la partida en todos los clientes
    [ClientRpc]
    public void SetReadyTextClientRpc()
    {
        pressR.SetActive(false);
        calentamientoText.SetActive(false);
        readyText.SetActive(false);
        uiManager.UpdateLifeUI(0);
    }

    //Cuando se mata a alguien se muestra a todos los clientes el jugador que ha matado y el que ha muerto
    [ClientRpc]
    void ShowKillClientRpc(string shootingPlayerC, string shotPlayerC)
    {
        player1.text = shootingPlayerC;
        player2.text = shotPlayerC;

        killTexts.SetActive(true);
        Invoke("HideKill", 7);
    }
    //Se resetean los valores de la lista en todos los clientes para luego poder mostrarlos correctamente
    [ClientRpc]
    void ResetPlayerListClientRpc()
    {
        listMenuNames.text = "";
        listMenuPing.text = "";
        listMenuKills.text = "";
        listMenuDeaths.text = "";
    }

    //Actualizamos la lista con los datos de los jugadores al pulsar la E 
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
    //actualizamos el tiempo cuando cambia
    void OnTimeValueChanged(float previous, float current)
    {
        time.Value = current;
    }

    //actualizamos la ronda cuando cambia
    void OnRoundValueChanged(int previous, int current)
    {
        currentRound.Value = current;
    }

    #endregion
}
