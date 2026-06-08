using UnityEngine;

public class EnemyIdleState : IEnemyState
{
    public void OnEnter(Enemy enemy)
    {
        Debug.Log("Enter Enemy Idle State");
    }

    public void OnExit(Enemy enemy)
    {
    }

    public void OnUpdate(Enemy enemy)
    {
        if (enemy.Detector != null && enemy.Detector.NearestTarget != null)
        {
            enemy.SwitchState(enemy.MoveState);
        }
    }
}