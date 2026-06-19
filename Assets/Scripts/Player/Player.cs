using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : NetworkBehaviour
{
    [Header("=== Component Reference ===")]
    public PlayerRunTimeStats Stats { get; private set; }
    public PlayerInputHandler InputHandler { get; private set; }
    public PlayerMovement Movement { get; private set; }
    public PlayerReviveHandler Revive { get; private set; }
    public Animator Anim { get; private set; }
    public SpriteRenderer SpriteRend { get; private set; }

    [Header("=== Ghost Mode Sprite ===")]
    public Sprite GhostSprite;

    // === FSM States ===
    public readonly IPlayerState IdleState = new PlayerIdleState();
    public readonly IPlayerState MoveState = new PlayerMoveState();
    public readonly IPlayerState DownedState = new PlayerDownedState();
    public readonly IPlayerState DiedState = new PlayerDiedState();
    private IPlayerState _currentState;

    public bool IsDowned => _currentState == DownedState;
    public bool IsDownOrDied => _currentState == DownedState || _currentState == DiedState;

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

    private int _playersInReviveZone = 0;

    // === Animation Hashes ===
    public readonly int IDLE = Animator.StringToHash("PLAYER_IDLE");
    public readonly int RUN = Animator.StringToHash("PLAYER_RUN");
    // public readonly int DOWN = Animator.StringToHash("PLAYER_DOWN");
    // public readonly int DIED = Animator.StringToHash("PLAYER_DIED");

    void Awake()
    {
        Stats = GetComponent<PlayerRunTimeStats>();
        InputHandler = GetComponent<PlayerInputHandler>();
        Movement = GetComponent<PlayerMovement>();
        Revive = GetComponentInChildren<PlayerReviveHandler>();
        Anim = GetComponentInChildren<Animator>();
        SpriteRend = GetComponentInChildren<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer && PlayerManager.Instance != null) PlayerManager.Instance.AddPlayer(this);

        if (!IsOwner) return;
        Camera.main.GetComponent<CameraController>().Target = transform;
        SwitchState(IdleState);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.RemoveActiveTarget(transform);

            if (PlayerManager.Instance.AllPlayers.Contains(this))
            {
                PlayerManager.Instance.AllPlayers.Remove(this);
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

        // === Debug Controls ===
        if (Keyboard.current.tKey.wasPressedThisFrame) TakeDamageRpc(10);
        if (Keyboard.current.yKey.wasPressedThisFrame) TakeDamageRpc(9999);
        if (Keyboard.current.uKey.wasPressedThisFrame) SwitchToIdleRpc();
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        _currentState?.OnFixedUpdate(this);
    }

    public void SwitchState(IPlayerState newState)
    {
        _currentState?.OnExit(this);
        _currentState = newState;
        _currentState?.OnEnter(this);
    }

    public void PlayAnimation(int hash)
    {
        if (Anim.enabled) Anim.CrossFade(hash, 0.1f);
    }

    public void BecomeGhostRpc()
    {
        if (Anim != null) Anim.enabled = false;
        if (SpriteRend != null && GhostSprite != null) SpriteRend.sprite = GhostSprite;
        if (IsServer && PlayerManager.Instance != null) PlayerManager.Instance.RemoveActiveTarget(transform);

        if (Revive != null) Revive.gameObject.SetActive(false);
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

        if (_playersInReviveZone > 0)
        {
            ReviveTimer.Value -= Time.deltaTime;
            Debug.Log($"[Debug] Reviving... Time left: {ReviveTimer.Value:F1}s");

            if (ReviveTimer.Value <= 0)
            {
                Stats.ResetHealthToMax();
                ReviveTimer.Value = 3f;
                _playersInReviveZone = 0;
                IsBeingRevived.Value = false;
                SwitchToIdleRpc();
            }
        }
        else
        {
            DiedTimer.Value -= Time.deltaTime;
            Debug.Log($"[Debug] Dying... Time left: {DiedTimer.Value:F1}s");

            if (DiedTimer.Value <= 0)
            {
                SwitchToGhostRpc();
                DiedTimer.Value = 10f;
            }
        }
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void UpdateReviverCountServerRpc(int amount)
    {
        if (!IsServer) return;

        _playersInReviveZone += amount;
        if (_playersInReviveZone < 0) _playersInReviveZone = 0;

        IsBeingRevived.Value = _playersInReviveZone > 0;

        if (_playersInReviveZone == 0)
        {
            ReviveTimer.Value = 3f;
        }
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageRpc(int damage)
    {
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