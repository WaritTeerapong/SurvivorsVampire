using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerLevelManager : NetworkBehaviour
{
    public static PlayerLevelManager Instance { get; private set; }

    public LevelData_SO LevelData;

    public NetworkVariable<int> SharedLevel = new NetworkVariable<int>(1);
    public NetworkVariable<int> SharedXP = new NetworkVariable<int>(0);
    public NetworkVariable<int> SharedXPNeeded = new NetworkVariable<int>(0);

    public event Action OnLevelUp;
    public event Action OnGainXP;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SharedLevel.OnValueChanged += HandleLevelChanged;
        SharedXP.OnValueChanged += HandleXPChanged;

        if (IsServer)
        {
            if (LevelData != null)
            {
                SharedLevel.Value = LevelData.Levels[0].Level;
                SharedXP.Value = 0;
                SharedXPNeeded.Value = LevelData.Levels[1].XPNeeded;
            }
        }
    }


    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        SharedLevel.OnValueChanged -= HandleLevelChanged;
        SharedXP.OnValueChanged -= HandleXPChanged;

        OnLevelUp = null;
        OnGainXP = null;
    }

    private void HandleLevelChanged(int previousValue, int newValue)
    {
        OnLevelUp?.Invoke();
    }

    private void HandleXPChanged(int previousValue, int newValue)
    {
        OnGainXP?.Invoke();
    }

    [Rpc(SendTo.Server)]
    public void RequestGainXPRpc(int amount)
    {
        GainXP(amount);
    }

    private void GainXP(int incomingXP)
    {
        if (!IsServer) return;
        if (SharedXPNeeded.Value == -1) return;

        SharedXP.Value += incomingXP;

        while (SharedXP.Value >= SharedXPNeeded.Value && SharedXPNeeded.Value != -1)
        {
            SharedXP.Value -= SharedXPNeeded.Value;
            SharedLevel.Value++;    // OnLevelUp event fire!
            SharedXPNeeded.Value = LevelData.GetNeededXPForLevel(SharedLevel.Value + 1);

            if (SharedXPNeeded.Value == -1)
            {
                SharedXP.Value = 0;
                break;
            }
        }
    }


}