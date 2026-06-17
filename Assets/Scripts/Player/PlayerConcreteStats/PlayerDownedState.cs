using UnityEngine;

public class PlayerDownedState : IPlayerState
{
    public void OnEnter(Player player)
    {
        player.Movement.Stop();

        player.ReviveTimer.Value = 3f;
        player.DiedTimer.Value = 10f;
        player.IsBeingRevived.Value = false;
        // Noti other player
    }

    public void OnExit(Player player)
    {
    }

    public void OnFixedUpdate(Player player)
    {
    }

    public void OnUpdate(Player player)
    {
        if (player.IsServer)
        {
            player.ReviveCheck();
        }
    }
}