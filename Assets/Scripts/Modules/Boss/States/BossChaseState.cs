using UnityEngine;

public class BossChaseState : IBossState
{
    public void OnEnter(Boss boss)
    {
        boss.PlayAnimation(boss.CHASE);
    }

    public void OnExit(Boss boss)
    {
    }

    public void OnUpdate(Boss boss)
    {
        if (boss.SpawnTimer <= 0)
        {
            boss.SwitchState(boss.SpawnState);
            return;
        }

        if (boss.AOETimer <= 0)
        {
            boss.SwitchState(boss.AOEState);
            return;
        }

        if (boss.Detector != null && boss.Detector.NearestTarget != null)
        {
            float sqrDir = (boss.Detector.NearestTarget.position - boss.transform.position).sqrMagnitude;

            float moveSpeed = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_MoveSpeed : boss.BossData.P2_MoveSpeed;
            int attackDamage = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_AtkDamage : boss.BossData.P2_AtkDamage;

            if (sqrDir <= (boss.BossData.AttackRange * boss.BossData.AttackRange))
            {
                if (boss.Combat.CanAttack)
                {
                    boss.Combat.ExecuteBasicAttack(boss.Detector.NearestTarget, attackDamage, 1.5f);
                }
                return;
            }

            boss.Movement.MoveTowardsTarget(boss.Detector.NearestTarget.position, moveSpeed);

        }

    }
}