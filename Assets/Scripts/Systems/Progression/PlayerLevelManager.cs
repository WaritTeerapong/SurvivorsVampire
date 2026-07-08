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

    public int LocalPendingUpgrades { get; private set; } = 0;
    private bool _isUpgradeSceneLoaded = false;

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
            if (LevelData != null && LevelData.Levels.Length > 0)
            {
                SharedLevel.Value = LevelData.Levels[0].Level;
                SharedXP.Value = 0;

                if (LevelData.Levels.Length > 1)
                    SharedXPNeeded.Value = LevelData.Levels[1].XPNeeded;
                else
                    SharedXPNeeded.Value = LevelData.Levels[0].XPNeeded;
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
        if (IsServer && PauseManager.Instance.PlayersSelectingUpgrade.Count == 0 && changeEvent.Type == NetworkListEvent<ulong>.EventType.Remove)
        {
            _isUpgradeSceneLoaded = false;

            if (PlayerManager.Instance != null)
            {
                foreach (Player player in PlayerManager.Instance.AllPlayers)
                {
                    if (player != null && !player.IsDownOrDied)
                    {
                        // Heal player by 40% of their Max HP on upgrade finish
                        player.Stats.HealPercentMaxHealth(0.4f);
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
        int delta = newValue - previousValue;
        if (delta > 0)
        {
            LocalPendingUpgrades += delta;
        }

        ReviveDownedPlayers();

        if (IsServer)
        {
            if (!_isUpgradeSceneLoaded)
            {
                _isUpgradeSceneLoaded = true;
                SceneController.Instance
                    .NewTransition()
                    .Load(Slots.SESSION_CONTENT, Scenes.UPGRADE, setActive: true)
                    .Perform();
            }
        }
    }

    public void ConsumePendingUpgrade()
    {
        if (LocalPendingUpgrades > 0)
        {
            LocalPendingUpgrades--;
        }
    }

    [Rpc(SendTo.Server)]
    public void ForceSyncPendingUpgradesServerRpc(ulong targetClientId, int pendingCount)
    {
        ForceSyncPendingUpgradesClientRpc(pendingCount, RpcTarget.Single(targetClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ForceSyncPendingUpgradesClientRpc(int pendingCount, RpcParams rpcParams = default)
    {
        LocalPendingUpgrades = pendingCount;
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
        if (!IsServer || LevelData == null) return;
        if (SharedXPNeeded.Value == -1) return;

        SharedXP.Value += incomingXP;

        int maxLevel = LevelData.Levels.Length; // Calculate dynamic max level based on Array length

        while (SharedXP.Value >= SharedXPNeeded.Value && SharedXPNeeded.Value != -1)
        {
            SharedXP.Value -= SharedXPNeeded.Value;

            if (SharedLevel.Value < maxLevel)
            {
                SharedLevel.Value++;

                if (SharedLevel.Value < maxLevel)
                {
                    SharedXPNeeded.Value = LevelData.GetNeededXPForLevel(SharedLevel.Value + 1);
                }
                else
                {
                    // Reached max level. Maintain the last XP requirement to allow looping.
                    SharedXPNeeded.Value = LevelData.GetNeededXPForLevel(maxLevel);
                }
            }
            else
            {
                // Already at max level and bar filled up again. Trigger Auto Heal.
                SharedXPNeeded.Value = LevelData.GetNeededXPForLevel(maxLevel);
                HealAllPlayers(0.4f);
            }
        }
    }

    private void HealAllPlayers(float percent)
    {
        if (PlayerManager.Instance != null)
        {
            foreach (Player player in PlayerManager.Instance.AllPlayers)
            {
                if (player != null && !player.IsDownOrDied)
                {
                    player.Stats.HealPercentMaxHealth(percent);
                }
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