using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private Collider2D[] _boidNeighbors = new Collider2D[15];
    private float _boidsTimer = 0f;
    private float _boidsTickRate = 0.1f;
    private Vector2 _currentBoidsForce = Vector2.zero;

    private ContactFilter2D _enemyFilter;
    private bool _isFilterSetup = false;

    private int _debugNeighborCount = 0;
    private float _debugRadius = 0f;
    private bool _useBoidsGizmos = false;

    public void MoveToward(Enemy enemy)
    {
        if (enemy.Detector.NearestTarget == null) return;

        Vector2 targetPos = enemy.Detector.NearestTarget.position;
        Vector2 currentPos = transform.position;
        Vector2 directionToTarget = (targetPos - currentPos).normalized;

        float speed = enemy.CurrentStats.Value.MoveSpeed;
        Vector2 finalDirection = directionToTarget;

        if (enemy.EnemyType != null && enemy.EnemyType.UseBoids)
        {
            _boidsTimer -= Time.deltaTime;

            if (_boidsTimer <= 0f)
            {
                _currentBoidsForce = CalculateBoidsForce(enemy, directionToTarget, currentPos);
                _boidsTimer = _boidsTickRate;
            }

            finalDirection = _currentBoidsForce;
        }

        transform.position = Vector2.MoveTowards(transform.position, currentPos + finalDirection, speed * Time.deltaTime);
    }

    private Vector2 CalculateBoidsForce(Enemy enemy, Vector2 targetDir, Vector2 currentPos)
    {
        Vector2 separationForce = Vector2.zero;
        Vector2 alignmentForce = Vector2.zero;
        Vector2 cohesionCenter = Vector2.zero;
        int neighborCount = 0;

        if (!_isFilterSetup)
        {
            _enemyFilter = new ContactFilter2D();
            _enemyFilter.useLayerMask = true;
            _enemyFilter.SetLayerMask(enemy.EnemyType.EnemyLayer);
            _enemyFilter.useTriggers = true;

            _isFilterSetup = true;
        }

        int count = Physics2D.OverlapCircle(currentPos, enemy.EnemyType.BoidsDetectionRadius, _enemyFilter, _boidNeighbors);

        for (int i = 0; i < count; i++)
        {
            Collider2D col = _boidNeighbors[i];
            if (col.gameObject == this.gameObject) continue;

            Enemy neighbor = col.GetComponent<Enemy>();
            if (neighbor == null) continue;

            Vector2 neighborPos = neighbor.transform.position;
            Vector2 dirToNeighbor = neighborPos - currentPos;
            float distance = dirToNeighbor.magnitude;

            if (distance > 0)
            {
                separationForce -= (dirToNeighbor / distance);
                alignmentForce += neighbor.CurrentDirection;
                cohesionCenter += neighborPos;
                neighborCount++;
            }
        }

        _debugNeighborCount = neighborCount;

        if (neighborCount > 0)
        {
            alignmentForce /= neighborCount;
            cohesionCenter /= neighborCount;

            Vector2 cohesionForce = (cohesionCenter - currentPos).normalized;

            Vector2 finalForce = (targetDir * enemy.EnemyType.TargetWeight) +
                                    (separationForce.normalized * enemy.EnemyType.SeparationWeight) +
                                    (alignmentForce.normalized * enemy.EnemyType.AlignmentWeight) +
                                    (cohesionForce * enemy.EnemyType.CohesionWeight);

            return finalForce.normalized;
        }

        return targetDir;
    }

    private void OnDrawGizmos()
    {
        if (_useBoidsGizmos && _debugRadius > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _debugRadius);
        }
    }
}