using UnityEngine;

public class EnemyAttackState : IEnemyState
{
    private float _atkTimer;
    private float _atkCooldown;
    private bool _hasAttacked;

    public void OnEnter(Enemy enemy)
    {
        float atkSpeed = enemy.CurrentStats.Value.ATKSpeed;
        _atkCooldown = (atkSpeed > 0) ? (1f / atkSpeed) : 1f;

        _atkTimer = 0f;
        _hasAttacked = false;
    }

    public void OnUpdate(Enemy enemy)
    {
        _atkTimer += Time.deltaTime;

        float windUpTime = 0.2f;

        if (_atkTimer >= windUpTime && !_hasAttacked)
        {
            if (enemy.EnemyType.IsRange) enemy.Combat.PerformRangeAttack(enemy, enemy.Detector.NearestTarget);
            else if (!enemy.EnemyType.IsRange) enemy.Combat.PerformMeleeAttack(enemy, enemy.Detector.NearestTarget);
            enemy.PlayAnimation(Enemy.ATK);
            _hasAttacked = true;
        }

        if (_atkTimer >= _atkCooldown)
        {
            if (!enemy.IsPlayerInATKRange())
            {
                enemy.SwitchState(enemy.MoveState);
            }
            else
            {
                _atkTimer = 0f;
                _hasAttacked = false;
            }
        }
    }

    public void OnExit(Enemy enemy)
    {
    }
}