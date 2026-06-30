public interface IBossState
{
    void OnEnter(Boss boss);
    void OnUpdate(Boss boss);
    void OnExit(Boss boss);
}