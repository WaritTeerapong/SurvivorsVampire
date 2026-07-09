using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PlayerLevelManager : NetworkBehaviour
{
    public static PlayerLevelManager Instance { get; private set; }

    [Header("=== Databases ===")]
    public LevelData_SO LevelData;

    public NetworkVariable<int> SharedLevel = new NetworkVariable<int>(1);
    public NetworkVariable<int> SharedXP = new NetworkVariable<int>(0);
    public NetworkVariable<int> SharedXPNeeded = new NetworkVariable<int>(0);

    public event Action OnLevelUp;
    public event Action OnMaxLevelLoop;
    public event Action<int> OnPendingUpgradesAdded;

    public int LocalPendingUpgrades { get; private set; } = 0;

    private bool _isUpgradeSceneLoaded = false;
    private float _lastUnloadTime = -999f;
    private Coroutine _loadCoroutine;

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

                int initialXP = LevelData.GetNeededXPForLevel(SharedLevel.Value + 1);
                SharedXPNeeded.Value = initialXP != -1 ? initialXP : LevelData.Levels[0].XPNeeded;
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

            _lastUnloadTime = Time.realtimeSinceStartup;

            if (PlayerManager.Instance != null)
            {
                foreach (Player player in PlayerManager.Instance.AllPlayers)
                {
                    if (player != null && !player.IsDownOrDied)
                    {
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
            OnPendingUpgradesAdded?.Invoke(delta);
        }

        ReviveDownedPlayers();

        if (IsServer)
        {
            if (!_isUpgradeSceneLoaded)
            {
                _isUpgradeSceneLoaded = true;

                if (_loadCoroutine != null) StopCoroutine(_loadCoroutine);
                _loadCoroutine = StartCoroutine(SafeLoadUpgradeScene());
            }
        }
    }

    private IEnumerator SafeLoadUpgradeScene()
    {
        while (Time.realtimeSinceStartup - _lastUnloadTime < 1.5f)
        {
            yield return null;
        }

        if (_isUpgradeSceneLoaded && SceneController.Instance != null)
        {
            SceneController.Instance
                .NewTransition()
                .Load(Slots.SESSION_CONTENT, Scenes.UPGRADE, setActive: true)
                .Perform();
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

    [Rpc(SendTo.Server)]
    public void ForceLevelUpByXPNeededRpc()
    {
        if (!IsServer) return;

        int xpNeeded = SharedXPNeeded.Value;
        if (xpNeeded > 0)
        {
            GainXP(xpNeeded);
        }
    }

    private void GainXP(int incomingXP)
    {
        if (!IsServer || LevelData == null || LevelData.Levels.Length == 0) return;

        SharedXP.Value += incomingXP;
        int maxLevel = LevelData.Levels[LevelData.Levels.Length - 1].Level;

        while (SharedXP.Value >= SharedXPNeeded.Value && SharedXPNeeded.Value > 0)
        {
            if (SharedLevel.Value >= maxLevel)
            {
                SharedXP.Value -= SharedXPNeeded.Value;
                HealActivePlayers(0.4f);
                TriggerMaxLevelLoopClientRpc();
            }
            else
            {
                SharedXP.Value -= SharedXPNeeded.Value;
                SharedLevel.Value++;

                int nextXPNeeded = LevelData.GetNeededXPForLevel(SharedLevel.Value + 1);
                if (nextXPNeeded != -1)
                {
                    SharedXPNeeded.Value = nextXPNeeded;
                }
            }
        }
    }

    private void HealActivePlayers(float percentage)
    {
        if (PlayerManager.Instance == null) return;

        foreach (Player player in PlayerManager.Instance.AllPlayers)
        {
            if (player != null && !player.IsDownOrDied)
            {
                player.Stats.HealPercentMaxHealth(percentage);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void TriggerMaxLevelLoopClientRpc()
    {
        OnMaxLevelLoop?.Invoke();
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