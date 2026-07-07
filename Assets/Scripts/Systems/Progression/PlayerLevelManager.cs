using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerLevelManager : NetworkBehaviour
{
    public static PlayerLevelManager Instance { get; private set; }

    [Header("=== Databases ===")]
    public LevelData_SO LevelData;

    public NetworkVariable<int> SharedLevel = new NetworkVariable<int>(1);
    public NetworkVariable<int> SharedXP = new NetworkVariable<int>(0);
    public NetworkVariable<int> SharedXPNeeded = new NetworkVariable<int>(0);

    public event Action OnLevelUp;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            if (LevelData != null)
            {
                SharedLevel.Value = LevelData.Levels[0].Level;
                SharedXP.Value = 0;
                SharedXPNeeded.Value = LevelData.Levels[1].XPNeeded;
            }

            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.PlayersSelectingUpgrade.OnListChanged += OnPlayersSelectingUpgradeChanged;
            }
        }

        SharedLevel.OnValueChanged += OnLevelChange;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        SharedLevel.OnValueChanged -= OnLevelChange;

        if (IsServer && PauseManager.Instance != null)
        {
            PauseManager.Instance.PlayersSelectingUpgrade.OnListChanged -= OnPlayersSelectingUpgradeChanged;
        }
    }

    private void OnPlayersSelectingUpgradeChanged(NetworkListEvent<ulong> changeEvent)
    {
        // Check if all players have finished selecting their upgrades
        if (IsServer && PauseManager.Instance.PlayersSelectingUpgrade.Count == 0 && changeEvent.Type == NetworkListEvent<ulong>.EventType.Remove)
        {
            // Reset health for all active players in the session
            if (PlayerManager.Instance != null)
            {
                foreach (Player player in PlayerManager.Instance.AllPlayers)
                {
                    if (player != null && !player.IsDownOrDied)
                    {
                        player.Stats.ResetHealthToMax();
                    }
                }
            }

            if (SceneController.Instance != null)
            {
                SceneController.Instance
                    .NewTransition()
                    .Unload(Slots.SESSION_CONTENT)
                    .SetSceneActive(Slots.SESSION)
                    .WithClearUnusedAssets()
                    .Perform();
            }
        }
    }

    private void OnLevelChange(int previousValue, int newValue)
    {
        ReviveDownedPlayers();
        if (IsServer)
        {
            SceneController.Instance
                .NewTransition()
                .Load(Slots.SESSION_CONTENT, Scenes.UPGRADE, setActive: true)
                .Perform();
        }
    }

    public void OnUpgradeSelected()
    {
        if (IsServer)
        {
            OnLevelUp?.Invoke();
        }
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
            SharedLevel.Value++;
            SharedXPNeeded.Value = LevelData.GetNeededXPForLevel(SharedLevel.Value + 1);

            if (SharedXPNeeded.Value == -1)
            {
                SharedXP.Value = 0;
                break;
            }
        }
    }

    private void ReviveDownedPlayers()
    {
        if (PlayerManager.Instance != null)
        {
            foreach (Player player in PlayerManager.Instance.AllPlayers)
            {
                if (player != null && player.IsDowned)
                {
                    player.RevivePlayerRpc(isReviveOnFullHealth: false, healAmount: 0.5f);
                }
            }
        }
    }
}