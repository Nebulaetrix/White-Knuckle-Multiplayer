using System;
using System.Collections;
using BepInEx.Logging;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace White_Knuckle_Multiplayer.Managers;
public class CommandManager
{
    private readonly GameManager gameManager;
    private readonly ManualLogSource logger;
    private readonly MonoBehaviour coroutineHost;

    private const string ErrorCoroutineHostNull = "Coroutine host is null! Cannot start host command.";
    private const string ErrorSteamNotInitialized = "Steam not initialized.";
    private const string ErrorInvalidSteamId = "Please provide a valid Steam ID.";
    private const string ErrorTransportUnavailable = "Transport unavailable.";
    private const string MessageShutdown = "Shutting down host and disconnecting all clients...";
    private const string SceneMainMenu = "Main-Menu";

    public CommandManager(GameManager gameManager, ManualLogSource logger, MonoBehaviour coroutineHost)
    {
        this.gameManager = gameManager;
        this.logger = logger;
        this.coroutineHost = coroutineHost;
    }

    public void HandleHostCommand(string[] args)
    {
        if (!ValidateCoroutineHost()) return;

        coroutineHost.StartCoroutine(CreateLobbyAndHostGame());
    }

    public void HandleJoinCommand(string[] args)
    {
        if (!ValidateArgsAndParseSteamId(args, out ulong steamId)) return;

        JoinLobbyAndStartClient(steamId);
    }

    public void HandleDisconnectCommand(string[] args)
    {
        ShutdownHostAndDisconnectClients();
    }

    private IEnumerator CreateLobbyAndHostGame()
    {
        if (!IsSteamClientValid()) yield break;

        gameManager.StartHost();
    }

    private void JoinLobbyAndStartClient(ulong steamId)
    {
        if (!IsSteamClientValid()) return;

        var transport = gameManager.GetTransport();
        if (transport == null)
        {
            LogError(ErrorTransportUnavailable);
            return;
        }

        transport.targetSteamId = steamId;

        try
        {
            gameManager.StartClient();
        }
        catch (Exception ex)
        {
            LogError($"Client start error: {ex.Message}");
        }
    }

    private void ShutdownHostAndDisconnectClients()
    {
        logger.LogInfo(MessageShutdown);
        NetworkManager.Singleton.NetworkConfig.NetworkTransport.Shutdown();
        SceneManager.LoadScene(SceneMainMenu);
        logger.LogInfo("Host and client connections shut down.");
    }

    private bool ValidateCoroutineHost()
    {
        if (coroutineHost == null)
        {
            LogError(ErrorCoroutineHostNull);
            return false;
        }
        return true;
    }

    private bool ValidateArgsAndParseSteamId(string[] args, out ulong steamId)
    {
        if (args.Length != 1 || !ulong.TryParse(args[0], out steamId))
        {
            LogWarning(ErrorInvalidSteamId);
            steamId = 0;
            return false;
        }
        return true;
    }

    private bool IsSteamClientValid()
    {
        if (!SteamClient.IsValid || !SteamClient.IsLoggedOn)
        {
            LogError(ErrorSteamNotInitialized);
            return false;
        }
        return true;
    }

    private void LogError(string message)
    {
        CommandConsole.LogError(message);
        logger.LogError(message);
    }

    private void LogWarning(string message)
    {
        CommandConsole.LogError(message);
        logger.LogWarning(message);
    }
}
