using UnityEngine;

public class BossTransitionState : IBossState
{
    private float _transitionTimer;
    private const float TRANSITION_DURATION = 1.5f;

    public void OnEnter(Boss boss)
    {
        _transitionTimer = TRANSITION_DURATION;

        // Note: Trigger phase change animation or temporary invincibility here
        boss.PlayAnimation(boss.IDLE);
    }

    public void OnUpdate(Boss boss)
    {
        _transitionTimer -= Time.deltaTime;

        if (_transitionTimer <= 0f)
        {
            boss.SwitchState(boss.ChaseState);
        }
    }

    public void OnExit(Boss boss)
    {
        // Optional: Re-enable generic hitboxes or visual modifications here
    }
}