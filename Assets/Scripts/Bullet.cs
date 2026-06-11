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

    public void Initialize(Transform target, int damage)
    {
        _target = target;
        _damage = damage;
        _lifeTimer = 5f;
        _isFired = true;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.radius = HitDistance;
        }

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

        // ✅ 1. จดจำตำแหน่ง "ก่อนเดิน" เอาไว้
        Vector3 previousPosition = transform.position;

        // --- การเคลื่อนที่ ---
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

        // ✅ 2. ส่งตำแหน่งเก่าไปให้ฟังก์ชันเช็กการชนแบบลากเส้น
        CheckCollision(previousPosition);
    }

    private void CheckCollision(Vector3 previousPosition)
    {
        Vector3 currentPosition = transform.position;
        Vector3 direction = currentPosition - previousPosition;
        float distanceMoveThisFrame = direction.magnitude;

        // ดักบั๊กกรณีที่เฟรมนี้กระสุนยังไม่ได้ขยับ ให้ใช้ OverlapCircle แบบเดิม
        if (distanceMoveThisFrame == 0)
        {
            Collider2D hitCollider = Physics2D.OverlapCircle(currentPosition, HitDistance, _targetLayer);
            if (hitCollider != null) ProcessHit(hitCollider);
            return;
        }

        // ✅ 3. อัปเกรด: ใช้ CircleCast! (กวาดวงกลมจากจุดเก่า ไปยังจุดใหม่)
        // มันจะกวาดเช็กตลอดทางเดินของกระสุนในเฟรมนั้นๆ ทำให้ไม่มีทางทะลุเป้าหมายได้เลย
        RaycastHit2D hit = Physics2D.CircleCast(previousPosition, HitDistance, direction.normalized, distanceMoveThisFrame, _targetLayer);

        if (hit.collider != null)
        {
            ProcessHit(hit.collider);
        }
    }

    private void ProcessHit(Collider2D hitCollider)
    {
        if (!IsEnemy && hitCollider.CompareTag("Enemy"))
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                if (NetworkManager.Singleton.IsServer) enemy.TakeDamage(_damage);
                ReturnToPool();
            }
        }
        else if (IsEnemy && hitCollider.CompareTag("Player"))
        {
            PlayerController player = hitCollider.GetComponent<PlayerController>();
            if (player != null)
            {
                if (NetworkManager.Singleton.IsServer) player.TakeDamageRpc(_damage);
                ReturnToPool();
            }
        }
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