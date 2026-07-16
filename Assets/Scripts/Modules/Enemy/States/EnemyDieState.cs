using UnityEngine;

public class EnemyDieState : IEnemyState
{
    public void OnEnter(Enemy enemy)
    {
        enemy.PlayAnimation(Enemy.DIE);
        enemy.SetColliderTo(false);
    }

    public void OnExit(Enemy enemy)
    {
    }

    public void OnUpdate(Enemy enemy)
    {

    }
}