using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class UIManager : MonoBehaviour
{

    #region Variables

    UnityTransport transport;
    readonly ushort port = 7777;
    [SerializeField] Sprite[] hearts = new Sprite[3];

    [Header("Instances/Depencencies")]
    [SerializeField] NetworkManager networkManager;
    [SerializeField] private GameManager gameManager;

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    [SerializeField] private Button buttonServer;
    [SerializeField] private InputField inputFieldIP;



    //Añadimos por aquí más elementos para el lobby 
    [Header("Menu Personalización")]
    [SerializeField] private GameObject customizeMenu;
    [SerializeField] private InputField username;
    //Botones para seleccionar persoanjes
    [SerializeField] private Button greenButton;
    [SerializeField] private Button blueButton;
    [SerializeField] private Button pinkButton;
    [SerializeField] private Button orangeButton;
    
    [SerializeField] private Button changeNameButton;
    [SerializeField] public Button readyButton;


    [Header("Tiempo y Rondas")]
    [SerializeField] public Text counterText;
    [SerializeField] public Text roundText;


    [Header("Final de Juego")]
    [SerializeField] public GameObject finalText;

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
        buttonHost.onClick.AddListener(() => HostMenu());
        buttonClient.onClick.AddListener(() => ClientMenu());
        buttonServer.onClick.AddListener(() => StartServer());
        changeNameButton.onClick.AddListener(() => ChangeName());


        greenButton.onClick.AddListener(() => SkinSelector(0));
        blueButton.onClick.AddListener(() => SkinSelector(1));
        pinkButton.onClick.AddListener(() => SkinSelector(2));
        orangeButton.onClick.AddListener(() => SkinSelector(3));

        ActivateMainMenu();

        finalText.SetActive(false);

    }

    #endregion

    #region UI Related Methods


    private void ActivateMainMenu()
    {
        mainMenu.SetActive(true);

        inGameHUD.SetActive(false);
    }


    private void HostMenu()
    {
        mainMenu.SetActive(false);
        customizeMenu.SetActive(true);
        readyButton.onClick.AddListener(StartHost);
    }

    private void ClientMenu()

    {
        var ip = inputFieldIP.text;

        if (!string.IsNullOrEmpty(ip))

        {

            transport.SetConnectionData(ip, port);

        }

        mainMenu.SetActive(false);
        customizeMenu.SetActive(true);
        readyButton.onClick.AddListener(StartClient);
    }


    private void SkinSelector(int color)
    {
        //Aquí se pasaria por paramtro el color de la skin que se quiere para modificar luego el animator
        if (color == 0)
        {
            gameManager.SetSkinID(0);
        }
        else if (color == 1)
        {
            gameManager.SetSkinID(1);
        }
        else if (color == 2)
        {
            gameManager.SetSkinID(2);
        }
        else if (color == 3)
        {
            gameManager.SetSkinID(3);
        }

    }
    public void ChangeName()
    {

        gameManager.SetName(username.textComponent);

    }

    private void ActivateInGameHUD()
    {
        mainMenu.SetActive(false);

        inGameHUD.SetActive(true);

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
            case 0:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[0].texture;
                heartsUI[2].texture = hearts[0].texture;
                break;
        }
    }

    public void ShowTime()
    {
        counterText.text = "Tiempo: " + gameManager.time.Value.ToString("f0");
        roundText.text = "Ronda:  " + gameManager.currentRound.Value;

    }

    public void HideTime()
    {
        counterText.enabled = false;
        roundText.enabled = false;
    }

    #endregion

    #region Netcode Related Methods



    private void StartHost()
    {

        gameManager.InitialText();
        NetworkManager.Singleton.StartHost();
        ActivateInGameHUD();

        mainMenu.SetActive(false);
        customizeMenu.SetActive(false);
        inGameHUD.SetActive(true);
    }

    private void StartClient()
    {

        gameManager.InitialText();
        NetworkManager.Singleton.StartClient();
        ActivateInGameHUD();

        mainMenu.SetActive(false);
        customizeMenu.SetActive(false);
        inGameHUD.SetActive(true);
    }

    private void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        ActivateInGameHUD();
    }

    #endregion

}


