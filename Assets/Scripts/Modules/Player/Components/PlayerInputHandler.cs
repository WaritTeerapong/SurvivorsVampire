using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : NetworkBehaviour
{
    private PlayerControls _inputs;
    private InputAction _moveAction;
    public Vector2 MoveInput { get; private set; }

    void Awake() => _inputs = new PlayerControls();
    void OnEnable() { _inputs.Enable(); _moveAction = _inputs.Player.Move; }
    void OnDisable() { _inputs.Disable(); }

    void Update()
    {
        if (!IsOwner) return;
        MoveInput = _moveAction.ReadValue<Vector2>().normalized;
    }
}