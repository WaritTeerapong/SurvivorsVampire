using UnityEngine;

public class EnemyDieState : IEnemyState
{
    public void OnEnter(Enemy enemy)
    {
        enemy._anim.Play("Die");
    }

    public void OnExit(Enemy enemy)
    {
    }

    public void OnUpdate(Enemy enemy)
    {
        
    }
}