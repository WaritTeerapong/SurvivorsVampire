using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    // Component Refernce
    private Rigidbody2D _rb;
    private PlayerRunTimeStats _stats; // Data

    private Detector _detector;

    private PlayerControls _inputs;
    private InputAction _moveAction;

    private Vector2 _position;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            Camera.main.GetComponent<CameraController>().Target = transform;
        }
    }

    void Awake()
    {
        _inputs = new PlayerControls();
        _rb = GetComponent<Rigidbody2D>();
        _stats = GetComponent<PlayerRunTimeStats>();
        _detector = GetComponentInChildren<Detector>();
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
        if (!IsOwner) return;

        _position = _moveAction.ReadValue<Vector2>();
        _position.Normalize();

        _detector.FindNearestTarget();

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            TakeDamageRpc(10);
        }

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            _stats.DebugLogStatsRpc();
        }

    }

    void FixedUpdate()
    {
        Move();
    }

    private void Move() => _rb.linearVelocity = _position * _stats.CurrentStats.Value.MoveSpeed;

    [Rpc(SendTo.Server)]
    public void TakeDamageRpc(int damage) => _stats.ApplyDamage(damage);

    void OnDrawGizmos()
    {

    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}

