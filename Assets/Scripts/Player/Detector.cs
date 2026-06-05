using System.Collections.Generic;
using UnityEngine;

public class Detector : MonoBehaviour
{
    private CircleCollider2D _detectorCollider;
    private PlayerRunTimeStats _playerStats;

    private List<Transform> _enemiesInRange = new List<Transform>();
    public Transform NearestTarget { get; private set; }

    void Awake()
    {
        if (_detectorCollider == null) _detectorCollider = GetComponent<CircleCollider2D>();
        if (_playerStats == null) _playerStats = GetComponentInParent<PlayerRunTimeStats>();
    }

    void Start()
    {
        _detectorCollider.radius = _playerStats.CurrentStats.Value.ATKRange;
    }

    public void FindNearestTarget()
    {
        _enemiesInRange.RemoveAll(enemy => enemy == null);

        if (_enemiesInRange.Count == 0)
        {
            NearestTarget = null;
            return;
        }

        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        foreach (Transform enemy in _enemiesInRange)
        {
            float sqrDistance = (enemy.position - transform.position).sqrMagnitude;
            if (sqrDistance < shortestDistance)
            {
                shortestDistance = sqrDistance;
                nearestEnemy = enemy;
            }
        }

        NearestTarget = nearestEnemy;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (!_enemiesInRange.Contains(other.transform))
            {
                _enemiesInRange.Add(other.transform);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            _enemiesInRange.Remove(other.transform);
        }
    }
}
