public interface IPlayerState
{
    void OnEnter(Player player);
    void OnExit(Player player);
    void OnUpdate(Player player);
    void OnFixedUpdate(Player player);
}