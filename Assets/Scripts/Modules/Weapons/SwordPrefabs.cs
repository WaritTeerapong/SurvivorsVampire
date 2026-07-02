using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SwordPrefabs : MonoBehaviour
{
    [Header("Sweep Settings")]
    public float SweepAngle;
    public float SweepDuration;
    public bool IsEnemy = false;

    private int _damage;
    private GameObject _hitVFXPrefab;
    private List<Collider2D> _overlapResults = new List<Collider2D>();
    private HashSet<Transform> _hitTargets = new HashSet<Transform>();

    private Collider2D _collider;
    private float _startAngle;
    private float _endAngle;
    private float _timer;
    private bool _isSweeping = false;

    private const float VisualOffsetAngle = 90f;

    private LayerMask _targetLayer;
    private TrailRenderer _trail;
    private SpriteRenderer _spriteRenderer;
    private float _detectorRange;
    private Transform _owner;

    void Awake()
    {
        _collider = GetComponentInChildren<Collider2D>();
        if (_collider != null)
        {
            _collider.isTrigger = true;
        }
        _trail = GetComponentInChildren<TrailRenderer>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Initialize(Transform target, int damage, GameObject hitVFX, float detectorRange, Transform owner)
    {
        // Guard against serialized 0 values
        if (SweepDuration <= 0f) SweepDuration = 0.2f;
        if (SweepAngle <= 0f) SweepAngle = 120f;

        _damage = damage;
        _hitVFXPrefab = hitVFX;
        _hitTargets.Clear();
        _timer = 0f;
        _detectorRange = detectorRange;
        _owner = owner;

        // Re-enable components
        if (_collider != null) _collider.enabled = true;
        if (_spriteRenderer != null) _spriteRenderer.enabled = true;
        if (_trail != null)
        {
            _trail.Clear();
            _trail.enabled = true;
        }

        // Determine angles
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

        // Dynamically scale and offset the child GameObject (which holds the sprite, collider, and trail)
        if (_collider != null)
        {
            Transform childTransform = _collider.transform;
            // Scale Y to match the detector range
            Vector3 localScale = childTransform.localScale;
            childTransform.localScale = new Vector3(localScale.x, detectorRange, localScale.z);
            // Position Y at half of the detector range to keep the handle pivot at (0,0)
            childTransform.localPosition = new Vector3(0f, detectorRange / 2f, 0f);
        }

        // Offset target angle by 90 degrees to center the Y-aligned blade on the target
        float adjustedTargetAngle = targetAngle - VisualOffsetAngle;

        _startAngle = adjustedTargetAngle - (SweepAngle / 2f);
        _endAngle = adjustedTargetAngle + (SweepAngle / 2f);

        transform.rotation = Quaternion.Euler(0, 0, _startAngle);
        _isSweeping = true;

        if (IsEnemy) _targetLayer = LayerMask.GetMask("Player");
        else _targetLayer = LayerMask.GetMask("Enemy");

        // Draw static boundaries in the direction of the blade (up)
        Vector3 startDir = Quaternion.Euler(0, 0, _startAngle) * Vector3.up;
        Vector3 endDir = Quaternion.Euler(0, 0, _endAngle) * Vector3.up;
        Debug.DrawLine(transform.position, transform.position + startDir * _detectorRange, Color.red, SweepDuration);
        Debug.DrawLine(transform.position, transform.position + endDir * _detectorRange, Color.blue, SweepDuration);
    }

    void Update()
    {
        if (!_isSweeping) return;

        if (_owner != null)
        {
            transform.position = _owner.position;
        }

        _timer += Time.deltaTime;
        float progress = Mathf.Clamp01(_timer / SweepDuration);

        // Sweep rotation
        float currentAngle = Mathf.Lerp(_startAngle, _endAngle, progress);
        transform.rotation = Quaternion.Euler(0, 0, currentAngle);

        // Draw dynamic sweep line matching the blade direction (transform.up)
        Debug.DrawLine(transform.position, transform.position + transform.up * _detectorRange, Color.yellow);

        CheckCollisions();

        if (progress >= 1f)
        {
            FinishSweep();
        }
    }

    private void CheckCollisions()
    {
        if (_collider == null) return;

        ContactFilter2D filter = new ContactFilter2D 
        { 
            useTriggers = true,
            useLayerMask = true,
            layerMask = _targetLayer
        };
        int count = _collider.Overlap(filter, _overlapResults);

        for (int i = 0; i < count; i++)
        {
            Collider2D other = _overlapResults[i];
            Transform targetTransform = null;
            System.Action takeDamageAction = null;

            if (!IsEnemy && other.CompareTag("Enemy"))
            {
                Enemy enemy = other.GetComponentInParent<Enemy>();
                if (enemy != null)
                {
                    targetTransform = enemy.transform;
                    takeDamageAction = () => enemy.TakeDamage(_damage);
                }
                else
                {
                    Boss boss = other.GetComponentInParent<Boss>();
                    if (boss != null)
                    {
                        targetTransform = boss.transform;
                        takeDamageAction = () => boss.TakeDamage(_damage);
                    }
                }
            }
            else if (IsEnemy && other.CompareTag("Player"))
            {
                Player player = other.GetComponentInParent<Player>();
                if (player != null)
                {
                    targetTransform = player.transform;
                    takeDamageAction = () => player.TakeDamageRpc(_damage);
                }
            }

            if (targetTransform == null || _hitTargets.Contains(targetTransform)) continue;

            _hitTargets.Add(targetTransform);
            ProcessHit(other, takeDamageAction);
        }
    }

    private void ProcessHit(Collider2D hitCollider, System.Action takeDamageAction)
    {
        if (_hitVFXPrefab != null && ObjectPoolManager.Instance != null)
        {
            ParticleSystem ps = ObjectPoolManager.Instance.SpawnObject<ParticleSystem>(
                _hitVFXPrefab,
                hitCollider.transform.position,
                Quaternion.identity,
                PoolCategory.Default
            );
            if (ps != null) ps.Play();
        }

        if (NetworkManager.Singleton.IsServer)
        {
            takeDamageAction?.Invoke();
        }
    }

    private void FinishSweep()
    {
        _isSweeping = false;

        // Disable collider and visual rendering so it's done hitting/showing
        if (_collider != null) _collider.enabled = false;
        if (_spriteRenderer != null) _spriteRenderer.enabled = false;

        // Allow trail to fade out before returning to pool
        Invoke(nameof(ReturnToPool), 0.15f);
    }

    private void ReturnToPool()
    {
        _isSweeping = false;
        CancelInvoke(nameof(ReturnToPool));
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        // Adjust angles for Gizmo visualization since the blade points along Y axis
        float startAngle = _isSweeping ? _startAngle + VisualOffsetAngle : transform.eulerAngles.z;
        float endAngle = _isSweeping ? _endAngle + VisualOffsetAngle : transform.eulerAngles.z + SweepAngle;

        if (!_isSweeping)
        {
            startAngle = transform.eulerAngles.z + VisualOffsetAngle - (SweepAngle / 2f);
            endAngle = transform.eulerAngles.z + VisualOffsetAngle + (SweepAngle / 2f);
        }
        
        Vector3 pos = transform.position;
        float radius = 3f;

        Collider2D col = _collider != null ? _collider : GetComponentInChildren<Collider2D>();
        if (col is CircleCollider2D circleCol)
        {
            radius = circleCol.radius * col.transform.localScale.y;
        }
        else if (col is BoxCollider2D boxCol)
        {
            radius = boxCol.size.y * col.transform.localScale.y;
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
