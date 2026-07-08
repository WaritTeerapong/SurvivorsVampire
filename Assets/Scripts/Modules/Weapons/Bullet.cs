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

    private bool _isReturned = false;

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
        _movement.Speed = Speed;
        _collision.HitRadius = HitDistance;
        _collision.SetFilter(gameObject.layer);
        _damageDealer.SetDamage(damage);
        _visuals.Setup(hitVFX);

        Vector3 direction = Vector3.right;
        if (target != null)
        {
            direction = (target.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // DEBUG
            Debug.Log($"[Bullet] Spawn={transform.position} Target={target.name}@{target.position} Dir={direction} MyLayer={LayerMask.LayerToName(gameObject.layer)} FilterMask={_collision.filter.layerMask.value}");
        }
        else
        {
            Debug.LogWarning("[Bullet] target is NULL at Initialize!"); // ถ้าเจอ log นี้บน Client = เจอสาเหตุแล้ว
            transform.rotation = Quaternion.identity;
        }

        _collision.OnHitDetected += OnHit;
        _lifetime.OnLifetimeExpired += OnLifetimeExpired;

        _movement.MoveInDirection(direction);
        _collision.Activate();
        _lifetime.StartCountdown();
    }

    private void OnHit(Collider2D hitCollider, Vector3 hitPoint)
    {
        if (_isReturned) return;
        _isReturned = true;

        Cleanup();

        _visuals.SpawnHitVFX(hitPoint);

        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            IDamageble damagebleObj = hitCollider.GetComponentInParent<IDamageble>();
            if (damagebleObj != null) _damageDealer.DealDamage(damagebleObj);
        }

        ReturnToPool();
    }

    private void OnLifetimeExpired()
    {
        if (_isReturned) return;
        _isReturned = true;

        Cleanup();
        ReturnToPool();
    }

    private void Cleanup()
    {
        _collision.OnHitDetected -= OnHit;
        _lifetime.OnLifetimeExpired -= OnLifetimeExpired;

        _movement.Stop();
        _collision.Deactivate();
        _lifetime.StopCountdown();
        _visuals.Disable();
    }

    private void ReturnToPool()
    {
        CancelInvoke(nameof(ReturnToPool));
        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        else
            Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, HitDistance);
    }
}