using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using White_Knuckle_Multiplayer.Networking;
using White_Knuckle_Multiplayer.Networking.SteamLayer;

namespace White_Knuckle_Multiplayer.Managers;

public class UiManager : MonoBehaviour
{
    private static UiManager _instance;
    private GameObject playerCardTemplate;
    private Button startGame;
    private Button leaveGame;
    private Button joinGame;
    private TMP_InputField inputField;
    private TMP_Text inputFieldText;

    private Button lobbyTypeSelect;
    private TextMeshProUGUI lobbyTypeSelectText;
    private string lobbyNetworking;

    private Dictionary<CSteamID, GameObject> playerCards = new Dictionary<CSteamID, GameObject>();
    private Dictionary<ushort, GameObject> localPlayerCards = new Dictionary<ushort, GameObject>();
    
    private void Awake()
    {
        if (_instance == null || _instance != this)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }
        
        playerCardTemplate = transform.Find("Container/Content/Player Card").gameObject;
        startGame = transform.Find("Start").gameObject.GetComponent<Button>(); // This is SetActive(false); by default
        leaveGame = transform.Find("Back").gameObject.GetComponent<Button>();
        joinGame = transform.Find("LocalHost/Join").gameObject.GetComponent<Button>();
        inputField = transform.Find("LocalHost/LobbyInput").gameObject.GetComponent<TMP_InputField>();
        inputFieldText = inputField.transform.Find("Text").GetComponent<TMP_Text>();
        
        // Setup Lobby Type Button vars
        lobbyTypeSelect = transform.Find("Type").gameObject.GetComponent<Button>();
        lobbyTypeSelectText = lobbyTypeSelect.gameObject.GetComponent<TextMeshProUGUI>();
        LogManager.Warn(lobbyTypeSelectText.ToString());

        var gamemodeButton = startGame.gameObject.AddComponent<UI_Gamemode_Button>();
        gamemodeButton.gamemode = CL_AssetManager.instance.assetDatabase.gamemodeAssets.FirstOrDefault(x => x.name == "GM_Campaign");
        
        // Setup Networking to be Local at first!
        lobbyNetworking = "steam";
        lobbyTypeSelectText.text = $"Type: {lobbyNetworking}";
        
        // Start Game Callback
        startGame.onClick.AddListener(() =>
        {
            CL_GameManager.gamemode = gamemodeButton.gamemode;
            if (lobbyNetworking == "local")
                StartGameLocal();
            else
                StartGame();
        });
        
        // Join Game Callback
        joinGame.onClick.AddListener(() =>
        {
            CL_GameManager.gamemode = gamemodeButton.gamemode;
            JoinGame(inputFieldText.text);
        });
        
        leaveGame.onClick.AddListener(() =>
        {
            transform.GetComponent<UI_LerpOpen>().Hide();
            if (!LobbyManager.Instance.isInLobby) return;
            
            LobbyManager.Instance.LeaveLobby();
            CleanupPlayerCards();
        });
        
        inputField.onValueChanged.AddListener((string value) =>
        {
            LogManager.Client.Info($"Inputted text: {value}");
            inputField.text = value;
            inputFieldText.text = value;
            if (inputField.text == "")
                inputFieldText.text = "               ";
            // This is the text that shows up in the input field
        });

        lobbyTypeSelect.onClick.AddListener(() =>
        {
            lobbyNetworking = lobbyNetworking == "local" ? "steam" : "local";

            lobbyTypeSelectText.text = $"Type: {lobbyNetworking}";

            if (lobbyNetworking == "local")
            {
                CleanupPlayerCards();
                LobbyManager.Instance.LeaveLobby();
            }
            else
            {
                if (!LobbyManager.Instance.isInLobby)
                {
                    LobbyManager.Instance.CreateLobby();
                }
            }
                
        });
        
        LobbyManager.Instance.MemberJoined += OnMemberJoined;
        LobbyManager.Instance.MemberLeft += OnMemberLeft;
    }

    private void OnMemberJoined(LobbyMember member)
    {
        if (!SteamManager.connected) return;
        CreatePlayerCard(member.Name, member.GetSteamAvatar(), member.SteamID);
    }

    private void OnMemberLeft(LobbyMember member)
    {
        if (!SteamManager.connected) return;
        DeletePlayerCard(member.SteamID);
    }

    private void CreatePlayerCard(string username, Texture2D userAvatar, CSteamID steamID)
    {
        // Basically you want to run this when someone joins on the both sides
        // If joining as client, you can run this as many times as there is people in the lobby
        
        // Create a player card and parent it to the list
        var currentPlayerCard = Instantiate(playerCardTemplate, transform.Find("Container/Content"));
        
        // Keep Track of the cards
        playerCards.TryAdd(steamID, currentPlayerCard);
        
        // Setting player name
        var playerName = currentPlayerCard.transform.Find("Name").transform.GetComponent<TMP_Text>();
        playerName.text = username;
        
        // Setting player avatar
        var avatar = currentPlayerCard.transform.Find("Avatar").transform.GetComponent<Image>();
        avatar.sprite = Sprite.Create(userAvatar, new Rect(0, 0, userAvatar.width, -userAvatar.height), Vector2.zero);

        // Setting kick button
        var kickButton = currentPlayerCard.transform.Find("Kick").gameObject.GetComponent<Button>();
        
        // If host, show kick button & Start Button
        bool host;

        try
        {
            host = SteamMatchmaking.GetLobbyOwner(LobbyManager.Instance.CurrentLobby.GetLobbyID()) ==
                   SteamUser.GetSteamID();
        }
        catch
        {
            host = true;
        }
        if (host)
        {
           kickButton.gameObject.SetActive(true);
           startGame.gameObject.SetActive(true);
        }
        
        //kickButton.onClick.AddListener(() => 
        //{
            // kick the user
        //});
        
        
        // Set active after everything is set
        currentPlayerCard.SetActive(true);
        LogManager.SteamClient.Debug("Successfully created player card");
    }

    private void DeletePlayerCard(CSteamID steamID)
    {
        var playerCard = playerCards.GetValueOrDefault(steamID);
        if (playerCard)
            Destroy(playerCard);
    }
    
    private void CleanupPlayerCards()
    {
        foreach (var playerCard in playerCards.Values)
        {
            Destroy(playerCard);
        }
        
        playerCards.Clear();
    }

    /// <summary>
    /// Start the game (Steam)
    /// </summary>
    private void StartGame()
    {
        LobbyManager.Instance.isInGame = true;
        SceneManager.LoadScene("Game-Main");
        
        NetworkServer.Instance.StartSteamServer();
        
        PlayerStateManager.Instance.StartHosting();
        
        NetworkClient.Instance.StartClient("127.0.0.1", 0, "steam", true);
    }

    /// <summary>
    /// Start the game (Local)
    /// </summary>
    private void StartGameLocal()
    {
        LobbyManager.Instance.isInGame = true;
        SceneManager.LoadScene("Game-Main");
        
        NetworkServer.Instance.StartServer();

        PlayerStateManager.Instance.StartHosting();
        
        NetworkClient.Instance.StartClient("127.0.0.1");
    }

    /// <summary>
    /// Start the game (Local)
    /// </summary>
    /// <param name="ip">Ipv4 Address to connect to</param>
    private void JoinGame(string ip)
    {
        LobbyManager.Instance.isInGame = true;
        SceneManager.LoadScene("Game-Main");
        
        NetworkClient.Instance.StartClient(ip);
        
        PlayerStateManager.Instance.StartAsClient();
    }
}