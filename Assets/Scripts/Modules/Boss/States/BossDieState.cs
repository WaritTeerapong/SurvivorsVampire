using UnityEngine;

public class BossDieState : IBossState
{
    public void OnEnter(Boss boss)
    {
        boss.PlayAnimation(boss.DIED);

        boss.TriggerDeathSequence();

        Collider2D bossCollider = boss.GetComponent<Collider2D>();
        if (bossCollider != null)
        {
            bossCollider.enabled = false;
        }

        // Stop all movement
        if (boss.Movement != null)
        {
            boss.Movement.MoveTowardsTarget(boss.transform.position, 0f);
        }
    }

    public void OnUpdate(Boss boss)
    {
    }

    public void OnExit(Boss boss)
    {
    }
}