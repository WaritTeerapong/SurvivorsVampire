using System;
using Unity.Netcode;
using UnityEngine;

public struct PlayerStats : INetworkSerializable
{
    public int CurrentHealth;
    public int MoveSpeed;
    public int ATKDamage;
    public float ATKSpeed;
    public float ATKRange;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref CurrentHealth);
        serializer.SerializeValue(ref MoveSpeed);
        serializer.SerializeValue(ref ATKDamage);
        serializer.SerializeValue(ref ATKSpeed);
        serializer.SerializeValue(ref ATKRange);
    }
}

public class PlayerRunTimeStats : NetworkBehaviour
{
    public PlayerData_SO PlayerData;

    public event Action<PlayerStats> OnStatChanged;
    public NetworkVariable<PlayerStats> CurrentStats = new NetworkVariable<PlayerStats>
    (
        new PlayerStats(),
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        CurrentStats.OnValueChanged += OnStatsValueChanged;

        if (IsServer)
        {
            InitStats();
        }

    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        CurrentStats.OnValueChanged -= OnStatsValueChanged;
    }

    private void OnStatsValueChanged(PlayerStats previousValue, PlayerStats newValue)
    {
        OnStatChanged?.Invoke(newValue);
    }

    private void InitStats()
    {
        if (PlayerData == null)
        {
            Debug.LogWarning("PlayerData_SO is not assigned in PlayerRunTimeStats.");
            return;
        }

        PlayerStats initStats = new PlayerStats
        {
            CurrentHealth = PlayerData.MaxHealth,
            MoveSpeed = PlayerData.MoveSpeed,
            ATKDamage = PlayerData.ATKDamage,
            ATKSpeed = PlayerData.ATKSpeed,
            ATKRange = PlayerData.ATKRange
        };

        CurrentStats.Value = initStats;
    }

    public void ApplyDamage(int damage)
    {
        if (!IsServer) return;

        PlayerStats stats = CurrentStats.Value;
        stats.CurrentHealth -= damage;

        if (stats.CurrentHealth < 0) stats.CurrentHealth = 0;

        CurrentStats.Value = stats;
    }

    [Rpc(SendTo.Owner)]
    public void DebugLogStatsRpc()
    {
        Debug.Log($"Player {OwnerClientId} Stats - " +
            $"Health: {CurrentStats.Value.CurrentHealth}, " +
            $"MoveSpeed: {CurrentStats.Value.MoveSpeed}, " +
            $"ATKDamage: {CurrentStats.Value.ATKDamage}, " +
            $"ATKSpeed: {CurrentStats.Value.ATKSpeed}");
    }


}