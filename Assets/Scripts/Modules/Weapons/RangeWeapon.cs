using UnityEngine;

public class RangeWeapon : BaseWeapon
{
    [Header("Range Weapon Settings")]
    public GameObject BulletPrefab;

    public override void Attack(Transform target)
    {
        if (WeaponData == null || ObjectPoolManager.Instance == null || BulletPrefab == null) return;

        Vector3 spawnPos = transform.position;
        Transform firePoint = transform.Find("FirePoint");

        Player myPlayer = GetComponentInParent<Player>();

        if (firePoint != null)
        {
            spawnPos = firePoint.position;
        }
        else
        {
            if (myPlayer != null) spawnPos = myPlayer.TargetPoint.position;
        }

        GameObject bulletObj = ObjectPoolManager.Instance.SpawnObject<GameObject>(BulletPrefab, spawnPos, Quaternion.identity, PoolCategory.Projectiles);

        if (bulletObj != null)
        {
            Bullet bulletScript = bulletObj.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                int totalDamage = GetTotalATKDamage();
                GameObject hitVFX = WeaponData.HitVFXPrefab;

                Transform aimTarget = target;
                if (target.TryGetComponent<IDamageble>(out IDamageble d)) aimTarget = d.TargetPoint;

                // [FIX] Explicitly declare this bullet belongs to the Player (isEnemy = false)
                bulletScript.Initialize(aimTarget, totalDamage, hitVFX, false);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("PlayerShoot", spawnPos);
            }
        }
    }
}