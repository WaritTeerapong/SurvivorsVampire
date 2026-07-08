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

    [HideInInspector]
    public bool IsEnemy = false;

    private ProjectileMovement _movement;
    private ProjectileCollision _collision;
    private WeaponDamageDealer _damageDealer;
    private ProjectileVisuals _visuals;
    private ProjectileLifetime _lifetime;

    private void Awake()
    {
        _movement = GetComponent<ProjectileMovement>() ?? gameObject.AddComponent<ProjectileMovement>();
        _collision = GetComponent<ProjectileCollision>() ?? gameObject.AddComponent<ProjectileCollision>();
        _damageDealer = GetComponent<WeaponDamageDealer>() ?? gameObject.AddComponent<WeaponDamageDealer>();
        _visuals = GetComponent<ProjectileVisuals>() ?? gameObject.AddComponent<ProjectileVisuals>();
        _lifetime = GetComponent<ProjectileLifetime>() ?? gameObject.AddComponent<ProjectileLifetime>();
    }

    // [FIX] Replaced selfLayer with explicit isEnemy boolean
    public void Initialize(Transform target, int damage, GameObject hitVFX, bool isEnemy)
    {
        IsEnemy = isEnemy;

        // Force the object to strictly align its physics layer based on the faction
        gameObject.layer = IsEnemy ? LayerMask.NameToLayer("Enemy") : LayerMask.NameToLayer("Player");

        _movement.Speed = Speed;
        _collision.HitRadius = HitDistance;
        _collision.SetFilter(IsEnemy);
        _damageDealer.SetDamage(damage);

        _visuals.Setup(hitVFX);

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

        _collision.OnHitDetected += OnHit;
        _lifetime.OnLifetimeExpired += ReturnToPool;

        _movement.MoveInDirection(direction);
        _collision.Activate();
        _lifetime.StartCountdown();
    }

    private void OnHit(Collider2D hitCollider, Vector3 hitPoint)
    {
        _collision.OnHitDetected -= OnHit;
        _lifetime.OnLifetimeExpired -= ReturnToPool;

        _movement.Stop();
        _collision.Deactivate();
        _lifetime.StopCountdown();

        _visuals.Disable();
        _visuals.SpawnHitVFX(hitPoint);

        IDamageble damagebleObj = hitCollider.GetComponentInParent<IDamageble>();

        if (damagebleObj != null)
        {
            _damageDealer.DealDamage(damagebleObj);
        }
        else
        {
            // Debug.LogWarning("[Bullet] Hit object does not implement IDamageble.");
        }

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