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

    public void ExecuteMeleeAttack(ulong targetId, int damage, float cooldown)
    {
        if (!IsServer || !CanAttack) return;

        _attackCooldownTimer = cooldown;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetObj))
        {
            if (targetObj.TryGetComponent<Player>(out Player player))
            {
                player.TakeDamageRpc(damage);
            }
        }
        else
        {
            // Debug.LogWarning("[BossCombat] Melee target not found or despawned.");
        }
    }

    public void ExecuteRangedAttack(ulong targetId, int damage, float cooldown)
    {
        if (!IsServer || !CanAttack) return;

        _attackCooldownTimer = cooldown;
        ExecuteRangedAttackRpc(targetId, damage);
    }

    [Rpc(SendTo.Everyone)]
    private void ExecuteRangedAttackRpc(ulong targetId, int damage)
    {
        Boss boss = GetComponent<Boss>();

        if (boss.BossData.BulletPrefab == null || ObjectPoolManager.Instance == null)
        {
            // Debug.LogWarning("[BossCombat] BulletPrefab or ObjectPoolManager missing.");
            return;
        }

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetObj))
        {
            Vector3 spawnPos = transform.position;
            Bullet bulletObj = ObjectPoolManager.Instance.SpawnObject<Bullet>(boss.BossData.BulletPrefab, spawnPos, Quaternion.identity, PoolCategory.Projectiles);

            if (bulletObj != null)
            {
                bulletObj.IsEnemy = true;
                float bulletSpeed = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_BulletSpeed : boss.BossData.P2_BulletSpeed;
                bulletObj.Speed = bulletSpeed;

                bulletObj.Initialize(targetObj.transform, damage, boss.BossData.BulletHitVFXPrefab);
            }
        }
    }
}