public interface IEnemyState
{
    void OnEnter(Enemy enemy);
    void OnUpdate(Enemy enemy);
    void OnExit(Enemy enemy);
}