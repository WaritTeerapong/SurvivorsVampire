using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    public void PerformMeleeAttack(Enemy enemy, Transform target)
    {
        if (target == null) return;

        float hitRange = enemy.CurrentStats.Value.ATKRange + 0.5f;

        Player player = target.GetComponent<Player>();
        Vector3 targetPos = player != null ? player.TargetPoint.position : target.position;

        float currentSqrDistance = (targetPos - enemy.TargetPoint.position).sqrMagnitude;

        if (currentSqrDistance <= (hitRange * hitRange))
        {
            if (player != null)
            {
                player.TakeDamageRpc(enemy.CurrentStats.Value.ATKDamage);
            }
        }
    }

    public void PerformRangeAttack(Enemy enemy, Transform target)
    {
        if (target == null) return;

        float hitRange = enemy.CurrentStats.Value.ATKRange;

        Player player = target.GetComponent<Player>();
        Vector3 targetPos = player != null ? player.TargetPoint.position : target.position;

        float currentSqrDistance = (targetPos - enemy.TargetPoint.position).sqrMagnitude;

        if (currentSqrDistance <= (hitRange * hitRange))
        {
            if (player != null)
            {
                enemy.RequestFireRpc();
            }
        }
    }
}