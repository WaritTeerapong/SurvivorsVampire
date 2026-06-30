using System;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

public class Boss : NetworkBehaviour
{
    [Header("=== Data ===")]
    [SerializeField] private BossTypeData_SO _bossData;

    [Header("=== Components ===")]
    [SerializeField] private EnemyDetector _detector;
    [SerializeField] private BossMovement _movement;
    [SerializeField] private BossCombat _combat;

    public BossTypeData_SO BossData => _bossData;
    public EnemyDetector Detector => _detector;
    public BossMovement Movement => _movement;
    public BossCombat Combat => _combat;

    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>();
    public NetworkVariable<int> CurrentPhase = new NetworkVariable<int>(1);

    public event Action<int, int> OnBossHealthChanged;
    public event Action<int> OnPhaseChanged;

    // FSM 
    public readonly IBossState ChaseState = new BossChaseState();
    public readonly IBossState AOEState = new BossChaseState();
    public readonly IBossState SpawnState = new BossChaseState();
    public readonly IBossState TransitionState = new BossChaseState();

    private IBossState _currentState;
    private bool _isDead = false;

    public float AOETimer { get; set; }
    public float SpawnTimer { get; set; }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        CurrentHealth.OnValueChanged += HandleHealthChanged;
        CurrentPhase.OnValueChanged += HandlePhaseChanged;

        if (IsServer && _bossData != null)
        {
            CurrentHealth.Value = _bossData.MaxHealth;
            CurrentPhase.Value = 1;

            AOETimer = _bossData.P1_AOECooldown;
            SpawnTimer = _bossData.P1_SpawnCooldown;

            if (_detector != null) _detector.StartDetect();
            SwitchState(ChaseState);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        CurrentHealth.OnValueChanged -= HandleHealthChanged;
        CurrentPhase.OnValueChanged -= HandlePhaseChanged;
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

        if (_currentState == ChaseState)
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
}