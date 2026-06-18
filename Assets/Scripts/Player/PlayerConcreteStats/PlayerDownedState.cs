using UnityEngine;

public class PlayerDownedState : IPlayerState
{
    public void OnEnter(Player player)
    {
        // Added safety check for Movement
        if (player.IsOwner && player.Movement != null)
        {
            player.Movement.Stop();
        }

        if (player.Revive != null)
        {
            player.Revive.TriggerReviveZone();
        }

        // Play Downed Animation
        // Play Downed SFX
        // Reset Revive Stats
        // Noti other player
    }

    public void OnExit(Player player)
    {
        // Close the revive zone
        if (player.Revive != null)
        {
            player.Revive.TriggerReviveZone();
        }

        // Play Revived SFX 
    }

    public void OnFixedUpdate(Player player)
    {
        // Physics update empty
    }

    public void OnUpdate(Player player)
    {
        // Only server handles the revive check progression
        if (player.IsServer)
        {
            player.ReviveCheck();
        }
    }
}