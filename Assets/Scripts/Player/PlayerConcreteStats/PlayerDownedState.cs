using UnityEngine;

public class PlayerDownedState : IPlayerState
{
    public void OnEnter(Player player)
    {
        player.Movement.Stop();
    }

    public void OnExit(Player player)
    {
    }

    public void OnFixedUpdate(Player player)
    {
    }

    public void OnUpdate(Player player)
    {
    }
}