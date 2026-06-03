using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    private Rigidbody2D _rb;

    [SerializeField] private float _speed = 5f;

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
    }

    void OnEnable()
    {
        _inputs.Enable();

        _moveAction = _inputs.Player.Move;

    }

    void Update()
    {
        if (!IsOwner) return;

        _position = _moveAction.ReadValue<Vector2>();
        _position.Normalize();

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            DebugLogSomethingRpc(new DataSomethings { Value = 42 });
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            InteractWithPlayerRpc();
        }

    }

    void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        _rb.linearVelocity = _position * _speed;
    }

    [Rpc(SendTo.Server)]
    private void InteractWithPlayerRpc()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, 2f);

        foreach (var hitCollider in hitColliders)
        {
            var pc = hitCollider.GetComponent<PlayerController>();
            if (pc != null && pc != this)
            {
                Debug.Log("Interacted with player: " + pc.name);
                pc.ShowTargetUIRpc();
            }
        }
    }

    [Rpc(SendTo.Owner)]
    private void ShowTargetUIRpc()
    {
        // Show UI element above the player
        Debug.Log("Yoo! Someone interacted with you!");
    }

    [Rpc(SendTo.NotOwner)]
    public void DebugLogSomethingRpc(DataSomethings data)
    {
        Debug.Log("Something" + data.Value);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}


public struct DataSomethings : INetworkSerializable
{
    public int Value;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Value);
    }
}
