using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerLevelManager : NetworkBehaviour
{
    public static PlayerLevelManager Instance { get; private set; }

    public LevelData_SO LevelData;
    public StatUpgradeDatabase_SO StatUpgradeData;

    public NetworkVariable<int> SharedLevel = new NetworkVariable<int>(1);
    public NetworkVariable<int> SharedXP = new NetworkVariable<int>(0);
    public NetworkVariable<int> SharedXPNeeded = new NetworkVariable<int>(0);

    public event Action OnLevelUp;
    public event Action OnGainXP;

    [SerializeField] private List<StatType> _availableUpgradeStat;


    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        _availableUpgradeStat.Add(StatType.MaxHealth);
        _availableUpgradeStat.Add(StatType.MoveSpeed);
        _availableUpgradeStat.Add(StatType.ATKDamage);
        _availableUpgradeStat.Add(StatType.ATKSpeed);
        _availableUpgradeStat.Add(StatType.ATKRange);
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
    
    public Dictionary<StatType, int> RandomUpgradeStats(LevelCheckpoint statLevel)
    {
        Dictionary<StatType, int> selectedUpgradeStats = new Dictionary<StatType, int>();

        List<StatType> poolToDrawFrom = new List<StatType>(_availableUpgradeStat);
        // Exclude stat that has maxlevel from pull
        for (int i = poolToDrawFrom.Count - 1; i >= 0; i--)
        {
            StatType stat = poolToDrawFrom[i];
            int maxLevel = StatUpgradeData.GetMaxLevelForStat(stat);
            int currentLevel = statLevel.GetCurrentLevel(stat);

            if (maxLevel == 0) continue;

            if (currentLevel >= maxLevel)
            {
                poolToDrawFrom.RemoveAt(i); 
            }
        }

        StatType selectedStat;
        while (selectedUpgradeStats.Count < 3 && poolToDrawFrom.Count > 0)
        {
            int randomIndex = Random.Range(0, poolToDrawFrom.Count);
            selectedStat = poolToDrawFrom[randomIndex];

            poolToDrawFrom.RemoveAt(randomIndex);

            int currentLevel = statLevel.GetCurrentLevel(selectedStat);
            selectedUpgradeStats.Add(selectedStat, currentLevel);
        }

        return selectedUpgradeStats;
    }
}