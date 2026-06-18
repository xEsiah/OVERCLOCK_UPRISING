using UnityEngine;
using System;

public enum PlayerState
{
    Default,
    Hanging,
    Mantling,
    Tutorial
}

public class PlayerStateManager : MonoBehaviour
{
    public PlayerState CurrentState { get; private set; } = PlayerState.Default;

    public event Action<PlayerState> OnStateChanged;
    public event Action<bool> OnLedgeAvailable;

    public void ChangeState(PlayerState newState)
    {
        if (CurrentState == newState) return;
        
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    public void SetLedgeAvailable(bool available)
    {
        OnLedgeAvailable?.Invoke(available);
    }
}