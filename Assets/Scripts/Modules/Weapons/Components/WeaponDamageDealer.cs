using Unity.Netcode;
using UnityEngine;

public class WeaponDamageDealer : MonoBehaviour
{
    [HideInInspector]
    public bool IsEnemy = false;
    private int _damage;

    public void SetDamage(int damage)
    {
        _damage = damage;
    }

    public bool DealDamage(Collider2D hitCollider)
    {
        if (!NetworkManager.Singleton.IsServer) return false;
        
        if (!IsEnemy && hitCollider.CompareTag("Enemy"))
        {
            Enemy enemy = hitCollider.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(_damage);
            }
            else
            {
                Boss boss = hitCollider.GetComponentInParent<Boss>();
                if (boss != null)
                {
                    boss.TakeDamage(_damage);
                }
            }
        }
        else if (IsEnemy && hitCollider.CompareTag("Player"))
        {
            Player player = hitCollider.GetComponentInParent<Player>();
            if (player != null)
            {
                player.TakeDamageRpc(_damage);
            }
        }
        return true;
    }
}
