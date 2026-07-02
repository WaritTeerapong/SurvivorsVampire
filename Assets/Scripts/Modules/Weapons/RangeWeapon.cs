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
        if (firePoint != null)
        {
            spawnPos = firePoint.position;
        }

        GameObject bulletObj = ObjectPoolManager.Instance.SpawnObject<GameObject>(BulletPrefab, spawnPos, Quaternion.identity, PoolCategory.Projectiles);
        if (bulletObj != null)
        {
            Bullet bulletScript = bulletObj.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                // Set the bullet damage to the combined weapon stat and player stat
                int totalDamage = GetTotalATKDamage();
                GameObject hitVFX = WeaponData.HitVFXPrefab;
                bulletScript.Initialize(target, totalDamage, hitVFX);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("PlayerShoot", spawnPos);
            }
        }
    }
}
