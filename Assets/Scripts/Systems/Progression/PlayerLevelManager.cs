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
    public event Action OnMaxLevelLoop; // Event แจ้ง UI ให้เล่นอนิเมชันตอนหลอดวนลูป

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

                // ดึงค่า XP ที่ต้องการสำหรับขึ้นเลเวล 2 มาตั้งต้น
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

            if (PlayerManager.Instance != null)
            {
                foreach (Player player in PlayerManager.Instance.AllPlayers)
                {
                    if (player != null && !player.IsDownOrDied)
                    {
                        // ฮีล 40% หลังจากเลือกอัปเกรดเสร็จ
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
            // หน้าต่างอัปเกรดจะเด้งขึ้นมา "ก็ต่อเมื่อ" SharedLevel มีการเปลี่ยนแปลงเท่านั้น
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
        if (!IsServer || LevelData == null || LevelData.Levels.Length == 0) return;

        SharedXP.Value += incomingXP;

        // ดึงตัวเลขเลเวลสูงสุดจาก Array ช่องสุดท้ายมาใช้เป็น Max Level (เช่น 20)
        int maxLevel = LevelData.Levels[LevelData.Levels.Length - 1].Level;

        // ใส่เงื่อนไข > 0 เพื่อป้องกันกรณี XPNeeded เป็น 0 แล้วเกิด Infinite Loop ค้าง
        while (SharedXP.Value >= SharedXPNeeded.Value && SharedXPNeeded.Value > 0)
        {
            if (SharedLevel.Value >= maxLevel)
            {
                // [ถึงจุด Max Level] - หักลบ XP, สั่งฮีล, ทริกเกอร์ลูป UI โดย **ห้ามบวกเลเวลเด็ดขาด**
                SharedXP.Value -= SharedXPNeeded.Value;
                HealActivePlayers(0.4f);
                TriggerMaxLevelLoopClientRpc();
            }
            else
            {
                // [เลเวลอัปปกติ] - หักลบ XP และบวกเลเวลเพิ่ม 1
                SharedXP.Value -= SharedXPNeeded.Value;
                SharedLevel.Value++;

                // ค้นหา XP ของเลเวลถัดไปมาเตรียมไว้รอ
                int nextXPNeeded = LevelData.GetNeededXPForLevel(SharedLevel.Value + 1);
                if (nextXPNeeded != -1)
                {
                    SharedXPNeeded.Value = nextXPNeeded;
                }
                // ถ้าเป็น -1 (พึ่งถึงเวล 20 พอดี) จะใช้ค่า SharedXPNeeded เดิมของเลเวล 20 ต่อไปตามที่คุณต้องการ
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