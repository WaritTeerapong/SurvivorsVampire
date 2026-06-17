using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    // Component Refernce
    private Rigidbody2D _rb;
    private PlayerRunTimeStats _stats; // Data
    private Animator _anim;

    private PlayerControls _inputs;
    private InputAction _moveAction;

    private Vector2 _position;


    public NetworkVariable<float> FacingDirection = new NetworkVariable<float>(
        1f,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            Camera.main.GetComponent<CameraController>().Target = transform;
        }

        if (IsServer && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.AddPlayer(transform);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.RemovePlayer(transform);
        }
    }

    void Awake()
    {
        _inputs = new PlayerControls();
        _rb = GetComponent<Rigidbody2D>();
        _stats = GetComponent<PlayerRunTimeStats>();
        _anim = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        _inputs.Enable();

        _moveAction = _inputs.Player.Move;

    }

    void OnDisable()
    {
        _inputs.Disable();
    }

    void Update()
    {
        transform.localScale = new Vector3(FacingDirection.Value, 1, 1);

        if (!IsOwner) return;

        _position = _moveAction.ReadValue<Vector2>();

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            TakeDamageRpc(10);
        }

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            _stats.DebugLogStatsRpc();
        }

        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            PlayerLevelManager.Instance.RequestGainXPRpc(100);
        }

        // Add or upgrade gun
        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            PlayerInventoryManager _inventory = GetComponent<PlayerInventoryManager>();
            if (_inventory != null) _inventory.AddOrUpgradeWeaponServerRpc("1");
        }

        // Add or upgrade machine gun
        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            PlayerInventoryManager _inventory = GetComponent<PlayerInventoryManager>();
            if (_inventory != null) _inventory.AddOrUpgradeWeaponServerRpc("2");
        }

        // Add or upgrade armour
        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            PlayerInventoryManager _inventory = GetComponent<PlayerInventoryManager>();
            if (_inventory != null) _inventory.AddOrUpgradePassiveServerRpc("1");
        }

        // Add or upgrade shoes
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            PlayerInventoryManager _inventory = GetComponent<PlayerInventoryManager>();
            if (_inventory != null) _inventory.AddOrUpgradePassiveServerRpc("2");
        }

    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        Move();
        Flip();
    }

    

    private void Move()
    {
        _rb.linearVelocity = _position * _stats.CurrentStats.Value.MoveSpeed;

        _anim.SetBool("IsMove", _rb.linearVelocity.sqrMagnitude >= 0.002f);
    }

    private void Flip()
    {
        if (_position.x > 0) FacingDirection.Value = 1f;
        else if (_position.x < 0) FacingDirection.Value = -1f;
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageRpc(int damage)
    {
        _stats.ApplyDamage(damage);

        if (DamagePopupManager.Instance != null)
        {
            DamagePopupManager.Instance.ShowPopup(transform.position, damage, true);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 2f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _stats.CurrentStats.Value.ATKRange);
    }
}

