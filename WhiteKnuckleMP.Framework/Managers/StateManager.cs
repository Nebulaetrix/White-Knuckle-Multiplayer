using System;
using UnityEngine;
using WhiteKnuckleMP.Utils;

namespace WhiteKnuckleMP.Framework.Managers;

public class StateManager : MonoBehaviour
{
    public static StateManager Instance { get; private set; } = null!;

    [System.Serializable]
    public enum State
    {
        Disconnected,
        InLobby,
        InGame,
    }

    public State CurrentState { get; private set; } = State.Disconnected;

    // Fired whenever the game state changes. Passes: (OldState, NewState)
    public static event Action<State, State>? OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// Request a transition to a new game state.
    /// </summary>
    public void TransitionTo(State newState)
    {
        if (CurrentState == newState) return;

        // Add guard rails to prevent weird bugs
        if (CurrentState == State.Disconnected && newState == State.InGame)
        {
            LogManager.StateManager.Warn("Trap avoided! Cannot jump straight from Disconnected to InGame without entering a Lobby.");
            return;
        }

        State oldState = CurrentState;
        CurrentState = newState;

        LogManager.StateManager.Info($"State Changed: {oldState} ➔ {newState}");

        // Notify every other system in the mod that something happened!
        OnStateChanged?.Invoke(oldState, newState);
    }
}