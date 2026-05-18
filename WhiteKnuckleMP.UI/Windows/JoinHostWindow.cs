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
        
        gui.Separator("Info");
        
        gui.Text($"Lobby ID: {LobbyManager.Instance.CurrentLobbyId}");
        gui.Text($"Net ID: no clue");
        
        gui.EndWindow();
        //what the fuck
    }

    public override void HandleInput(ImGui gui)
    {
        /**/
    }
}