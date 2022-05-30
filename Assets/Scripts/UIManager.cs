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

    [SerializeField] Sprite[] hearts = new Sprite[3];

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    [SerializeField] private Button buttonServer;
    [SerializeField] private InputField inputFieldIP;

    //A�adimos por aqu� m�s elementos para el lobby 
    [Header("Menu Personalizaci�n")]
    [SerializeField] private GameObject menuPersonalizacion;
    [SerializeField] private InputField nombreUsuario;
    //Botones para seleccionar persoanjes
    [SerializeField] private Button rosa;
    [SerializeField] private Button verde;
    [SerializeField] private Button naranja;
    [SerializeField] private Button azul;
    [SerializeField] public  Button preparado;

    



    [Header("In-Game HUD")]
    [SerializeField] private GameObject inGameHUD;
    [SerializeField] RawImage[] heartsUI = new RawImage[3];

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
        preparado.onClick.AddListener(() => Jugar());


        rosa.onClick.AddListener(() => SkinPersonaje());
        verde.onClick.AddListener(() => SkinPersonaje());
        naranja.onClick.AddListener(() => SkinPersonaje());
        azul.onClick.AddListener(() => SkinPersonaje());
        ActivateMainMenu();
    }

    #endregion

    #region UI Related Methods
    private void SkinPersonaje()
    {
        //Aqu� se pasaria por paramtro el color de la skin que se quiere para modificar luego el animator

    }
    private void Jugar()
    {
        NetworkManager.Singleton.StartHost();
        ActivateInGameHUD();
        mainMenu.SetActive(false);
        menuPersonalizacion.SetActive(false);
        inGameHUD.SetActive(true);
    }
    private void ActivateMainMenu()
    {
        mainMenu.SetActive(true);

        inGameHUD.SetActive(false);
    }
    //Activamos el menu
    private void MenuPersonalizacion()
    {
        
        mainMenu.SetActive(false);
        menuPersonalizacion.SetActive(true);
        
    }
    private void ActivateInGameHUD()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(true);

        //por cada unidad se le quita medio coraz�n, a 0 tiene 3 vidas y si lo pones a 4 solo te queda un coraz�n 
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

    #endregion

    #region Netcode Related Methods

    private void StartHost()
    {
        mainMenu.SetActive(false);
        menuPersonalizacion.SetActive(true);
        //NetworkManager.Singleton.StartHost();
        //ActivateInGameHUD();
    }
    //Con estos metodos establecemos la sincronizaci�n de los jugadores con el bot�n listo
    private void OkeyCliente()
    {
        //Si todos los clientes han dicho que est� listo habitar boton de listo al host
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

        NetworkManager.Singleton.StartClient();

        ActivateInGameHUD();

    }
    private void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        ActivateInGameHUD();
    }

    #endregion

}
