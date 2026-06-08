using UnityEngine;

public class EnemyMoveState : IEnemyState
{
    public void OnEnter(Enemy enemy)
    {
    }

    public void OnExit(Enemy enemy)
    {
    }

    public void OnUpdate(Enemy enemy)
    {
        if (enemy.Detector.NearestTarget == null)
        {
            enemy.SwitchState(enemy.IdleState);
            return;
        }

        if (enemy.IsPlayerInATKRange())
        {
            enemy.SwitchState(enemy.AttackState);
            return;
        }

        enemy.Movement.MoveToward(
            enemy.Detector.NearestTarget,
            enemy.CurrentStats.Value.MoveSpeed
        );
    }
}