using System;
using UnityEngine;

public class ProjectileLifetime : MonoBehaviour
{
    [HideInInspector]
    public float MaxLifetime = 5f;
    private float _timer;
    private bool _isRunning = false;

    public event Action OnLifetimeExpired;

    public void StartCountdown()
    {
        _timer = MaxLifetime;
        _isRunning = true;
    }

    public void StopCountdown()
    {
        _isRunning = false;
    }

    void Update()
    {
        if (!_isRunning) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            OnLifetimeExpired?.Invoke();
        }
    }
}
