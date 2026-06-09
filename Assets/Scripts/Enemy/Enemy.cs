using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public struct EnemyCurrentStats : INetworkSerializable
{
    public int EnemyID;
    public int Tier;
    public int CurrentHealth;
    public int MoveSpeed;
    public int ATKDamage;
    public float ATKSpeed;
    public float ATKRange;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref EnemyID);
        serializer.SerializeValue(ref Tier);
        serializer.SerializeValue(ref CurrentHealth);
        serializer.SerializeValue(ref MoveSpeed);
        serializer.SerializeValue(ref ATKDamage);
        serializer.SerializeValue(ref ATKSpeed);
        serializer.SerializeValue(ref ATKRange);
    }

}

public class Enemy : NetworkBehaviour
{
    [Header("Component refernce")]
    public EnemyDetector Detector;
    public EnemyMovement Movement;
    public EnemyCombat Combat;


    [Header("Eneym Type SO")]
    public EnemyTypeData_SO EnemyType;

    [Header("XP Prefab")]
    [SerializeField] private GameObject _xpPrefab;

    public NetworkVariable<EnemyCurrentStats> CurrentStats = new NetworkVariable<EnemyCurrentStats>(
        new EnemyCurrentStats(),
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    // C# event
    public event Action<EnemyCurrentStats> OnEnemyStatsChanged;
    public event Action<Enemy> OnEnemyDespawned;

    // === FSM ( Finite State-Machine ) ===
    public readonly IEnemyState IdleState = new EnemyIdleState();
    public readonly IEnemyState MoveState = new EnemyMoveState();
    public readonly IEnemyState AttackState = new EnemyAttackState();
    private IEnemyState _currentState;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        CurrentStats.OnValueChanged += OnEnemyStatsValueChanged;

        if (IsServer && EnemySpawnManager.Instance != null)
        {
            InitStats();

            Detector?.StartDetect();

            SwitchState(IdleState);
        }
        else if (IsServer) // Check if Manager not Instance
        {
            Debug.LogError("EnemySpawnManager is missing on " + gameObject.name);
        }

        // DebugLogStatsRpc();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        CurrentStats.OnValueChanged -= OnEnemyStatsValueChanged;

        if (IsServer)
        {
            Detector?.StopDetect();

            OnEnemyDespawned?.Invoke(this);
            OnEnemyDespawned = null;

            EnemyType = null;
            _currentState = null;
        }
    }

    private void OnEnemyStatsValueChanged(EnemyCurrentStats previousValue, EnemyCurrentStats newValue)
    {
        OnEnemyStatsChanged?.Invoke(newValue);
    }

    public void InitStats()
    {
        EnemyType = EnemySpawnManager.Instance.GetRandomEnemyType();
        int tierLevel = EnemySpawnManager.Instance.GetRandomEnemyTier(); // ได้เลข 1, 2, 3

        EnemyTier currentTierData = EnemyType.Setup(tierLevel);
        EnemyStats _stats = currentTierData.enemyStats;

        EnemyCurrentStats initStats = new EnemyCurrentStats
        {
            EnemyID = currentTierData.EnemyID,
            Tier = tierLevel,
            CurrentHealth = _stats.MaxHealth,
            MoveSpeed = _stats.MoveSpeed,
            ATKDamage = _stats.ATKDamage,
            ATKSpeed = _stats.ATKSpeed,
            ATKRange = _stats.ATKRange
        };
        CurrentStats.Value = initStats;
    }

    public void SwitchState(IEnemyState newState)
    {
        if (!IsServer) return;

        _currentState?.OnExit(this);
        _currentState = newState;
        _currentState?.OnEnter(this);
    }

    public bool IsPlayerInATKRange()
    {
        if (Detector == null || Detector.NearestTarget == null) return false;

        float atkRange = CurrentStats.Value.ATKRange;

        float currentSqrDistance = (Detector.NearestTarget.position - transform.position).sqrMagnitude;

        return currentSqrDistance <= (atkRange * atkRange);
    }

    private void Update()
    {
        if (!IsServer) return;

        _currentState?.OnUpdate(this);
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;

        EnemyCurrentStats stats = CurrentStats.Value;
        stats.CurrentHealth -= damage;
        if (stats.CurrentHealth <= 0)
        {
            stats.CurrentHealth = 0;
            Despawn();
        }

        CurrentStats.Value = stats;
    }

    private void Despawn()
    {
        if (!IsServer) return;

        // Spawn XP Orb
        if (_xpPrefab != null && ObjectPoolManager.Instance != null)
        {
            GameObject xpObj = ObjectPoolManager.Instance.SpawnObject(
                _xpPrefab, transform.position, Quaternion.identity, PoolCategory.XP
            );

            if (xpObj != null)
            {
                NetworkObject netObj = xpObj.GetComponent<NetworkObject>();

                if (netObj != null && !netObj.IsSpawned)
                {
                    xpObj.GetComponent<XPOrb>().Initialize(EnemyType.XPValue);
                    netObj.Spawn(true);
                }
                else
                {
                    xpObj.GetComponent<XPOrb>().Initialize(EnemyType.XPValue);
                }
            }
        }

        // Despawn enemy obj
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }



    [Rpc(SendTo.Server)]
    public void DebugLogStatsRpc()
    {
        Debug.Log($"Enemy Stats - " +
            $"EnemyID: {CurrentStats.Value.EnemyID}, " +
            $"Tier: {CurrentStats.Value.Tier}, " +
            $"Health: {CurrentStats.Value.CurrentHealth}, " +
            $"MoveSpeed: {CurrentStats.Value.MoveSpeed}, " +
            $"ATKDamage: {CurrentStats.Value.ATKDamage}, " +
            $"ATKSpeed: {CurrentStats.Value.ATKSpeed}" +
            $"ATKRange: {CurrentStats.Value.ATKRange}");
    }

    private void OnDrawGizmos()
    {
        Color color = Color.red;
        Gizmos.DrawWireSphere(transform.position, CurrentStats.Value.ATKRange);
    }
}