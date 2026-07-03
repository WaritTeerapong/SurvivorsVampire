using System;
using UnityEngine;

public class SwordSweepMovement : MonoBehaviour
{
    private Transform _owner;
    private float _startAngle;
    private float _endAngle;
    private float _duration;
    private float _timer;
    private bool _isSweeping = false;

    public event Action OnSweepCompleted;

    public void StartSweep(float startAngle, float endAngle, float duration, Transform owner)
    {
        _startAngle = startAngle;
        _endAngle = endAngle;
        _duration = duration;
        _owner = owner;
        _timer = 0f;
        _isSweeping = true;

        transform.rotation = Quaternion.Euler(0, 0, _startAngle);
    }

    public void StopSweep()
    {
        _isSweeping = false;
    }

    void Update()
    {
        if (!_isSweeping) return;

        if (_owner != null)
        {
            transform.position = _owner.position;
        }

        _timer += Time.deltaTime;
        float progress = Mathf.Clamp01(_timer / _duration);

        float currentAngle = Mathf.Lerp(_startAngle, _endAngle, progress);
        transform.rotation = Quaternion.Euler(0, 0, currentAngle);

        if (progress >= 1f)
        {
            _isSweeping = false;
            OnSweepCompleted?.Invoke();
        }
    }
}
