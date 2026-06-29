using DG.Tweening;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }
    public Transform Target;
    public Vector3 Offset = new Vector3(0, 0, -10f);
    public float FollowSpeed = 10f;

    private Vector3 _currentPosition;
    private Vector3 _shakeOffset;

    void Awake()
    {
        if (Instance == null) Instance = this;
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
