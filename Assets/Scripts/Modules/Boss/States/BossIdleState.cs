using UnityEngine;

public class BossIdleState : IBossState
{
    public void OnEnter(Boss boss)
    {
        boss.PlayAnimation(boss.IDLE);
    }

    public void OnUpdate(Boss boss)
    {
        // When cooldown is ready, return to chase logic
        if (boss.Combat.CanAttack)
        {
            boss.SwitchState(boss.ChaseState);
        }
    }

    public void OnExit(Boss boss) { }
}