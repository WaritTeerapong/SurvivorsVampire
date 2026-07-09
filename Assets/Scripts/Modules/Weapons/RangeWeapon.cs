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
        else
        {
            Player myPlayer = GetComponentInParent<Player>();
            if (myPlayer != null) spawnPos = myPlayer.TargetPoint.position;
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

                Transform aimTarget = target;
                if (target.TryGetComponent<IDamageble>(out IDamageble d)) aimTarget = d.TargetPoint;

                bulletScript.Initialize(aimTarget, totalDamage, hitVFX);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("PlayerMGShoot", spawnPos);
            }
        }
    }
}