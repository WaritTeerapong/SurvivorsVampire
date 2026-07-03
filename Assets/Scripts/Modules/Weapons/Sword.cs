using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(SwordSweepMovement))]
[RequireComponent(typeof(BladeCollision))]
[RequireComponent(typeof(WeaponDamageDealer))]
[RequireComponent(typeof(SwordVisuals))]
public class Sword : MonoBehaviour
{
    [Header("Sweep Settings")]
    public float SweepAngle;
    public float SweepDuration;
    public bool IsEnemy = false;

    private SwordSweepMovement _movement;
    private BladeCollision _collision;
    private WeaponDamageDealer _damageDealer;
    private SwordVisuals _visuals;

    private const float VisualOffsetAngle = 90f;
    private readonly HashSet<Transform> _hitTargets = new HashSet<Transform>();
    private float _startAngle;
    private float _endAngle;

    void Awake()
    {
        _movement = GetComponent<SwordSweepMovement>() ?? gameObject.AddComponent<SwordSweepMovement>();
        _collision = GetComponent<BladeCollision>() ?? gameObject.AddComponent<BladeCollision>();
        _damageDealer = GetComponent<WeaponDamageDealer>() ?? gameObject.AddComponent<WeaponDamageDealer>();
        _visuals = GetComponent<SwordVisuals>() ?? gameObject.AddComponent<SwordVisuals>();
    }

    public void Initialize(Transform target, int damage, GameObject hitVFX, float detectorRange, Transform owner)
    {
        // Guard against serialized 0 values
        if (SweepDuration <= 0f) SweepDuration = 0.2f;
        if (SweepAngle <= 0f) SweepAngle = 120f;

        _hitTargets.Clear();

        // 1. Setup damage & visuals components
        _damageDealer.IsEnemy = IsEnemy;
        _damageDealer.SetDamage(damage);
        _visuals.Setup(detectorRange, hitVFX);

        // 2. Determine angles
        float targetAngle = 0f;
        if (target != null)
        {
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            targetAngle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg;
        }
        else
        {
            targetAngle = transform.eulerAngles.z;
        }

        // Offset target angle by 90 degrees to center the Y-aligned blade on the target
        float adjustedTargetAngle = targetAngle - VisualOffsetAngle;
        _startAngle = adjustedTargetAngle - (SweepAngle / 2f);
        _endAngle = adjustedTargetAngle + (SweepAngle / 2f);

        // 3. Register events
        _movement.OnSweepCompleted += OnSweepCompleted;
        _collision.OnHitDetected += OnHit;

        // 4. Activate movement and collision components
        _collision.Range = detectorRange;
        _collision.TargetLayer = IsEnemy ? LayerMask.GetMask("Player") : LayerMask.GetMask("Enemy");
        _collision.Activate();

        _movement.StartSweep(_startAngle, _endAngle, SweepDuration, owner);

        // Draw static boundaries in the direction of the blade (up)
        Vector3 startDir = Quaternion.Euler(0, 0, _startAngle) * Vector3.up;
        Vector3 endDir = Quaternion.Euler(0, 0, _endAngle) * Vector3.up;
        Debug.DrawLine(transform.position, transform.position + startDir * detectorRange, Color.red, SweepDuration);
        Debug.DrawLine(transform.position, transform.position + endDir * detectorRange, Color.blue, SweepDuration);
    }

    private void OnHit(Collider2D other, Vector3 hitPoint)
    {
        Transform targetTransform = other.transform;

        // Prevent hitting the same enemy target multiple times in a single sweep
        if (_hitTargets.Contains(targetTransform)) return;

        if (_damageDealer.DealDamage(other))
        {
            _hitTargets.Add(targetTransform);

            // Trigger impact visual effect and deal damage
            _visuals.SpawnHitVFX(hitPoint);

        }
    }

    private void OnSweepCompleted()
    {
        // Unsubscribe
        _movement.OnSweepCompleted -= OnSweepCompleted;
        _collision.OnHitDetected -= OnHit;

        // Deactivate collision
        _collision.Deactivate();

        // Visual hide
        _visuals.Disable();

        // Allow trail to fade out before returning to pool
        Invoke(nameof(ReturnToPool), 0.15f);
    }

    private void ReturnToPool()
    {
        _movement.OnSweepCompleted -= OnSweepCompleted;
        _collision.OnHitDetected -= OnHit;

        CancelInvoke(nameof(ReturnToPool));
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        float startAngle = transform.eulerAngles.z + VisualOffsetAngle - (SweepAngle / 2f);
        float endAngle = transform.eulerAngles.z + VisualOffsetAngle + (SweepAngle / 2f);

        Vector3 pos = transform.position;
        float radius = 3f;

        if (Application.isPlaying && _collision != null)
        {
            radius = _collision.Range;
            startAngle = _startAngle + VisualOffsetAngle;
            endAngle = _endAngle + VisualOffsetAngle;
        }

        Gizmos.color = Color.yellow;
        int segments = 20;
        Vector3 prevPoint = pos + Quaternion.Euler(0, 0, startAngle) * Vector3.right * radius;
        Gizmos.DrawLine(pos, prevPoint);
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, (float)i / segments);
            Vector3 nextPoint = pos + Quaternion.Euler(0, 0, angle) * Vector3.right * radius;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
        Gizmos.DrawLine(pos, prevPoint);
    }
}
