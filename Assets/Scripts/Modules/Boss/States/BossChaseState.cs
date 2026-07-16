using UnityEngine;

public class BossChaseState : IBossState
{
    public void OnEnter(Boss boss)
    {
        boss.PlayAnimation(boss.CHASE);
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
            Player p = boss.Detector.NearestTarget.GetComponent<Player>();
            Vector3 targetPos = p != null ? p.TargetPoint.position : boss.Detector.NearestTarget.position;

            float sqrDir = (targetPos - boss.TargetPoint.position).sqrMagnitude;
            float rangedRangeSq = boss.BossData.RangedAttackRange * boss.BossData.RangedAttackRange;

            if (sqrDir <= rangedRangeSq)
            {
                if (boss.Combat.CanAttack)
                {
                    boss.SwitchState(boss.AttackState);
                }
                return;
            }

            float moveSpeed = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_MoveSpeed : boss.BossData.P2_MoveSpeed;
            boss.Movement.MoveTowardsTarget(boss.Detector.NearestTarget.position, moveSpeed);
        }
        else
        {
            boss.SwitchState(boss.IdleState);
        }
    }

    public void OnExit(Boss boss) { }
}