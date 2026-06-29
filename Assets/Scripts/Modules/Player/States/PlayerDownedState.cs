using UnityEngine;

public class PlayerDownedState : IPlayerState
{
    public void OnEnter(Player player)
    {
        if (player.IsOwner && player.Movement != null) player.Movement.Stop();
        if (player.Revive != null) player.Revive.SetReviveZoneActive(true);
        player.ResetDownedState();

        player.PlayAnimation(player.DOWN);

        if (player.IsServer && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.RemoveActiveTarget(player.transform);
        }
    }

    public void OnExit(Player player)
    {
        if (player.Revive != null) player.Revive.SetReviveZoneActive(false);

        // Play Revived SFX
    }

    public void OnFixedUpdate(Player player) { }

    public void OnUpdate(Player player)
    {
    }
}