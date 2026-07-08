using System;
using UnityEngine;

public class ProjectileCollision : MonoBehaviour
{
    [HideInInspector]
    public float HitRadius = 1f;
    [HideInInspector]
    public ContactFilter2D Filter;

    public event Action<Collider2D, Vector3> OnHitDetected;

    private Vector3 _lastPosition;
    private bool _isActive = false;

    private readonly RaycastHit2D[] _castResults = new RaycastHit2D[1];
    private readonly Collider2D[] _overlapResults = new Collider2D[1];

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

    // [FIX] Explicitly calculate Target Layer from isEnemy flag
    public void SetFilter(bool isEnemy)
    {
        int targetLayer = isEnemy ? LayerMask.NameToLayer("Player") : LayerMask.NameToLayer("Enemy");

        Filter = new ContactFilter2D()
        {
            useTriggers = true,
            useLayerMask = true,
            layerMask = 1 << targetLayer,
        };
    }

    public void Deactivate()
    {
        _isActive = false;
    }

    private void Update()
    {
        if (!_isActive) return;

        Vector3 currentPosition = transform.position;

        int overlapCount = Physics2D.OverlapCircle(currentPosition, HitRadius, Filter, _overlapResults);
        if (overlapCount > 0 && _overlapResults[0] != null)
        {
            OnHitDetected?.Invoke(_overlapResults[0], currentPosition);
            return;
        }

        float distance = Vector3.Distance(_lastPosition, currentPosition);
        if (distance > 0.001f)
        {
            Vector3 direction = (currentPosition - _lastPosition).normalized;
            int hitCount = Physics2D.CircleCast(_lastPosition, HitRadius, direction, Filter, _castResults, distance);

            if (hitCount > 0 && _castResults[0].collider != null)
            {
                OnHitDetected?.Invoke(_castResults[0].collider, _castResults[0].point);
                return;
            }
        }

        _lastPosition = currentPosition;
    }
}