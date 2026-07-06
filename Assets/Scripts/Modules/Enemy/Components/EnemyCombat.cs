using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    public void PerformMeleeAttack(Enemy enemy, Transform target)
    {
        if (target == null) return;

        float hitRange = enemy.CurrentStats.Value.ATKRange + 0.5f;

        IDamageble damageable = target.GetComponent<IDamageble>();
        Vector3 targetPos = damageable != null ? damageable.TargetPoint.position : target.position;

        float currentSqrDistance = (targetPos - enemy.TargetPoint.position).sqrMagnitude;

        if (currentSqrDistance <= (hitRange * hitRange))
        {
            if (damageable != null)
            {
                damageable.TakeDamage(enemy.CurrentStats.Value.ATKDamage);
            }
        }
    }

    public void PerformRangeAttack(Enemy enemy, Transform target)
    {
        if (target == null) return;

        float hitRange = enemy.CurrentStats.Value.ATKRange;

        IDamageble damageable = target.GetComponent<IDamageble>();
        Vector3 targetPos = damageable != null ? damageable.TargetPoint.position : target.position;

        float currentSqrDistance = (targetPos - enemy.TargetPoint.position).sqrMagnitude;

        if (currentSqrDistance <= (hitRange * hitRange))
        {
            if (damageable != null)
            {
                enemy.RequestFireRpc();
            }
        }
    }
}