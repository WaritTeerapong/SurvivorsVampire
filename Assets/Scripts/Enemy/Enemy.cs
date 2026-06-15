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
    public float ColorR;
    public float ColorG;
    public float ColorB;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref EnemyID);
        serializer.SerializeValue(ref Tier);
        serializer.SerializeValue(ref CurrentHealth);
        serializer.SerializeValue(ref MoveSpeed);
        serializer.SerializeValue(ref ATKDamage);
        serializer.SerializeValue(ref ATKSpeed);
        serializer.SerializeValue(ref ATKRange);
        serializer.SerializeValue(ref ColorR);
        serializer.SerializeValue(ref ColorG);
        serializer.SerializeValue(ref ColorB);
    }

}

public class Enemy : NetworkBehaviour
{
    [Header("Component refernce")]
    public EnemyDetector Detector;
    public EnemyMovement Movement;
    public EnemyCombat Combat;

    [Header("Bullet Prefab")]
    public GameObject BulletPrefab;

    [Header("Eneym Type SO")]
    public EnemyTypeData_SO EnemyType;

    private Animator _anim;
    private Vector3 _lastPosition;
    private bool _isDead = false;

    public NetworkVariable<EnemyCurrentStats> CurrentStats = new NetworkVariable<EnemyCurrentStats>(
        new EnemyCurrentStats(),
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> FacingDirection = new NetworkVariable<float>(
        1f,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    // C# event
    public event Action<EnemyCurrentStats> OnEnemyStatsChanged;
    public event Action<Enemy> OnEnemyDespawned;

    // === FSM ( Finite State-Machine ) ===
    #region     FSM State-Machine
    public readonly IEnemyState IdleState = new EnemyIdleState();
    public readonly IEnemyState MoveState = new EnemyMoveState();
    public readonly IEnemyState AttackState = new EnemyAttackState();
    public readonly IEnemyState DieState = new EnemyDieState();
    private IEnemyState _currentState;
    #endregion

    public static readonly int IDLE = Animator.StringToHash("Idle");
    public static readonly int RUN = Animator.StringToHash("Run");
    public static readonly int ATK = Animator.StringToHash("Attack");
    public static readonly int DIE = Animator.StringToHash("Die");

    private void Awake()
    {
        _anim = GetComponentInChildren<Animator>();

        OnEnemyStatsChanged += ApplyTierColor;
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        CurrentStats.OnValueChanged += OnEnemyStatsValueChanged;

        ApplyTierColor(CurrentStats.Value);

        if (IsServer && EnemySpawnManager.Instance != null)
        {

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
        OnEnemyStatsChanged -= ApplyTierColor;
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

    public void InitStats(EnemyTypeData_SO enemyType, int tierLevel)
    {
        EnemyType = enemyType;

        EnemyTier currentTierData = EnemyType.Setup(tierLevel);
        EnemyStats _stats = currentTierData.enemyStats;
        Color _color = currentTierData.color;

        EnemyCurrentStats initStats = new EnemyCurrentStats
        {
            EnemyID = currentTierData.EnemyID,
            Tier = tierLevel,
            CurrentHealth = _stats.MaxHealth,
            MoveSpeed = _stats.MoveSpeed,
            ATKDamage = _stats.ATKDamage,
            ATKSpeed = _stats.ATKSpeed,
            ATKRange = _stats.ATKRange,
            ColorR = _color.r,
            ColorG = _color.g,
            ColorB = _color.b
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
        if (_isDead) return;
        FacingToDirection();
        _currentState?.OnUpdate(this);
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;

        if (DamagePopupManager.Instance != null)
        {
            DamagePopupManager.Instance.ShowPopup(transform.position, damage, false);
        }

        EnemyCurrentStats stats = CurrentStats.Value;
        stats.CurrentHealth -= damage;
        if (stats.CurrentHealth <= 0)
        {
            _isDead = true;
            stats.CurrentHealth = 0;
            Despawn();
        }

        CurrentStats.Value = stats;
    }

    public void PlayAnimation(int animation)
    {
        if (!IsServer) return;
        _anim.Play(animation);
    }

    private void Despawn()
    {
        if (!IsServer) return;
        SwitchState(DieState);
        // Spawn XP Orb
        if (XPDropManager.Instance != null && EnemyType != null)
        {
            XPDropManager.Instance.DropXP(transform.position, EnemyType.XPValue);
        }

        // Despawn enemy obj after 1.2 s
        StartCoroutine(DelayDespawnRoutine(1.2f));
    }
    private IEnumerator DelayDespawnRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
    private void ApplyTierColor(EnemyCurrentStats _stat)
    {
        if (_anim == null) return;

        SpriteRenderer renderer = _anim.GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (renderer == null) return;

        if (_stat.ColorR == 0 && _stat.ColorG == 0 && _stat.ColorB == 0)
        {
            renderer.color = Color.white;
            return;
        }
        renderer.color = new Color(_stat.ColorR, _stat.ColorG, _stat.ColorB, 1f);

    }

    private void FacingToDirection()
    {
        transform.localScale = new Vector3(FacingDirection.Value, 1, 1);

        if (!IsServer) return;
        Vector3 positionDelta = Vector3.zero;

        // If found target, face to target 
        if (Detector != null && Detector.NearestTarget != null)
        {
            positionDelta = Detector.NearestTarget.position - transform.position;
        }

        // If not found target, face to where you move
        if (Detector.NearestTarget == null)
        {
            positionDelta = transform.position - _lastPosition;
        }

        if (positionDelta.x > 0.001f) FacingDirection.Value = 1f;
        else if (positionDelta.x < -0.001f) FacingDirection.Value = -1f;
        _lastPosition = transform.position;

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

    [Rpc(SendTo.Server)]
    public void RequestFireRpc()
    {
        if (Detector != null && Detector.NearestTarget != null)
        {
            NetworkObject targetNetObj = Detector.NearestTarget.GetComponent<NetworkObject>();
            if (targetNetObj != null)
            {
                EnemyFireRpc(targetNetObj.NetworkObjectId);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void EnemyFireRpc(ulong targetNetworkId)
    {
        if (BulletPrefab == null || ObjectPoolManager.Instance == null) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out NetworkObject targetObj))
        {
            Vector3 spawnPos = transform.position;

            GameObject bulletObj = ObjectPoolManager.Instance.SpawnObject(BulletPrefab, spawnPos, Quaternion.identity, PoolCategory.Projectiles);

            if (bulletObj != null)
            {
                Bullet bulletScript = bulletObj.GetComponent<Bullet>();
                if (bulletScript != null)
                {
                    bulletScript.IsEnemy = true;
                    bulletScript.Initialize(targetObj.transform, CurrentStats.Value.ATKDamage);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, CurrentStats.Value.ATKRange);
    }
}