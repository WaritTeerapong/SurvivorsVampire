using Unity.Netcode;
using UnityEngine;

public class BossCombat : NetworkBehaviour
{
    private float _attackCooldownTimer;

    public bool CanAttack => _attackCooldownTimer <= 0f;

    void Update()
    {
        if (!IsServer) return;
        if (_attackCooldownTimer > 0f) _attackCooldownTimer -= Time.deltaTime;
    }

    public void ExecuteBasicAttack(Transform target, int damage, float cooldown)
    {
        if (!IsServer || !CanAttack) return;

        _attackCooldownTimer = cooldown;

        // Play ATK Animation 

        if (target.TryGetComponent<Player>(out Player player))
        {
            player.TakeDamageRpc(damage);
        }
    }
}