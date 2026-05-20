using Imui.Controls;
using Imui.Core;
using WKLib.API.UI;

namespace WhiteKnuckleMP.UI.Windows;

public class MainWindow : WKLibWindow
{
    public bool IsClient = false;
    public bool IsServer = false;
    public bool ModWorks = true;
    
    public MainWindow()
    {
        isOpen = false;
    }
    
    public override void Draw(ImGui gui, bool isRootPanelOpen)
    {
        if (!isRootPanelOpen)
            return;

        if (!gui.BeginWindow("White Knuckle Multiplayer", ref isOpen, new ImSize(400, 400), ImWindowFlag.None))
            return;
        
        gui.Separator("Stats");
        gui.BeginReadOnly(true);
        gui.Checkbox(ref ModWorks, "Works?");
        gui.Checkbox(ref IsServer, "Server?");
        gui.Checkbox(ref IsClient, "Client?");
        gui.EndReadOnly();
        
        gui.EndWindow();
    }

    public override void HandleInput(ImGui gui)
    {
        // WHAt
    }
}