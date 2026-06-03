using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D _rb;

    [SerializeField] private float _speed = 5f;

    [SerializeField] private PlayerControls _inputs;

    [SerializeField] private InputAction _moveAction;

    [SerializeField] private Vector2 _position;

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
        _rb.linearVelocity = _position * _speed;
    }


}
