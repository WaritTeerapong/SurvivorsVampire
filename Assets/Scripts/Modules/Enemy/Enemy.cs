using System;
using System.Collections;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;

public struct EnemyCurrentStats : INetworkSerializable
{
    public int EnemyID;
    public int Tier;
    public int CurrentHealth;
    public float MoveSpeed;
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

public class Enemy : NetworkBehaviour, IDamageble
{
    [Header("=== Targeting ===")]
    [SerializeField] private Transform _targetPoint;
    public Transform TargetPoint => _targetPoint != null ? _targetPoint : transform;

    [Header("=== Component References ===")]
    public EnemyDetector Detector;
    public EnemyMovement Movement;
    public EnemyCombat Combat;

    [Header("=== Spawning & Setup ===")]
    public GameObject BulletPrefab;
    public EnemyTypeData_SO EnemyType;

    [Header("=== Despawn Settings ===")]
    [SerializeField] private float _clientColliderDisableDelay = 0.25f;

    public Vector2 CurrentDirection { get; private set; }

    private Animator _anim;
    private Vector3 _lastPosition;
    private bool _isDead = false;
    private Collider2D _col;

    [Header("=== Hit Flash ===")]
    public Material HitFlashMaterial;
    private Material _originalMaterial;
    private SpriteRenderer _spriteRenderer;
    private Tween _flashTween;

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

    public event Action<EnemyCurrentStats> OnEnemyStatsChanged;
    public event Action<Enemy> OnEnemyDespawned;

    // === FSM ( Finite State-Machine ) ===
    #region FSM State-Machine
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
        _col = GetComponent<Collider2D>();

        // Handle missing references safely
        // Debug.LogWarning("Animator is missing on Enemy", this);

        _spriteRenderer = _anim != null ? _anim.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer != null)
        {
            _originalMaterial = _spriteRenderer.material;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        CurrentStats.OnValueChanged += OnEnemyStatsValueChanged;
        ApplyTierColor(CurrentStats.Value);

        // Reset state for everyone to fix Client-side pooling bug
        _isDead = false;
        SetColliderTo(true);

        if (IsServer && EnemySpawnManager.Instance != null)
        {
            Detector?.StartDetect();
            SwitchState(IdleState);
        }
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

    public void SetColliderTo(bool isEnable)
    {
        if (_col != null)
        {
            _col.enabled = isEnable;
        }
    }

    private void OnEnemyStatsValueChanged(EnemyCurrentStats previousValue, EnemyCurrentStats newValue)
    {
        if (newValue.CurrentHealth < previousValue.CurrentHealth)
        {
            if (_spriteRenderer != null && HitFlashMaterial != null)
            {
                _flashTween?.Kill();
                _spriteRenderer.material = HitFlashMaterial;

                _flashTween = DOVirtual.DelayedCall(0.15f, () =>
                {
                    if (_spriteRenderer != null)
                    {
                        _spriteRenderer.material = _originalMaterial;
                    }
                }).SetLink(gameObject);
            }
        }
        else if (previousValue.Tier != newValue.Tier || previousValue.EnemyID != newValue.EnemyID)
        {
            ApplyTierColor(newValue);
        }

        OnEnemyStatsChanged?.Invoke(newValue);
    }

    public void InitStats(EnemyTypeData_SO enemyType, int tierLevel)
    {
        EnemyType = enemyType;
        EnemyTier currentTierData = EnemyType.Setup(tierLevel);
        EnemyStats stats = currentTierData.enemyStats;
        Color color = currentTierData.color;

        EnemyCurrentStats initStats = new EnemyCurrentStats
        {
            EnemyID = currentTierData.EnemyID,
            Tier = tierLevel,
            CurrentHealth = stats.MaxHealth,
            MoveSpeed = stats.MoveSpeed,
            ATKDamage = stats.ATKDamage,
            ATKSpeed = stats.ATKSpeed,
            ATKRange = stats.ATKRange,
            ColorR = color.r,
            ColorG = color.g,
            ColorB = color.b
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

        Player p = Detector.NearestTarget.GetComponent<Player>();
        Vector3 targetPos = p != null ? p.TargetPoint.position : Detector.NearestTarget.position;
        float currentSqrDistance = (targetPos - TargetPoint.position).sqrMagnitude;

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
        if (_anim != null) _anim.Play(animation);
    }

    private void Despawn()
    {
        if (!IsServer) return;
        SwitchState(DieState);

        if (XPDropManager.Instance != null && EnemyType != null)
        {
            XPDropManager.Instance.DropXP(transform.position, EnemyType.XPValue);
        }

        PlayDeathVFXClientRpc(transform.position);
        DisableColliderRpc();
        StartCoroutine(DelayDespawnRoutine(1.2f));
    }

    [Rpc(SendTo.Everyone)]
    private void DisableColliderRpc()
    {
        if (IsServer)
        {
            // Server disables immediately to stop logic processing
            SetColliderTo(false);
        }
        else
        {
            // Client delays disabling to allow local bullets to hit and trigger VFX
            DOVirtual.DelayedCall(_clientColliderDisableDelay, () =>
            {
                if (this != null && gameObject != null && gameObject.activeInHierarchy)
                {
                    SetColliderTo(false);
                }
            }).SetLink(gameObject);
        }
    }

    private IEnumerator DelayDespawnRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(false);
        }

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayDeathVFXClientRpc(Vector3 position)
    {
        if (EnemyType != null && EnemyType.DeathVFXPrefab != null)
        {
            ParticleSystem ps = ObjectPoolManager.Instance.SpawnObject<ParticleSystem>(
                EnemyType.DeathVFXPrefab,
                position,
                Quaternion.identity,
                PoolCategory.VFX
            );
            if (ps != null)
            {
                ps.Play();
            }
        }
    }

    private void ApplyTierColor(EnemyCurrentStats stat)
    {
        if (_anim == null) return;

        SpriteRenderer renderer = _anim.GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (renderer == null) return;

        if (stat.ColorR == 0 && stat.ColorG == 0 && stat.ColorB == 0)
        {
            renderer.color = Color.white;
            return;
        }

        renderer.color = new Color(stat.ColorR, stat.ColorG, stat.ColorB, 1f);
    }

    private void FacingToDirection()
    {
        transform.localScale = new Vector3(FacingDirection.Value, 1, 1);

        if (!IsServer) return;

        Vector3 positionDelta = Vector3.zero;

        if (Detector != null && Detector.NearestTarget != null)
        {
            positionDelta = Detector.NearestTarget.position - transform.position;
        }
        else
        {
            positionDelta = transform.position - _lastPosition;
        }

        if (positionDelta.x > 0.001f) FacingDirection.Value = 1f;
        else if (positionDelta.x < -0.001f) FacingDirection.Value = -1f;

        Vector2 moveDir = transform.position - _lastPosition;
        if (moveDir != Vector2.zero)
        {
            CurrentDirection = moveDir.normalized;
        }

        _lastPosition = transform.position;
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
            Vector3 spawnPos = TargetPoint.position;

            Bullet bulletObj = ObjectPoolManager.Instance.SpawnObject<Bullet>(BulletPrefab, spawnPos, Quaternion.identity, PoolCategory.Projectiles);
            if (bulletObj != null)
            {
                bulletObj.IsEnemy = true;
                GameObject hitVFX = null;
                if (EnemyType != null) hitVFX = EnemyType.BulletHitVFXPrefab;

                Transform aimTarget = targetObj.transform;
                if (targetObj.TryGetComponent<IDamageble>(out IDamageble d)) aimTarget = d.TargetPoint;

                bulletObj.Initialize(aimTarget, CurrentStats.Value.ATKDamage, hitVFX);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(TargetPoint != null ? TargetPoint.position : transform.position, CurrentStats.Value.ATKRange);
    }
}