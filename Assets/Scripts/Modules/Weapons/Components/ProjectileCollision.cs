using System;
using UnityEngine;

public class ProjectileCollision : MonoBehaviour
{
    [HideInInspector]
    public float HitRadius = 1f;
    [HideInInspector]
    public ContactFilter2D filter;

    public event Action<Collider2D, Vector3> OnHitDetected;

    private Vector3 _lastPosition;
    private bool _isActive = false;
    private readonly RaycastHit2D[] _castResults = new RaycastHit2D[1];

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

    public void SetFilter(int selfLayer)
    {
        int targetLayer = GetTargetFromLayer(selfLayer);
        filter = new ContactFilter2D()
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

    void Update()
    {
        if (!_isActive) return;

        Physics2D.SyncTransforms();

        Vector3 currentPosition = transform.position;
        float distance = Vector3.Distance(_lastPosition, currentPosition);
        Vector3 direction = (currentPosition - _lastPosition).normalized;

        if (distance <= 0.001f) return;

        int hitCount = Physics2D.CircleCast(_lastPosition, HitRadius, direction, filter, _castResults, distance);

        // ตรวจก่อนว่ามี Enemy อยู่ในระยะ sweep นี้จริงไหม (ไม่สนใจ filter)
        Collider2D[] enemiesNearby = Physics2D.OverlapCircleAll(currentPosition, HitRadius * 2f);
        foreach (var c in enemiesNearby)
        {
            if (c.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                Debug.Log($"[Collision] === ENEMY IN RANGE === From={_lastPosition} To={currentPosition} Dist={distance} HitRadius={HitRadius} hitCount(CircleCast)={hitCount} EnemyPos={c.transform.position} EnemyCollider={c.name} EnemyBounds={c.bounds}");
            }
        }

        _lastPosition = currentPosition;

        if (hitCount <= 0) return;

        RaycastHit2D hit = _castResults[0];
        Debug.Log($"[Collision] >>> HIT CONFIRMED: {hit.collider?.name} at {hit.point}");
        if (hit.collider == null) return;

        OnHitDetected?.Invoke(hit.collider, hit.point);
    }

    private int GetTargetFromLayer(int layerIndex)
    {
        if (layerIndex == LayerMask.NameToLayer("Enemy"))
            return LayerMask.NameToLayer("Player");
        if (layerIndex == LayerMask.NameToLayer("Player"))
            return LayerMask.NameToLayer("Enemy");
        return LayerMask.NameToLayer("Default");
    }
}
