using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    // Component Refernce
    private Rigidbody2D _rb;
    private PlayerRunTimeStats _stats; // Data
    private Detector _detector;
    private Animator _anim;

    private PlayerControls _inputs;
    private InputAction _moveAction;

    private Vector2 _position;

    [Header("Combat Setting")]
    public GameObject BulletPrefab;
    public Transform FirePoint;

    private float _atkTimer = 0f;

    public NetworkVariable<float> FacingDirection = new NetworkVariable<float>(
        1f,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            Camera.main.GetComponent<CameraController>().Target = transform;
        }

        if (IsServer && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.AddPlayer(transform);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.RemovePlayer(transform);
        }
    }

    void Awake()
    {
        _inputs = new PlayerControls();
        _rb = GetComponent<Rigidbody2D>();
        _stats = GetComponent<PlayerRunTimeStats>();
        _detector = GetComponentInChildren<Detector>();
        _anim = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        _inputs.Enable();

        _moveAction = _inputs.Player.Move;

    }

    void OnDisable()
    {
        _inputs.Disable();
    }

    void Update()
    {
        transform.localScale = new Vector3(FacingDirection.Value, 1, 1);

        if (!IsOwner) return;

        _position = _moveAction.ReadValue<Vector2>();
        _position.Normalize();

        _detector.FindNearestTarget();

        HandleAutoAttack();

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            TakeDamageRpc(10);
        }

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            _stats.DebugLogStatsRpc();
        }

        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            PlayerLevelManager.Instance.RequestGainXPRpc(100);
        }

    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        Move();
        Flip();
    }

    void HandleAutoAttack()
    {
        if (_detector.NearestTarget == null) return;

        float atkSpeed = _stats.CurrentStats.Value.ATKSpeed;

        if (atkSpeed <= 0) return;

        float atkCD = 1f / atkSpeed;

        _atkTimer += Time.deltaTime;

        if (_atkTimer >= atkCD)
        {
            _atkTimer = 0f;

            NetworkObject targetNetObj = _detector.NearestTarget.GetComponent<NetworkObject>();
            if (targetNetObj != null)
            {
                RequestFireServerRpc(targetNetObj.NetworkObjectId);
            }
        }
    }

    private void Move()
    {
        _rb.linearVelocity = _position * _stats.CurrentStats.Value.MoveSpeed;

        _anim.SetBool("IsMove", _rb.linearVelocity.sqrMagnitude >= 0.002f);
    }

    private void Flip()
    {
        if (_position.x > 0) FacingDirection.Value = 1f;
        else if (_position.x < 0) FacingDirection.Value = -1f;
    }

    [Rpc(SendTo.Server)]
    public void TakeDamageRpc(int damage)
    {
        _stats.ApplyDamage(damage);

        if (DamagePopupManager.Instance != null)
        {
            DamagePopupManager.Instance.ShowPopup(transform.position, damage, true);
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestFireServerRpc(ulong targetNetworkId)
    {
        FireClientRpc(targetNetworkId);
    }

    [Rpc(SendTo.Everyone)]
    private void FireClientRpc(ulong targetNetworkId)
    {
        if (BulletPrefab == null || ObjectPoolManager.Instance == null) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out NetworkObject targetObj))
        {
            Vector3 spawnPos = FirePoint != null ? FirePoint.position : transform.position;

            // Ask the manager for a bullet
            GameObject bulletObj = ObjectPoolManager.Instance.SpawnObject(BulletPrefab, spawnPos, Quaternion.identity, PoolCategory.Projectiles);

            if (bulletObj != null)
            {
                Bullet bulletScript = bulletObj.GetComponent<Bullet>();
                if (bulletScript != null)
                {
                    bulletScript.Initialize(targetObj.transform, _stats.CurrentStats.Value.ATKDamage);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 2f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _stats.CurrentStats.Value.ATKRange);
    }
}

