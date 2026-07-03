using System;
using UnityEngine;

public class ProjectileCollision : MonoBehaviour
{
    [HideInInspector]
    public float HitRadius = 1f;
    [HideInInspector]
    public LayerMask TargetLayer;

    public event Action<Collider2D, Vector3> OnHitDetected;

    private Vector3 _lastPosition;
    private bool _isActive = false;

    public void Activate()
    {
        _lastPosition = transform.position;
        _isActive = true;

        CircleCollider2D col = GetComponent<CircleCollider2D>() ?? GetComponentInChildren<CircleCollider2D>();
        if (col != null)
        {
            col.radius = HitRadius;
        }
    }

    public void Deactivate()
    {
        _isActive = false;
    }

    void Update()
    {
        if (!_isActive) return;

        Vector3 currentPosition = transform.position;
        float distance = Vector3.Distance(_lastPosition, currentPosition);
        Vector3 direction = (currentPosition - _lastPosition).normalized;

        if (distance > 0.001f)
        {
            RaycastHit2D hit = Physics2D.CircleCast(_lastPosition, HitRadius, direction, distance, TargetLayer);
            if (hit.collider != null)
            {
                OnHitDetected?.Invoke(hit.collider, hit.point);
            }
        }
        _lastPosition = currentPosition;
    }
}
