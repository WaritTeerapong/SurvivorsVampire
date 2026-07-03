using UnityEngine;

[RequireComponent(typeof(ProjectileMovement))]
[RequireComponent(typeof(ProjectileCollision))]
[RequireComponent(typeof(WeaponDamageDealer))]
[RequireComponent(typeof(ProjectileVisuals))]
[RequireComponent(typeof(ProjectileLifetime))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float Speed = 15f;
    public float HitDistance = 1f;
    public bool IsEnemy = false;

    private ProjectileMovement _movement;
    private ProjectileCollision _collision;
    private WeaponDamageDealer _damageDealer;
    private ProjectileVisuals _visuals;
    private ProjectileLifetime _lifetime;

    void Awake()
    {
        _movement = GetComponent<ProjectileMovement>() ?? gameObject.AddComponent<ProjectileMovement>();
        _collision = GetComponent<ProjectileCollision>() ?? gameObject.AddComponent<ProjectileCollision>();
        _damageDealer = GetComponent<WeaponDamageDealer>() ?? gameObject.AddComponent<WeaponDamageDealer>();
        _visuals = GetComponent<ProjectileVisuals>() ?? gameObject.AddComponent<ProjectileVisuals>();
        _lifetime = GetComponent<ProjectileLifetime>() ?? gameObject.AddComponent<ProjectileLifetime>();
    }

    public void Initialize(Transform target, int damage, GameObject hitVFX)
    {
        // Set dynamic properties on the sub-components from config values
        _movement.Speed = Speed;
        _collision.HitRadius = HitDistance;
        _collision.TargetLayer = IsEnemy ? LayerMask.GetMask("Player") : LayerMask.GetMask("Enemy");
        _damageDealer.IsEnemy = IsEnemy;
        _damageDealer.SetDamage(damage);

        _visuals.Setup(hitVFX);

        // Set direction
        Vector3 direction = Vector3.right;
        if (target != null)
        {
            direction = (target.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }

        // Register events
        _collision.OnHitDetected += OnHit;
        _lifetime.OnLifetimeExpired += ReturnToPool;

        // Activate components
        _movement.MoveInDirection(direction);
        _collision.Activate();
        _lifetime.StartCountdown();
    }

    private void OnHit(Collider2D hitCollider, Vector3 hitPoint)
    {
        // Unsubscribe to avoid double execution
        _collision.OnHitDetected -= OnHit;
        _lifetime.OnLifetimeExpired -= ReturnToPool;

        // Stop updates
        _movement.Stop();
        _collision.Deactivate();
        _lifetime.StopCountdown();

        // Handle impact
        _visuals.Disable();
        _visuals.SpawnHitVFX(hitPoint);
        _damageDealer.DealDamage(hitCollider);

        // Pool cleanup delay
        Invoke(nameof(ReturnToPool), 0.1f);
    }

    private void ReturnToPool()
    {
        _collision.OnHitDetected -= OnHit;
        _lifetime.OnLifetimeExpired -= ReturnToPool;
        
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