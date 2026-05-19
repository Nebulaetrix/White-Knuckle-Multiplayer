using Imui.Controls;
using Imui.Core;
using WhiteKnuckleMP.Framework.Managers;
using WKLib.API.UI;

namespace WhiteKnuckleMP.UI.Windows;

public class JoinHostWindow : WKLibWindow
{
    private LobbyManager.NetworkType _selectedLobbyType;

    private string _targetIp = "127.0.0.1";
    
    private static ImDropdownPreviewType dropdownPreview;

    private string _netId = string.Empty;
    
    public JoinHostWindow()
    {
        isOpen = false;
    }
    
    public override void Draw(ImGui gui, bool isRootPanelOpen)
    {
        if (!isRootPanelOpen)
            return;
        
        if (!gui.BeginWindow("WKMP Lobby Creator", ref isOpen, new ImSize(600, 600), ImWindowFlag.None))
            return;

        gui.Dropdown(ref _selectedLobbyType, preview: dropdownPreview);
        
        gui.Separator("Actions");

        gui.TextEdit(ref _targetIp, multiline: false, hint: "host's IP");
        
        if (gui.Button("Create Lobby"))
        {
            LobbyManager.Instance.CreateLobby(_selectedLobbyType);
        }

        if (gui.Button("Join Lobby"))
        {
            LobbyManager.Instance.JoinLobby(_selectedLobbyType, _targetIp);
        }

        if (NetworkManager.Instance.Client.IsConnected && _netId == string.Empty)
        {
            _netId = NetworkManager.Instance.Client.Id.ToString();
        }
        
        gui.Separator("Info");
        
        gui.Text($"Lobby ID: {LobbyManager.Instance.CurrentLobbyId}");
        gui.Text($"Net ID: {_netId}");
        
        var grid = gui.BeginGrid(2, gui.GetRowHeight());
        foreach (var player in LobbyManager.Instance.ConnectedLobbyPlayers)
        {
            DrawLobbyMember(gui, player.NetId, player.Username, ref grid);
        }
        gui.EndGrid(grid);
        
        gui.EndWindow();
    }

    private static void DrawLobbyMember(ImGui gui, ushort netId, string username, ref ImGridState grid)
    {
        gui.TextAutoSize($"Player: {username}\nID: {netId}", gui.GridNextCell(ref grid));
    }

    public override void HandleInput(ImGui gui)
    {
        /**/
    }
}