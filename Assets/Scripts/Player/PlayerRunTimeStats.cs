using System;
using Unity.Netcode;
using UnityEngine;

public struct PlayerStats : INetworkSerializable
{
    public int CurrentHealth;
    public int MaxHealth;
    public int MoveSpeed;
    public int ATKDamage;
    public float ATKSpeed;
    public float ATKRange;
    public void ApplyStat(StatType type, BaseStat baseStats, float bonus)
    {
        switch (type)
        {
            case StatType.MaxHealth:
                MaxHealth = baseStats.MaxHealth + Mathf.RoundToInt(bonus);
                break;
            case StatType.MoveSpeed:
                MoveSpeed = baseStats.MoveSpeed + Mathf.RoundToInt(bonus);
                break;
            case StatType.ATKDamage:
                ATKDamage = baseStats.ATKDamage + Mathf.RoundToInt(bonus);
                break;
            case StatType.ATKSpeed:
                ATKSpeed = baseStats.ATKSpeed + bonus;
                break;
            case StatType.ATKRange:
                ATKRange = baseStats.ATKRange + bonus;
                break;
        }
    }
    public float GetCurrentStat(StatType type)
    {
        return type switch
        {
            StatType.MaxHealth => MaxHealth,
            StatType.MoveSpeed => MoveSpeed,
            StatType.ATKDamage => ATKDamage,
            StatType.ATKSpeed => ATKSpeed,
            StatType.ATKRange => ATKRange,
            _ => 0
        };
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref CurrentHealth);
        serializer.SerializeValue(ref MaxHealth);
        serializer.SerializeValue(ref MoveSpeed);
        serializer.SerializeValue(ref ATKDamage);
        serializer.SerializeValue(ref ATKSpeed);
        serializer.SerializeValue(ref ATKRange);
    }
}
public struct LevelCheckpoint : INetworkSerializable
{
    public int MaxHealth;
    public int MoveSpeed;
    public int ATKDamage;
    public int ATKSpeed;
    public int ATKRange;

    public int IncrementLevel(StatType type)
    {
        return type switch
        {
            StatType.MaxHealth => ++MaxHealth,
            StatType.MoveSpeed => ++MoveSpeed,
            StatType.ATKDamage => ++ATKDamage,
            StatType.ATKSpeed => ++ATKSpeed,
            StatType.ATKRange => ++ATKRange,
            _ => 0
        };
    }
    public int GetCurrentLevel(StatType type)
    {
        return type switch
        {
            StatType.MaxHealth => MaxHealth,
            StatType.MoveSpeed => MoveSpeed,
            StatType.ATKDamage => ATKDamage,
            StatType.ATKSpeed => ATKSpeed,
            StatType.ATKRange => ATKRange,
            _ => 0
        };
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref MaxHealth);
        serializer.SerializeValue(ref MoveSpeed);
        serializer.SerializeValue(ref ATKDamage);
        serializer.SerializeValue(ref ATKSpeed);
        serializer.SerializeValue(ref ATKRange);
    }
}

public class PlayerRunTimeStats : NetworkBehaviour
{
    public PlayerData_SO PlayerData;
    public StatUpgradeDatabase_SO StatUpgradeData;
    public event Action<PlayerStats> OnStatChanged;
    public NetworkVariable<PlayerStats> CurrentStats = new NetworkVariable<PlayerStats>
    (
        new PlayerStats(),
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );
    public NetworkVariable<LevelCheckpoint> CurrentStatsLevel = new NetworkVariable<LevelCheckpoint>
    (
        new LevelCheckpoint(),
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
            InitStatsLevel();
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
            CurrentHealth = PlayerData.Stat.MaxHealth,
            MaxHealth = PlayerData.Stat.MaxHealth,
            MoveSpeed = PlayerData.Stat.MoveSpeed,
            ATKDamage = PlayerData.Stat.ATKDamage,
            ATKSpeed = PlayerData.Stat.ATKSpeed,
            ATKRange = PlayerData.Stat.ATKRange
        };

        CurrentStats.Value = initStats;
    }
    private void InitStatsLevel()
    {
        if (PlayerData == null)
        {
            Debug.LogWarning("PlayerData_SO is not assigned in PlayerRunTimeStats.");
            return;
        }

        LevelCheckpoint initStats = new LevelCheckpoint
        {
            MaxHealth = PlayerData.StatLevel.MaxHealthLevel,
            MoveSpeed = PlayerData.StatLevel.MoveSpeedLevel,
            ATKDamage = PlayerData.StatLevel.ATKDamageLevel,
            ATKSpeed = PlayerData.StatLevel.ATKSpeedLevel,
            ATKRange = PlayerData.StatLevel.ATKRangeLevel
        };

        CurrentStatsLevel.Value = initStats;
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
            $"MaxHealth: {CurrentStats.Value.MaxHealth}, " +
            $"MoveSpeed: {CurrentStats.Value.MoveSpeed}, " +
            $"ATKDamage: {CurrentStats.Value.ATKDamage}, " +
            $"ATKSpeed: {CurrentStats.Value.ATKSpeed}, " +
            $"ATKRange: {CurrentStats.Value.ATKRange}");
    }

    [Rpc(SendTo.Owner)]
    public void DebugLogStatsLevelRpc()
    {
        Debug.Log($"Player {OwnerClientId} Stats - " +
            $"MaxHealth Level: {CurrentStatsLevel.Value.MaxHealth}, " +
            $"MoveSpeed Level: {CurrentStatsLevel.Value.MoveSpeed}, " +
            $"ATKDamage Level: {CurrentStatsLevel.Value.ATKDamage}, " +
            $"ATKSpeed Level: {CurrentStatsLevel.Value.ATKSpeed}, " +
            $"ATKRange Level: {CurrentStatsLevel.Value.ATKRange}");
    }

    [Rpc(SendTo.Server)]
    public void RequestUpgradeServerRpc(StatType chosenStat)
    {
        PlayerStats currentStat = CurrentStats.Value;
        LevelCheckpoint statLevel = CurrentStatsLevel.Value;

        int newLevel = statLevel.IncrementLevel(chosenStat);
        float bonus = FindUpgradeStat(chosenStat, newLevel);
        currentStat.ApplyStat(chosenStat, PlayerData.Stat,bonus);

        CurrentStats.Value = currentStat;
        CurrentStatsLevel.Value = statLevel;
    }

    private float FindUpgradeStat(StatType chosenStat, int level)
    { 
        foreach(StatUpgrade stat in StatUpgradeData.Stats)
        {
            if(stat.StatType == chosenStat)
            {
                return stat.GetBonusForLevel(level);
            }
        }
        Debug.LogWarning($"Stat {chosenStat} not found in database!");
        return 0f;
    }
}