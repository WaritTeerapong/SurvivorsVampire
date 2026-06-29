using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [Header("=== Boundary Settings ===")]
    [SerializeField] private float _sidePadding = 0.5f;
    [SerializeField] private float _topPadding = 0.5f;
    [SerializeField] private float _bottomPadding = 0.5f;

    [Header("=== Ghost Setting ===")]
    public float GhostMoveSpeed = 7f;

    private Rigidbody2D _rb;
    private Vector2 _cachedMapSize;
    private bool _isMapCached = false;

    public NetworkVariable<float> FacingDirection = new NetworkVariable<float>
    (
        1f,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Owner
    );

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    void Update()
    {
        transform.localScale = new Vector3(FacingDirection.Value, 1, 1);
    }

    void LateUpdate()
    {
        if (!IsOwner) return;

        // Cache the MapSize once to improve performance
        if (!_isMapCached)
        {
            if (EnemySpawnManager.Instance != null)
            {
                _cachedMapSize = EnemySpawnManager.Instance.MapSize;
                _isMapCached = true;
            }
            else
            {
                return;
            }
        }

        // Calculate limits based on cached MapSize and directional paddings
        float minX = -(_cachedMapSize.x / 2f) + _sidePadding;
        float maxX = (_cachedMapSize.x / 2f) - _sidePadding;
        float minY = -(_cachedMapSize.y / 2f) + _bottomPadding;
        float maxY = (_cachedMapSize.y / 2f) - _topPadding;

        // Fail-safe to prevent inverted bounds
        if (minX > maxX)
        {
            minX = 0f;
            maxX = 0f;
        }
        if (minY > maxY)
        {
            minY = 0f;
            maxY = 0f;
        }

        // Apply clamping
        Vector3 currentPos = transform.position;
        currentPos.x = Mathf.Clamp(currentPos.x, minX, maxX);
        currentPos.y = Mathf.Clamp(currentPos.y, minY, maxY);

        transform.position = currentPos;
    }

    public void Move(Vector2 direction, float speed)
    {
        _rb.linearVelocity = direction * speed;

        if (direction.x > 0) FacingDirection.Value = 1f;
        else if (direction.x < 0) FacingDirection.Value = -1f;
    }

    public void Stop() => _rb.linearVelocity = Vector2.zero;

    private void OnDrawGizmosSelected()
    {
        Vector2 mapSize = Vector2.zero;

        if (Application.isPlaying && _isMapCached)
        {
            mapSize = _cachedMapSize;
        }
        else if (!Application.isPlaying)
        {
            EnemySpawnManager manager = FindAnyObjectByType<EnemySpawnManager>();
            if (manager != null)
            {
                mapSize = manager.MapSize;
            }
        }

        if (mapSize != Vector2.zero)
        {
            float minX = -(mapSize.x / 2f) + _sidePadding;
            float maxX = (mapSize.x / 2f) - _sidePadding;
            float minY = -(mapSize.y / 2f) + _bottomPadding;
            float maxY = (mapSize.y / 2f) - _topPadding;

            if (minX > maxX) { minX = 0f; maxX = 0f; }
            if (minY > maxY) { minY = 0f; maxY = 0f; }

            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
            Vector3 size = new Vector3(maxX - minX, maxY - minY, 0f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}