using UnityEngine;
public class PlayerDiedState : IPlayerState
{
    private int _currentAnimHash;
    public void OnEnter(Player player)
    {
        player.BecomeGhostRpc();
        _currentAnimHash = player.GHOST_IDLE;

        if (player.IsServer && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.RemoveActiveTarget(player.transform);
        }
    }

    public void OnUpdate(Player player)
    {
        int targetHash = player.InputHandler.MoveInput != Vector2.zero ? player.GHOST_RUN : player.GHOST_IDLE;

        if (_currentAnimHash != targetHash)
        {
            _currentAnimHash = targetHash;
            player.PlayAnimation(_currentAnimHash);
        }
    }

    public void OnFixedUpdate(Player player)
    {
        player.Movement.Move(player.InputHandler.MoveInput, player.Movement.GhostMoveSpeed);
    }

    public void OnExit(Player player)
    {
    }
}