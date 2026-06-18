using UnityEngine;
public class PlayerDiedState : IPlayerState
{
    public void OnEnter(Player player)
    {
        player.BecomeGhostRpc();

        if (player.IsServer && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.RemoveActiveTarget(player.transform);
        }
    }

    public void OnUpdate(Player player) { }

    public void OnFixedUpdate(Player player)
    {
        player.Movement.Move(player.InputHandler.MoveInput, player.Stats.CurrentStats.Value.MoveSpeed);
    }

    public void OnExit(Player player)
    {
    }
}