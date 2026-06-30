using Unity.Netcode;
using UnityEngine;

public class BossMovement : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public void MoveTowardsTarget(Vector3 targetPosition, float speed)
    {
        if (!IsServer) return;

        transform.position = Vector2.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        HandleFacingDirectionRpc(targetPosition.x > transform.position.x);
    }

    [Rpc(SendTo.Everyone)]
    private void HandleFacingDirectionRpc(bool isFacingRight)
    {
        if (_spriteRenderer == null) return;

        _spriteRenderer.flipX = !isFacingRight;
    }
}