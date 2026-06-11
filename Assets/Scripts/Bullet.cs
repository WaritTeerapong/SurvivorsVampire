using Unity.Netcode;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float Speed = 15f;
    public float HitDistance = 1f;
    public bool IsEnemy = false;

    private Transform _firePoint;
    private Transform _target;
    private int _damage;
    private bool _isFired;

    private Vector3 _shootDirection;
    private float _lifeTimer = 5f;

    public void Initialize(Transform target, int damage)
    {
        _target = target;
        _damage = damage;
        _lifeTimer = 5f;
        _isFired = true;

        // ✅ คำนวณ "ทิศทาง (Direction)" เอาไว้ตั้งแต่ตอนกดยิง (สำหรับ Non-Lock)
        if (target != null)
        {
            _shootDirection = (target.position - transform.position).normalized;

            // (Optional) หันหัวกระสุนให้ตรงกับทิศทางที่พุ่งไป
            float angle = Mathf.Atan2(_shootDirection.y, _shootDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else
        {
            _shootDirection = Vector3.right; // เผื่อเหนียวกรณีเป้าหมายหายไปกระทันหัน
        }
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

        if (!IsEnemy)
        {
            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                ReturnToPool();
                return;
            }
            transform.position = Vector3.MoveTowards(transform.position, _target.position, Speed * Time.deltaTime);
        }
        else
        {
            transform.position += _shootDirection * Speed * Time.deltaTime;
        }

        if (_target != null && _target.gameObject.activeInHierarchy)
        {
            // Check impact
            float shotdistance = (_target.position - transform.position).sqrMagnitude;
            if (shotdistance <= (HitDistance * HitDistance))
            {
                HitTarget();
            }
            // if (Vector3.Distance(transform.position, _target.position) <= HitDistance)
            // {
            //     HitTarget();
            // }
        }

    }

    private void HitTarget()
    {
        // Only the Server applies damage
        if (NetworkManager.Singleton.IsServer)
        {
            if (!IsEnemy)
            {
                Enemy enemy = _target.GetComponent<Enemy>();
                if (enemy != null) enemy.TakeDamage(_damage);
            }
            else if (IsEnemy)
            {
                PlayerController player = _target.GetComponent<PlayerController>();
                if (player != null) player.TakeDamageRpc(_damage);
            }
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        _isFired = false;

        // Return this game object to the global pool manager
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        }
        else
        {
            Destroy(gameObject); // Fallback if manager is destroyed
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, HitDistance);
    }
}