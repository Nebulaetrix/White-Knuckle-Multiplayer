using Imui.Controls;
using Imui.Core;
using UnityEngine;
using WKLib.API.UI;

namespace WhiteKnuckleMP.UI.Windows;

public class LobbyWindow : WKLibWindow
{
    private double _lastFetchTime = Time.time;

    public LobbyWindow()
    {
        isOpen = false;
    }


    public override void Draw(ImGui gui, bool isRootPanelOpen)
    {
        if (!isRootPanelOpen)
            return;
        
        if (!gui.BeginWindow("WKMP Lobbies", ref isOpen, new ImSize(600, 600), ImWindowFlag.None))
            return;
        
    }

    public override void HandleInput(ImGui gui)
    {
        /**/
    }
}