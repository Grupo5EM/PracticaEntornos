using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class UIManager : MonoBehaviour
{

    #region Variables

    [SerializeField] NetworkManager networkManager;
    UnityTransport transport;
    readonly ushort port = 7777;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Player playerScript;
    [SerializeField] Sprite[] hearts = new Sprite[3];

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    [SerializeField] private Button buttonServer;
    [SerializeField] private InputField inputFieldIP;
    


    //Añadimos por aquí más elementos para el lobby 
    [Header("Menu Personalización")]
    [SerializeField] private GameObject menuPersonalizacion;
    [SerializeField] private InputField nombreUsuario;
    //Botones para seleccionar persoanjes
    [SerializeField] private Button rosa;
    [SerializeField] private Button verde;
    [SerializeField] private Button naranja;
    [SerializeField] private Button azul;
    [SerializeField] private Button changeNameButton;
    [SerializeField] public  Button preparado;
    
    [SerializeField] private GameManager gameManager;    
    [SerializeField] GameObject player;

    [Header("Tiempo y Rondas")]
    [SerializeField] public Text contador;
    private float time = 60f;
    private int rondaActual = 1;
    [SerializeField] public Text Ronda ;


    [Header("Final de Juego")]
    [SerializeField] private GameObject menuVictoria;
    [SerializeField] private Text TextoFinal;



    [Header("In-Game HUD")]
    [SerializeField] private GameObject inGameHUD;
    [SerializeField] RawImage[] heartsUI = new RawImage[3];
    [SerializeField] private Text bajasJugador;
    [SerializeField] private Text informacionBajas;

    #endregion

    #region Unity Event Functions

    private void Awake()

    {

        transport = (UnityTransport)networkManager.NetworkConfig.NetworkTransport;
    }




    private void Start()
    {
        buttonHost.onClick.AddListener(() => StartHost());
        buttonClient.onClick.AddListener(() => StartClient());
        buttonServer.onClick.AddListener(() => StartServer());
        changeNameButton.onClick.AddListener(() => ChangeName());
        

        verde.onClick.AddListener(() => SkinPersonaje(0));
        azul.onClick.AddListener(() => SkinPersonaje(1));
        rosa.onClick.AddListener(() => SkinPersonaje(2));
        naranja.onClick.AddListener(() => SkinPersonaje(3));
        
        ActivateMainMenu();
        FindPlayer();
        
    }

    #endregion

    #region UI Related Methods
    private void TiempoyRondas()
    {
        contador.text = " " + time;
        Ronda.text = " " + rondaActual;

    }
    private void ContadorTiempo()
    {
        time -= Time.deltaTime;
        contador.text = " " + time.ToString("f0");
        if (time==0)
        {
            rondaActual++;
            //paralizarrondas
        
            time = 60f;
            if (rondaActual == 4)
            {
                //llamar a Fin de partida completo
            }
        }
    }
    
    private void SkinPersonaje(int color)
    {
        FindPlayer();
        //Aquí se pasaria por paramtro el color de la skin que se quiere para modificar luego el animator
        if (color == 0)
        {          
            //playerController.anim.runtimeAnimatorController = prefabSkins[0];
            gameManager.setSkinID(0);
            Debug.Log("Skin cambiada a verde");
        } else if (color == 1)
        {            
            //playerController.anim.runtimeAnimatorController = prefabSkins[1];
            gameManager.setSkinID(1);
            Debug.Log("Skin cambiada a azul");
        } else if (color == 2)
        {           
            //playerController.anim.runtimeAnimatorController = prefabSkins[2];
            gameManager.setSkinID(2);
            Debug.Log("Skin cambiada a rosa");
        } else if (color == 3)
        {           
            //playerController.anim.runtimeAnimatorController = prefabSkins[3];
            gameManager.setSkinID(3);
            Debug.Log("Skin cambiada a naranja");            
        }

    }
    //private void FinPartida()
    //{
    //    TextoFinal.text = " ";
    //    player.GetComponent<InputHandler>().enabled = false;
    //    menuVictoria.SetActive(true);
    //    for (int i = 0; i < lista.Length; i++)
    //    {
    //        TextoFinal.text += lista[i] + " " + lista[i].bajas + " " + lista[i].muertes;
    //    }

    //}
    private void JugarHost()
    {
        //playerScript = player.GetComponent<Player>();
        NetworkManager.Singleton.StartHost();
        ActivateInGameHUD();
        //playerScript.StartPlayerNoCallback();
        mainMenu.SetActive(false);
        menuPersonalizacion.SetActive(false);
        inGameHUD.SetActive(true);
    }

    private void JugarClient()
    {
        //playerScript = player.GetComponent<Player>();
        NetworkManager.Singleton.StartClient();
        ActivateInGameHUD();
        //playerScript.StartPlayerNoCallback();
        mainMenu.SetActive(false);
        menuPersonalizacion.SetActive(false);
        inGameHUD.SetActive(true);
    }
    private void ActivateMainMenu()
    {
        mainMenu.SetActive(true);

        inGameHUD.SetActive(false);
    }
    private void ActualizarBajas(int bajas)
    {
        bajasJugador.text = ""+bajas;
    }
    //Activamos el menu
    private void MenuPersonalizacion()
    {
        
        mainMenu.SetActive(false);
        menuPersonalizacion.SetActive(true);
        
    }
    //En este metodo informaremos que jugador a matado a quien
    private void ParteDeGuerra(string asesino, string muerto)
    {

        informacionBajas.text = " Ha matado a ";

    }
    private void ActivateInGameHUD()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(true);

        //por cada unidad se le quita medio corazón, a 0 tiene 3 vidas y si lo pones a 4 solo te queda un corazón 
        UpdateLifeUI(0);
    }

    public void UpdateLifeUI(int hitpoints)
    {
        switch (hitpoints)
        {
            case 6:
                heartsUI[0].texture = hearts[2].texture;
                heartsUI[1].texture = hearts[2].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 5:
                heartsUI[0].texture = hearts[1].texture;
                heartsUI[1].texture = hearts[2].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 4:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[2].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 3:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[1].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 2:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[0].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 1:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[0].texture;
                heartsUI[2].texture = hearts[1].texture;
                break;
        }
    }


    public void ChangeName()
    {
        FindPlayer();
        gameManager.setName(nombreUsuario.textComponent);
        
    }

    #endregion

    #region Netcode Related Methods

    private void StartHost()
    {
        mainMenu.SetActive(false);
        menuPersonalizacion.SetActive(true);
        //NetworkManager.Singleton.StartHost();        
        preparado.onClick.AddListener(JugarHost);
        FindPlayer();

        //ActivateInGameHUD();
    }
    //Con estos metodos establecemos la sincronización de los jugadores con el botón listo
    private void OkeyCliente()
    {
        //Si todos los clientes han dicho que está listo habitar boton de listo al host
       // if ()
        //{
          //  preparado.IsActive();
        //}

    }
    private bool OkeyHost()
    {
        //Se le congela al cliente la pantalla hasta que inicie el host 

        return true;


    }
    private void StartClient()

    {

        var ip = inputFieldIP.text;

        if (!string.IsNullOrEmpty(ip))

        {

            transport.SetConnectionData(ip, port);

        }

        mainMenu.SetActive(false);
        menuPersonalizacion.SetActive(true);
        //NetworkManager.Singleton.StartClient();
        preparado.onClick.AddListener(JugarClient);
        FindPlayer();
        

    }
    private void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        ActivateInGameHUD();
       
    }


    private void FindPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            playerScript = player.GetComponent<Player>();

            if (!playerScript.IsLocalPlayer)
            {
                playerController = null;
                playerScript = null;
                player = null;
            }
        }
    }
    #endregion

}


