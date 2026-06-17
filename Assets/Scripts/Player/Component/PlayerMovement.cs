using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private Rigidbody2D _rb;
    public NetworkVariable<float> FacingDirection = new NetworkVariable<float>
    (
        1f,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Owner
    );

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    void Update()
    {
        transform.localScale = new Vector3(FacingDirection.Value, 1, 1);
    }

    public void Move(Vector2 direction, float speed)
    {
        _rb.linearVelocity = direction * speed;

        if (direction.x > 0) FacingDirection.Value = 1f;
        else if (direction.x < 0) FacingDirection.Value = -1f;
    }

    public void Stop() => _rb.linearVelocity = Vector2.zero;

}