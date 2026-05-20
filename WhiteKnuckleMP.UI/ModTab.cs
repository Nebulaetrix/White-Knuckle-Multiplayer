using Imui.Controls;
using Imui.Core;

namespace WhiteKnuckleMP.UI;

public class WKModTab : WKLib.API.UI.ModTab
{
    public override string DisplayName => "WK Multiplayer";
    
    public override void DrawSubMenu(ImGui gui)
    {
        if (gui.Button("Main Window"))
        {
            WindowDeclarations.MainWin.isOpen = true;
        }

        if (gui.Button("Lobby Creator"))
        {
            WindowDeclarations.JoinHostWin.isOpen = true;
        }
    }

}