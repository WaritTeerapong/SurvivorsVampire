using UnityEngine;

public class EnemyCombat : MonoBehaviour
{

    public void PerformMeleeAttack(Enemy enemy, Transform target)
    {
        if (target == null) return;

        float hitRange = enemy.CurrentStats.Value.ATKRange + 0.5f; // +0.5 for Enemy not missed more often

        float currentSqrDistance = (target.position - transform.position).sqrMagnitude;

        if (currentSqrDistance <= (hitRange * hitRange))
        {
            PlayerController player = target.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamageRpc(enemy.CurrentStats.Value.ATKDamage);
                Debug.Log("Hit Player!");
            }
        }
        else
        {
            Debug.Log("Missed!");
        }
    }

    public void PerformRangeAttack(Enemy enemy, Transform target)
    {
        if (target == null) return;

        float hitRange = enemy.CurrentStats.Value.ATKRange;

        float currentSqrDistance = (target.position - transform.position).sqrMagnitude;

        if (currentSqrDistance <= (hitRange * hitRange))
        {
            PlayerController player = target.GetComponent<PlayerController>();
            if (player != null)
            {
                // player.TakeDamageRpc(enemy.CurrentStats.Value.ATKDamage);
                Debug.Log("Shoot!!");
                enemy.RequestFireRpc();
                // TODO : Shoot Bullet
            }
        }
    }
}