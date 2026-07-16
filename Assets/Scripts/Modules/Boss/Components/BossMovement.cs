using Unity.Netcode;
using UnityEngine;

public class BossMovement : NetworkBehaviour
{
    public void MoveTowardsTarget(Vector3 targetPosition, float speed)
    {
        if (!IsServer) return;

        transform.position = Vector2.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        bool isFacingRight = targetPosition.x > transform.position.x;
        HandleFacingDirectionRpc(isFacingRight);
    }

    // Call this to force the boss to face a target without moving
    public void FaceTarget(Vector3 targetPosition)
    {
        if (!IsServer) return;

        bool isFacingRight = targetPosition.x > transform.position.x;
        HandleFacingDirectionRpc(isFacingRight);
    }

    [Rpc(SendTo.Everyone)]
    private void HandleFacingDirectionRpc(bool isFacingRight)
    {
        // Use localScale to flip the entire GameObject (including child FirePoint)
        // Default scale assumes the boss faces right when X is 1
        transform.localScale = new Vector3(isFacingRight ? 1f : -1f, 1f, 1f);
    }
}