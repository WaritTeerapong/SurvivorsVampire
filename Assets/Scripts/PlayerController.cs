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
        _position = _moveAction.ReadValue<Vector2>();
        _position.Normalize();
    }

    void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        _rb.linearVelocity = _position * _speed;
    }
}
