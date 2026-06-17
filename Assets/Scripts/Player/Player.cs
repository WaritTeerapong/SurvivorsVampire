using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    // === Component Reference ===
    public PlayerRunTimeStats Stats { get; private set; }
    public PlayerInputHandler InputHandler { get; private set; }
    public PlayerMovement Movement { get; private set; }
    public PlayerCombat Combat { get; private set; }
    public PlayerDetector Detector { get; private set; }
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

    // === Property that check player can attack or not ===
    public bool IsDownOrDied => _currentState == DownedState || _currentState == DiedState;

    // === Animation Hashes ===
    public readonly int IDLE = Animator.StringToHash("PLAYER_IDLE");
    public readonly int RUN = Animator.StringToHash("PLAYER_RUN");

    void Awake()
    {
        Stats = GetComponent<PlayerRunTimeStats>();
        InputHandler = GetComponent<PlayerInputHandler>();
        Movement = GetComponent<PlayerMovement>();
        Combat = GetComponent<PlayerCombat>();
        Detector = GetComponentInChildren<PlayerDetector>();
        Anim = GetComponentInChildren<Animator>();
        SpriteRend = GetComponentInChildren<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer && PlayerManager.Instance != null) PlayerManager.Instance.AddPlayer(transform);
        if (!IsOwner) return;
        Camera.main.GetComponent<CameraController>().Target = transform;
        SwitchState(IdleState);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer && PlayerManager.Instance != null) PlayerManager.Instance.RemovePlayer(transform);
    }

    void Update()
    {
        if (!IsOwner) return;
        _currentState?.OnUpdate(this);
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        _currentState?.OnFixedUpdate(this);
    }

    public void SwitchState(IPlayerState newState)
    {
        if (!IsOwner) return;

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

        if (IsServer && PlayerManager.Instance != null) PlayerManager.Instance.RemovePlayer(transform);
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageRpc(int damage)
    {
        Stats.ApplyDamage(damage);

        if (DamagePopupManager.Instance != null)
        {
            DamagePopupManager.Instance.ShowPopup(transform.position, damage, true);
        }

        if (Stats.CurrentStats.Value.CurrentHealth <= 0 && !IsDownOrDied)
        {
            SwitchToGhostClientRpc();
        }
    }

    [Rpc(SendTo.Owner)]
    private void SwitchToGhostClientRpc()
    {
        SwitchState(DiedState);
    }
}