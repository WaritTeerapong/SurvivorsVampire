using Unity.Netcode;
using UnityEngine;

public class BossCombat : NetworkBehaviour
{
    [Header("=== Combat Settings ===")]
    [SerializeField] private Transform _firePoint;

    private float _attackCooldownTimer;
    public bool CanAttack => _attackCooldownTimer <= 0f;

    void Update()
    {
        if (!IsServer) return;
        if (_attackCooldownTimer > 0f) _attackCooldownTimer -= Time.deltaTime;
    }

    public void ExecuteCloseAOEAttack(int damage, float radius, float expandDuration, float cooldown)
    {
        if (!IsServer || !CanAttack) return;

        _attackCooldownTimer = cooldown;

        Boss boss = GetComponent<Boss>();
        if (boss.BossData.CloseAOEPrefab == null || ObjectPoolManager.Instance == null)
        {
            // Debug.LogWarning("[BossCombat] CloseAOEPrefab missing.");
            return;
        }

        Vector3 spawnPos = boss.TargetPoint.position;
        GameObject aoeObj = ObjectPoolManager.Instance.SpawnObject<GameObject>(
            boss.BossData.CloseAOEPrefab, spawnPos, Quaternion.identity, PoolCategory.Default
        );

        if (aoeObj.TryGetComponent<NetworkObject>(out NetworkObject netObj) && !netObj.IsSpawned)
        {
            netObj.Spawn(true);
        }

        if (aoeObj.TryGetComponent<BossAOEController>(out BossAOEController controller))
        {
            controller.InitializeCloseAOE(damage, radius, expandDuration);
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
            return;
        }

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetObj))
        {
            Vector3 spawnPos = _firePoint != null ? _firePoint.position : boss.TargetPoint.position;
            Bullet bulletObj = ObjectPoolManager.Instance.SpawnObject<Bullet>(boss.BossData.BulletPrefab, spawnPos, Quaternion.identity, PoolCategory.Projectiles);

            if (bulletObj != null)
            {
                bulletObj.IsEnemy = true;
                float bulletSpeed = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_BulletSpeed : boss.BossData.P2_BulletSpeed;
                bulletObj.Speed = bulletSpeed;

                Transform aimTarget = targetObj.transform;
                if (targetObj.TryGetComponent<Player>(out Player p)) aimTarget = p.TargetPoint;

                bulletObj.Initialize(aimTarget, damage, boss.BossData.BulletHitVFXPrefab);
            }
        }
    }
}