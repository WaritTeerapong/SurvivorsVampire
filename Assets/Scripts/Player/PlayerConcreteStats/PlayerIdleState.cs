using UnityEngine;
public class PlayerIdleState : IPlayerState
{
    public void OnEnter(Player player) 
    { 
        if (player.Anim != null) player.Anim.enabled = true;
        player.PlayAnimation(player.IDLE); 
        player.Movement.Stop(); 
    }
    public void OnUpdate(Player player) { if (player.InputHandler.MoveInput != Vector2.zero) player.SwitchState(player.MoveState); }
    public void OnFixedUpdate(Player player) { }
    public void OnExit(Player player) { }
}