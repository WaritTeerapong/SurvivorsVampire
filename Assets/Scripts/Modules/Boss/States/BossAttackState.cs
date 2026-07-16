using UnityEngine;
using Unity.Netcode;

public class BossAttackState : IBossState
{
    private float _stateTimer;
    private bool _hasAttacked;
    private float _windUpTime;
    private float _totalDuration;
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

        boss.Movement.FaceTarget(boss.Detector.NearestTarget.position);

        Player p = boss.Detector.NearestTarget.GetComponent<Player>();
        Vector3 targetPos = p != null ? p.TargetPoint.position : boss.Detector.NearestTarget.position;
        float sqrDist = (targetPos - boss.TargetPoint.position).sqrMagnitude;

        _isMelee = sqrDist <= (boss.BossData.CloseAttackRange * boss.BossData.CloseAttackRange);

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
            boss.PlayAnimation(boss.CLOSEAOE);
            _damage = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_MeleeDamage : boss.BossData.P2_MeleeDamage;
            _totalDuration = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_CloseAOEDuration : boss.BossData.P2_CloseAOEDuration;
            _windUpTime = 0.1f;
        }
        else
        {
            boss.PlayAnimation(boss.RANGED);
            _damage = boss.CurrentPhase.Value == 1 ? boss.BossData.P1_RangedDamage : boss.BossData.P2_RangedDamage;
            _totalDuration = 1.0f;
            _windUpTime = 0.3f;
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
                boss.Combat.ExecuteCloseAOEAttack(_damage, boss.BossData.CloseAttackRange, _totalDuration, cooldown);
            }
            else
            {
                boss.PlaySFXClientRpc("BossShoot");
                boss.Combat.ExecuteRangedAttack(_targetId, _damage, cooldown);
            }

            _hasAttacked = true;
        }

        if (_stateTimer >= (_totalDuration + _windUpTime))
        {
            boss.SwitchState(boss.IdleState);
        }
    }

    public void OnExit(Boss boss) { }
}