using UnityEngine;
public class PlayerMoveState : IPlayerState
{
    public void OnEnter(Player player) { player.PlayAnimation(player.RUN); }
    public void OnUpdate(Player player) { if (player.InputHandler.MoveInput == Vector2.zero) player.SwitchState(player.IdleState); }
    public void OnFixedUpdate(Player player) { player.Movement.Move(player.InputHandler.MoveInput, player.Stats.CurrentStats.Value.MoveSpeed); }
    public void OnExit(Player player) { }
}