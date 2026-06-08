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
    public EnemyDetector Detector;
    public EnemyMovement Movement;
    public EnemyCombat Combat;

    [Header("Eneym Type SO")]
    public EnemyTypeData_SO EnemyType;

    public NetworkVariable<EnemyCurrentStats> CurrentStats = new NetworkVariable<EnemyCurrentStats>(
        new EnemyCurrentStats(),
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    public event Action<EnemyCurrentStats> OnEnemyStatsChanged;
    public event Action OnEnemyDespawned;

    public readonly IEnemyState IdleState = new EnemyIdleState();
    public readonly IEnemyState MoveState = new EnemyMoveState();
    public readonly IEnemyState AttackState = new EnemyAttackState();

    private IEnemyState _currentState;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        CurrentStats.OnValueChanged += OnEnemyStatsValueChanged;

        // แก้ไข: เอา EnemyType == null ออกไป เพราะเราจะสุ่มค่าให้มันใหม่เสมอ
        if (IsServer && EnemySpawnManager.Instance != null)
        {
            StartCoroutine(InitStats());

            Detector?.StartDetect();

            SwitchState(IdleState);
        }
        else if (IsServer) // ดัก Error ไว้เผื่อกรณีหา Manager ไม่เจอ
        {
            Debug.LogError("EnemySpawnManager is missing on " + gameObject.name);
        }

        DebugLogStatsRpc();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        CurrentStats.OnValueChanged -= OnEnemyStatsValueChanged;

        if (IsServer)
        {
            Detector?.StopDetect();

            OnEnemyDespawned?.Invoke();
            OnEnemyDespawned = null;

            // เพิ่มบรรทัดนี้: เคลียร์ค่าทิ้งตอนเก็บเข้า Pool
            EnemyType = null;
            _currentState = null;
        }
    }

    private void OnEnemyStatsValueChanged(EnemyCurrentStats previousValue, EnemyCurrentStats newValue)
    {
        OnEnemyStatsChanged?.Invoke(newValue);
    }

    public IEnumerator InitStats()
    {
        yield return null;
        // Random Type & Tier
        EnemyType = EnemySpawnManager.Instance.GetRandomEnemyType();
        int tierIndex = EnemySpawnManager.Instance.GetRandomEnemyTier();

        // Enemy Stats
        EnemyStats _stats = EnemyType.enemyTiers[tierIndex].enemyStats;

        // Assign Stats
        EnemyCurrentStats initStats = new EnemyCurrentStats
        {
            EnemyID = EnemyType.enemyTiers[tierIndex].EnemyID,
            Tier = tierIndex,
            CurrentHealth = _stats.MaxHealth,
            MoveSpeed = _stats.MoveSpeed,
            ATKDamage = _stats.ATKDamage,
            ATKSpeed = _stats.ATKSpeed,
            ATKRange = _stats.ATKRange
        };
        CurrentStats.Value = initStats;
    }

    private void ResetStats()
    {
        EnemyType = null;
        CurrentStats = null;
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
        // Only the Server calculates health and damage
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
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            // 'true' will destroy the GameObject in the scene along with despawning it
            NetworkObject.Despawn(true);
        }
    }



    [Rpc(SendTo.Server)]
    public void DebugLogStatsRpc()
    {
        Debug.Log($"Enemy Stats - " +
            $"EnemyID: {CurrentStats.Value.EnemyID}, " +
            $"TIer: {CurrentStats.Value.Tier}, " +
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