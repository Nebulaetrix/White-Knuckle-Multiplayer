using UnityEngine;

namespace WhiteKnuckleMP.Framework.Managers;

public class StateManager : MonoBehaviour
{
    public static StateManager Instance { get; private set; }

    [System.Serializable]
    public enum State
    {
        Disconnected,
        InLobby,
        InGame,
        
    }
}