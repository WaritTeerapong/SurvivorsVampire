using DG.Tweening;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("=== Target Settings ===")]
    public Transform Target;
    public Vector3 Offset = new Vector3(0, 0, -10f);

    [Header("=== Movement Settings ===")]
    public float FollowSpeed = 10f;

    private Camera _camera;
    private Vector3 _currentPosition;
    private Vector3 _shakeOffset;

    void Awake()
    {
        if (Instance == null) Instance = this;

        _camera = GetComponent<Camera>();
        if (_camera == null) _camera = Camera.main;
    }

    void Start()
    {
        _currentPosition = transform.position;
    }

    void LateUpdate()
    {
        if (Target != null)
        {
            Vector3 targetPos = Target.position + Offset;

            if (EnemySpawnManager.Instance != null)
            {
                Vector2 mapSize = EnemySpawnManager.Instance.MapSize;

                float camHeight = _camera.orthographicSize;
                float camWidth = camHeight * _camera.aspect;

                float minX = -(mapSize.x / 2f) + camWidth;
                float maxX = (mapSize.x / 2f) - camWidth;
                float minY = -(mapSize.y / 2f) + camHeight;
                float maxY = (mapSize.y / 2f) - camHeight;

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

                targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
                targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
            }

            _currentPosition = Vector3.Lerp(_currentPosition, targetPos, FollowSpeed * Time.deltaTime);

            transform.position = _currentPosition + _shakeOffset;
        }
    }

    public void TriggerShake(float duration = 0.2f, float strength = 0.5f)
    {
        DOTween.Kill(this);
        _shakeOffset = Vector3.zero;

        DOTween.Shake(() => _shakeOffset, x => _shakeOffset = x, duration, strength, 10, 90, false).SetTarget(this);
    }
}
