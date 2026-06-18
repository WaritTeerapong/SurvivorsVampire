using UnityEngine;

public class PlayerReviveHandler : MonoBehaviour
{
    private Player _player;
    private CircleCollider2D _reviveCol;

    [Header("=== Revive Settings ===")]
    public float ReviveZone = 2f;

    private bool _isZoneOpen = false;

    void Awake()
    {
        _player = GetComponentInParent<Player>();
        _reviveCol = GetComponent<CircleCollider2D>();

        _reviveCol.radius = ReviveZone > 0 ? ReviveZone : 2f;
        if (!_reviveCol.isTrigger) _reviveCol.isTrigger = true;
        _reviveCol.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Player reviver = other.GetComponentInParent<Player>();

        if (reviver != null && reviver.IsOwner && !reviver.IsDownOrDied)
        {
            _player.UpdateReviverCountServerRpc(1);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Player reviver = other.GetComponentInParent<Player>();

        if (reviver != null && reviver.IsOwner && !reviver.IsDownOrDied)
        {
            _player.UpdateReviverCountServerRpc(-1);
        }
    }

    public void TriggerReviveZone()
    {
        _isZoneOpen = !_isZoneOpen;
        _reviveCol.enabled = _isZoneOpen;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _isZoneOpen ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, ReviveZone > 0 ? ReviveZone : 2f);
    }
}