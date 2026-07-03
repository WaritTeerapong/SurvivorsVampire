using UnityEngine;
using Unity.Netcode;

public class BossAttackState : IBossState
{
    private float _stateTimer;
    private bool _hasAttacked;
    private float _windUpTime = 0.3f;
    private float _totalDuration = 1.0f;
    private bool _isMelee;
    private ulong _targetId;
    private int _damage;

    public void OnEnter(Boss boss)
    {
        _stateTimer = 0f;
        _hasAttacked = false;

        if (boss.Detector.NearestTarget == null)
        {
            boss.SwitchState(boss.IdleState);
            return;
        }

        Player p = boss.Detector.NearestTarget.GetComponent<Player>();
        Vector3 targetPos = p != null ? p.TargetPoint.position : boss.Detector.NearestTarget.position;
        float sqrDist = (targetPos - boss.TargetPoint.position).sqrMagnitude;

        _isMelee = sqrDist <= (boss.BossData.MeleeAttackRange * boss.BossData.MeleeAttackRange);

        if (boss.Detector.NearestTarget.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            _targetId = netObj.NetworkObjectId;
        }
        else
        {
            boss.SwitchState(boss.IdleState);
            return;
        }

        if (_isMelee)
        {
            boss.PlayAnimation(boss.MELEE);
            _damage = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_MeleeDamage : boss.BossData.P2_MeleeDamage;
        }
        else
        {
            boss.PlayAnimation(boss.RANGED);
            _damage = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_RangedDamage : boss.BossData.P2_RangedDamage;
        }
    }

    public void OnUpdate(Boss boss)
    {
        _stateTimer += Time.deltaTime;

        if (_stateTimer >= _windUpTime && !_hasAttacked)
        {
            float cooldown = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_AtkCooldown : boss.BossData.P2_AtkCooldown;

            if (_isMelee)
            {
                boss.Combat.ExecuteMeleeAttack(_targetId, _damage, cooldown);
            }
            else
            {
                boss.Combat.ExecuteRangedAttack(_targetId, _damage, cooldown);
            }

            _hasAttacked = true;
        }

        if (_stateTimer >= _totalDuration)
        {
            boss.SwitchState(boss.IdleState);
        }
    }

    public void OnExit(Boss boss) { }
}