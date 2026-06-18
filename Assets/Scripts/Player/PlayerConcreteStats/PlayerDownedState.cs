using UnityEngine;

public class PlayerDownedState : IPlayerState
{
    public void OnEnter(Player player)
    {
        if (player.IsOwner && player.Movement != null)
        {
            player.Movement.Stop();
        }

        if (player.Revive != null)
        {
            player.Revive.TriggerReviveZone();
        }

        player.ResetDownedState();

        // Play Downed Animation
        // Play Downed SFX
    }

    public void OnExit(Player player)
    {
        if (player.Revive != null)
        {
            player.Revive.TriggerReviveZone();
        }

        // Play Revived SFX
    }

    public void OnFixedUpdate(Player player) { }

    public void OnUpdate(Player player)
    {
    }
}