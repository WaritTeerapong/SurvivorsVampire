using UnityEngine;

public class PlayerReviveHandler : MonoBehaviour
{
    private Player _player;
    private CircleCollider2D _reviveCol;
    public float ReviveZone = 2f;

    void Awake()
    {
        _player = GetComponentInParent<Player>();
        _reviveCol = GetComponent<CircleCollider2D>();
        _reviveCol.radius = ReviveZone > 0 ? ReviveZone : 2f;
        if (!_reviveCol.isTrigger) _reviveCol.isTrigger = true;
        _reviveCol.enabled = false;
    }

    void Start()
    {
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") && this.gameObject) return;

        _player.SetPlayerInReviveRange(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") && this.gameObject) return;

        _player.SetPlayerInReviveRange(false);
    }

    public void TriggerReviveZone()
    {
        _reviveCol.enabled = true;
        // Show Died Timer UI
        // Show Noti Downed UI 
        // Show Revive Circle
        // Noti to other Player 
    }
}