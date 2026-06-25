using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class AutoReturnToPool : MonoBehaviour
{
    private ParticleSystem _particleSystem;

    void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    void OnEnable()
    {
        if (_particleSystem != null)
        {
            StartCoroutine(CheckAndReturn());
        }
    }

    private IEnumerator CheckAndReturn()
    {
        yield return new WaitWhile(() => _particleSystem.IsAlive(true));

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}