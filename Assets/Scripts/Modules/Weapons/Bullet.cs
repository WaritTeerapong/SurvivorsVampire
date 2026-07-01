using Unity.Netcode;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float Speed = 15f;
    public float HitDistance = 1f;
    public bool IsEnemy = false;

    private Transform _target;
    private int _damage;
    private bool _isFired;

    private LayerMask _targetLayer;
    private Vector3 _shootDirection;
    private float _lifeTimer = 5f;

    private GameObject _hitVFXPrefab;
    private SpriteRenderer _spriteRenderer;
    private Collider2D _collider;

    private TrailRenderer _trail;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        _trail = GetComponent<TrailRenderer>();
    }

    public void Initialize(Transform target, int damage, GameObject hitVFX)
    {
        _target = target;
        _damage = damage;
        _hitVFXPrefab = hitVFX;
        _lifeTimer = 5f;
        _isFired = true;

        if (_spriteRenderer != null) _spriteRenderer.enabled = true;
        if (_collider != null) _collider.enabled = true;

        if (_trail != null) _trail.Clear();

        CircleCollider2D col = _collider as CircleCollider2D;
        if (col != null)
        {
            col.radius = HitDistance;
        }

        if (target != null)
        {
            _shootDirection = (target.position - transform.position).normalized;
            float angle = Mathf.Atan2(_shootDirection.y, _shootDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else
        {
            _shootDirection = Vector3.right;
        }

        if (IsEnemy) _targetLayer = LayerMask.GetMask("Player");
        else _targetLayer = LayerMask.GetMask("Enemy");
    }

    private void Update()
    {
        if (!_isFired) return;

        _lifeTimer -= Time.deltaTime;
        if (_lifeTimer <= 0)
        {
            ReturnToPool();
            return;
        }

        Vector3 previousPosition = transform.position;
        float distanceMoveThisFrame = Speed * Time.deltaTime;
        transform.position += _shootDirection * distanceMoveThisFrame;

        RaycastHit2D hit = Physics2D.CircleCast(previousPosition, HitDistance, _shootDirection, distanceMoveThisFrame, _targetLayer);

        if (hit.collider != null)
        {
            ProcessHit(hit.collider);
        }
    }

    private void ProcessHit(Collider2D hitCollider)
    {
        _isFired = false;

        if (_spriteRenderer != null) _spriteRenderer.enabled = false;
        if (_collider != null) _collider.enabled = false;

        if (_hitVFXPrefab != null && ObjectPoolManager.Instance != null)
        {
            ParticleSystem ps = ObjectPoolManager.Instance.SpawnObject<ParticleSystem>(
                _hitVFXPrefab,
                transform.position,
                Quaternion.identity,
                PoolCategory.Default
            );
            if (ps != null) ps.Play();
        }

        if (NetworkManager.Singleton.IsServer)
        {
            if (!IsEnemy && hitCollider.CompareTag("Enemy"))
            {
                Enemy enemy = hitCollider.GetComponentInParent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(_damage);
                }
                else
                {
                    Boss boss = hitCollider.GetComponentInParent<Boss>();
                    if (boss != null)
                    {
                        boss.TakeDamage(_damage);
                    }
                }
            }
            else if (IsEnemy && hitCollider.CompareTag("Player"))
            {
                Player player = hitCollider.GetComponentInParent<Player>();
                if (player != null) player.TakeDamageRpc(_damage);
            }
        }

        Invoke(nameof(ReturnToPool), 0.1f);
    }

    private void ReturnToPool()
    {
        _isFired = false;
        CancelInvoke(nameof(ReturnToPool));

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, HitDistance);
    }
}