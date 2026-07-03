using System;
using Unity.Netcode;
using UnityEngine;

public class Boss : NetworkBehaviour
{
    public static event Action<Boss> OnBossSpawnedGlobal;
    public static event Action<Boss> OnBossDespawnedGlobal;

    [Header("=== Targeting ===")]
    [SerializeField] private Transform _targetPoint;
    public Transform TargetPoint => _targetPoint != null ? _targetPoint : transform;

    [Header("=== Data ===")]
    [SerializeField] private BossTypeData_SO _bossData;

    [Header("=== Components ===")]
    [SerializeField] private EnemyDetector _detector;
    [SerializeField] private BossMovement _movement;
    [SerializeField] private BossCombat _combat;

    private Animator _anim;

    public BossTypeData_SO BossData => _bossData;
    public EnemyDetector Detector => _detector;
    public BossMovement Movement => _movement;
    public BossCombat Combat => _combat;

    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>();
    public NetworkVariable<int> CurrentPhase = new NetworkVariable<int>(1);

    public event Action<int, int> OnBossHealthChanged;
    public event Action<int> OnPhaseChanged;
    public event Action OnBossDied;
    public event Action OnBossSpawned;

    // FSM 
    public readonly IBossState IdleState = new BossIdleState();
    public readonly IBossState ChaseState = new BossChaseState();
    public readonly IBossState AttackState = new BossAttackState();
    public readonly IBossState AOEState = new BossAOEState();
    public readonly IBossState SpawnState = new BossSpawnState();
    public readonly IBossState TransitionState = new BossTransitionState();

    private IBossState _currentState;
    private bool _isDead = false;

    public float AOETimer { get; set; }
    public float SpawnTimer { get; set; }

    // Animation Hashes
    public readonly int IDLE = Animator.StringToHash("IDLE");
    public readonly int CHASE = Animator.StringToHash("CHASE");
    public readonly int RANGED = Animator.StringToHash("RANGED");
    public readonly int MELEE = Animator.StringToHash("MELEE");
    public readonly int AOE = Animator.StringToHash("AOE");
    public readonly int SPAWN = Animator.StringToHash("SPAWN");

    void Awake()
    {
        _anim = GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        CurrentHealth.OnValueChanged += HandleHealthChanged;
        CurrentPhase.OnValueChanged += HandlePhaseChanged;

        OnBossSpawnedGlobal?.Invoke(this);

        if (IsServer && _bossData != null)
        {
            CurrentHealth.Value = _bossData.MaxHealth;
            CurrentPhase.Value = 1;

            AOETimer = _bossData.P1_AOECooldown;
            SpawnTimer = _bossData.P1_SpawnCooldown;

            if (_detector != null) _detector.StartDetect();
            SwitchState(IdleState);
            OnBossSpawned?.Invoke();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        CurrentHealth.OnValueChanged -= HandleHealthChanged;
        CurrentPhase.OnValueChanged -= HandlePhaseChanged;

        OnBossDespawnedGlobal?.Invoke(this);

        if (IsServer && _detector != null) _detector.StopDetect();
    }

    private void HandlePhaseChanged(int previousValue, int newValue)
    {
        OnPhaseChanged?.Invoke(newValue);
    }

    private void HandleHealthChanged(int previousValue, int newValue)
    {
        OnBossHealthChanged?.Invoke(newValue, _bossData.MaxHealth);

        if (IsServer && CurrentPhase.Value == 1 && newValue <= _bossData.MaxHealth / 2)
        {
            CurrentPhase.Value = 2;
            SwitchState(TransitionState);
        }
    }

    void Update()
    {
        if (!IsServer || _isDead) return;

        if (_currentState == ChaseState || _currentState == IdleState)
        {
            AOETimer -= Time.deltaTime;
            SpawnTimer -= Time.deltaTime;
        }

        _currentState?.OnUpdate(this);
    }

    public void SwitchState(IBossState newState)
    {
        if (!IsServer) return;

        _currentState?.OnExit(this);
        _currentState = newState;
        _currentState?.OnEnter(this);
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer || _isDead) return;

        if (DamagePopupManager.Instance != null)
        {
            DamagePopupManager.Instance.ShowPopup(transform.position, damage, false);
        }

        CurrentHealth.Value -= damage;
        if (CurrentHealth.Value <= 0)
        {
            CurrentHealth.Value = 0;
            _isDead = true;
            DespawnBoss();
        }
    }

    private void DespawnBoss()
    {
        if (!IsServer) return;

        OnBossDied?.Invoke();
        PlayDeathVFXRpc(transform.position);
        NetworkObject.Despawn(true);
    }

    [Rpc(SendTo.Everyone)]
    private void PlayDeathVFXRpc(Vector3 position)
    {
        if (_bossData != null && _bossData.DeathVFXPrefab != null && ObjectPoolManager.Instance != null)
        {
            ParticleSystem ps = ObjectPoolManager.Instance.SpawnObject<ParticleSystem>(_bossData.DeathVFXPrefab, position, Quaternion.identity, PoolCategory.VFX);
            if (ps != null) ps.Play();
        }
    }

    public void PlayAnimation(int animationHash)
    {
        if (!IsServer || _anim == null) return;
        _anim.Play(animationHash);
    }

    private void OnDrawGizmosSelected()
    {
        if (_bossData == null) return;

        Vector3 centerPos = TargetPoint != null ? TargetPoint.position : transform.position;

        // Draw Melee Attack Range (Red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centerPos, _bossData.MeleeAttackRange);

        // Draw Ranged Attack Range (Orange)
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(centerPos, _bossData.RangedAttackRange);
    }
}