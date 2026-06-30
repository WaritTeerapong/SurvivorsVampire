using System;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : NetworkBehaviour
{
    #region Component Reference
    [Header("=== Component Reference ===")]
    public PlayerRunTimeStats Stats { get; private set; }
    public PlayerInputHandler InputHandler { get; private set; }
    public PlayerMovement Movement { get; private set; }
    public PlayerReviveHandler Revive { get; private set; }
    public PlayerInventory Inventory { get; private set; } // for debug
    public Animator Anim { get; private set; }
    public SpriteRenderer SpriteRend { get; private set; }
    #endregion

    #region Ghost Mode
    [Header("=== Ghost Mode ===")]
    public GameObject GravestonePrefab;
    private GameObject _myGravestone;
    #endregion

    #region FSM States
    public readonly IPlayerState IdleState = new PlayerIdleState();
    public readonly IPlayerState MoveState = new PlayerMoveState();
    public readonly IPlayerState DownedState = new PlayerDownedState();
    public readonly IPlayerState DiedState = new PlayerDiedState();

    private IPlayerState _currentState;
    public IPlayerState CurrentState => _currentState;
    #endregion

    #region Downed Checker
    public bool IsDowned => _currentState == DownedState;
    public bool IsDownOrDied => _currentState == DownedState || _currentState == DiedState;
    #endregion

    #region NetworkVariable
    [Header("=== Revive & Die Settings ===")]
    public NetworkVariable<float> DiedTimer = new NetworkVariable<float>(
        10f,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> ReviveTimer = new NetworkVariable<float>(
        3f,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsBeingRevived = new NetworkVariable<bool>(
        false,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );
    #endregion

    #region Revive System
    private int _playersInReviveZone = 0;

    private float _reviveScanTimer = 0f;
    private const float REVIVE_SCAN_INTERVAL = 0.1f;
    private Collider2D[] _reviveScanResults = new Collider2D[2];

    private ContactFilter2D _playerScanFilter;
    private bool _isFilterInitialized = false;

    public LayerMask PlayerLayer;
    #endregion

    #region Animation Hash
    public readonly int IDLE = Animator.StringToHash("PLAYER_IDLE");
    public readonly int RUN = Animator.StringToHash("PLAYER_RUN");
    public readonly int DOWN = Animator.StringToHash("PLAYER_DOWNED");
    public readonly int GHOST_IDLE = Animator.StringToHash("PLAYER_GHOST_IDLE");
    public readonly int GHOST_RUN = Animator.StringToHash("PLAYER_GHOST_RUN");
    #endregion

    void Awake()
    {
        Stats = GetComponent<PlayerRunTimeStats>();
        InputHandler = GetComponent<PlayerInputHandler>();
        Movement = GetComponent<PlayerMovement>();
        Revive = GetComponentInChildren<PlayerReviveHandler>();
        Anim = GetComponentInChildren<Animator>();
        SpriteRend = GetComponentInChildren<SpriteRenderer>();
        Inventory = GetComponent<PlayerInventory>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (PlayerManager.Instance != null) PlayerManager.Instance.AddPlayer(this);
        if (IsOwner) SwitchState(IdleState);

        bool isWaitingRoom = gameObject.scene.name == "WaitingRoomScene" || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "WaitingRoomScene";

        if (isWaitingRoom)
        {
            var healthUI = GetComponentInChildren<PlayerHealthBarUI>();
            if (healthUI != null) healthUI.gameObject.SetActive(false);
        }

        if (!IsOwner) return;

        if (!isWaitingRoom)
        {
            CameraController camController = Camera.main.GetComponent<CameraController>();
            if (camController != null)
            {
                camController.Target = transform;
            }
        }

        Stats.CurrentStats.OnValueChanged += OnPlayerStatsChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.RemoveActiveTarget(transform);

            if (PlayerManager.Instance.AllPlayers.Contains(this))
            {
                PlayerManager.Instance.AllPlayers.Remove(this);
            }
        }

        Stats.CurrentStats.OnValueChanged -= OnPlayerStatsChanged;
    }

    private void OnPlayerStatsChanged(PlayerStats previousValue, PlayerStats newValue)
    {
        if (newValue.CurrentHealth < previousValue.CurrentHealth)
        {
            if (SpriteRend != null && !IsDownOrDied)
            {
                SpriteRend.DOKill();
                SpriteRend.color = Color.red;
                SpriteRend.DOColor(Color.white, 0.15f);
            }

            if (IsOwner && CameraController.Instance != null)
            {
                CameraController.Instance.TriggerShake(0.2f, 0.4f);
            }
        }
    }

    void Update()
    {
        if (IsServer && _currentState == DownedState)
        {
            ReviveCheck();
        }

        if (!IsOwner) return;
        _currentState?.OnUpdate(this);

#if UNITY_EDITOR
        // === Debug Controls ===
        if (Keyboard.current.tKey.wasPressedThisFrame) TakeDamageRpc(10);
        if (Keyboard.current.yKey.wasPressedThisFrame) TakeDamageRpc(9999);
        if (Keyboard.current.uKey.wasPressedThisFrame) SwitchToIdleRpc();

        if (Keyboard.current.nKey.wasPressedThisFrame) Inventory.AddOrUpgradeWeaponRpc("w1");
        if (Keyboard.current.mKey.wasPressedThisFrame) Inventory.AddOrUpgradeWeaponRpc("w2");
        if (Keyboard.current.oKey.wasPressedThisFrame) Inventory.AddOrUpgradePassiveRpc("p1");
        if (Keyboard.current.pKey.wasPressedThisFrame) Inventory.AddOrUpgradePassiveRpc("p2");

        if (Keyboard.current.lKey.wasPressedThisFrame) PlayerLevelManager.Instance.SharedLevel.Value += 1;
        if (Keyboard.current.kKey.wasPressedThisFrame) PlayerLevelManager.Instance.RequestGainXPRpc(100);
#endif
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        _currentState?.OnFixedUpdate(this);
    }

    public event Action<IPlayerState> OnStateChanged;

    public void SwitchState(IPlayerState newState)
    {
        _currentState?.OnExit(this);
        _currentState = newState;
        _currentState?.OnEnter(this);
        OnStateChanged?.Invoke(newState);
    }

    public void PlayAnimation(int hash)
    {
        if (Anim.enabled) Anim.CrossFade(hash, 0.1f);
    }

    public void BecomeGhost()
    {
        if (Anim != null)
        {
            Anim.enabled = true;
            PlayAnimation(GHOST_IDLE);
        }
        if (Revive != null) Revive.gameObject.SetActive(false);

        if (IsServer)
        {
            if (PlayerManager.Instance != null) PlayerManager.Instance.RemoveActiveTarget(transform);

            if (GravestonePrefab != null && _myGravestone == null)
            {
                _myGravestone = ObjectPoolManager.Instance.SpawnObject<GameObject>(GravestonePrefab, transform.position, Quaternion.identity);

                if (_myGravestone.TryGetComponent<NetworkObject>(out var netObj) && !netObj.IsSpawned)
                {
                    netObj.Spawn(true);
                }
            }
        }
    }

    public void ResetDownedState()
    {
        if (!IsServer) return;
        DiedTimer.Value = 10f;
        ReviveTimer.Value = 3f;
        IsBeingRevived.Value = false;
        _playersInReviveZone = 0;
    }

    // Server-side revive progression check
    public void ReviveCheck()
    {
        if (!IsServer) return;

        _reviveScanTimer -= Time.deltaTime;
        if (_reviveScanTimer <= 0)
        {
            _reviveScanTimer = REVIVE_SCAN_INTERVAL;
            _playersInReviveZone = PreformReviveScan();
            IsBeingRevived.Value = _playersInReviveZone > 0;
        }

        if (_playersInReviveZone > 0)
        {
            ReviveTimer.Value -= Time.deltaTime;

            if (ReviveTimer.Value <= 0)
            {
                Stats.ResetHealthToMax();
                ReviveTimer.Value = 3f;
                _playersInReviveZone = 0;
                IsBeingRevived.Value = false;
                RevivePlayerRpc(isReviveOnFullHealth: true);
            }
        }
        else
        {
            if (ReviveTimer.Value < 3f)
            {
                ReviveTimer.Value = 3f;
            }

            DiedTimer.Value -= Time.deltaTime;

            if (DiedTimer.Value <= 0)
            {
                SwitchToGhostRpc();
                DiedTimer.Value = 10f;
            }
        }
    }

    private int PreformReviveScan()
    {
        if (Revive == null) return 0;

        if (!_isFilterInitialized)
        {
            _playerScanFilter = new ContactFilter2D();
            _playerScanFilter.useLayerMask = true;
            _playerScanFilter.layerMask = PlayerLayer;
            _playerScanFilter.useTriggers = true;

            _isFilterInitialized = true;
        }

        int count = 0;

        int hitCount = Physics2D.OverlapCircle(transform.position, Revive.ReviveZoneRadius, _playerScanFilter, _reviveScanResults);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = _reviveScanResults[i];

            if (hit.CompareTag("Player"))
            {
                Player otherPlayer = hit.GetComponent<Player>();

                if (otherPlayer != null && otherPlayer != this && !otherPlayer.IsDownOrDied)
                {
                    count++;
                }
            }
        }
        return count;
    }

    // ดึง Function นี้ไปใช้กับ Card ได้เลย
    public void RespawnFromCard()
    {
        if (!IsServer) return;

        if (_myGravestone != null)
        {
            transform.position = _myGravestone.transform.position;

            if (_myGravestone.TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
            {
                netObj.Despawn(false);
            }
            ObjectPoolManager.Instance.ReturnObjectToPool(_myGravestone);
            _myGravestone = null;
        }

        RevivePlayerRpc();
    }

    [Rpc(SendTo.Everyone)]
    public void ForceGhostRpc()
    {
        // ถ้ากำลังนอนรอคนมาชุบอยู่ (Downed) ให้เปลี่ยนเป็นตายจริง (Died/ผี) ทันที
        if (_currentState == DownedState)
        {
            SwitchState(DiedState);
        }
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageRpc(int damage)
    {
        if (IsDownOrDied) return;

        Stats.ApplyDamage(damage);

        if (DamagePopupManager.Instance != null)
        {
            DamagePopupManager.Instance.ShowPopup(transform.position, damage, true);
        }

        if (Stats.CurrentStats.Value.CurrentHealth <= 0 && !IsDowned)
        {
            SwitchToDownedRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RevivePlayerRpc(bool isReviveOnFullHealth = false, float healAmount = 0.5f)
    {
        if (!IsServer) return;

        if (isReviveOnFullHealth)
        {
            Stats.ResetHealthToMax();
        }
        else
        {
            Stats.ResetHealthToPercent(healAmount);
        }

        if (PlayerManager.Instance != null && !PlayerManager.Instance.ActiveTargets.Contains(transform))
        {
            PlayerManager.Instance.ActiveTargets.Add(transform);
        }

        SwitchToIdleRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void SwitchToDownedRpc()
    {
        SwitchState(DownedState);
    }

    [Rpc(SendTo.Everyone)]
    private void SwitchToGhostRpc()
    {
        SwitchState(DiedState);
    }

    [Rpc(SendTo.Everyone)]
    public void SwitchToIdleRpc()
    {
        SwitchState(IdleState);
    }


}