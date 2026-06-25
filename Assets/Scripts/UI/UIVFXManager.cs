using UnityEngine;

public class UIVFXManager : MonoBehaviour
{
    public static UIVFXManager Instance { get; private set; }

    public GameObject ButtonClickVFXPrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlayClickVFX(Vector2 screenPosition, Transform clickedButtonTransform)
    {
        if (ButtonClickVFXPrefab == null || ObjectPoolManager.Instance == null) return;

        ParticleSystem ps = ObjectPoolManager.Instance.SpawnObject<ParticleSystem>(
            ButtonClickVFXPrefab,
            screenPosition,
            Quaternion.identity,
            PoolCategory.UIVFX
        );

        if (ps != null)
        {
            ps.transform.SetAsLastSibling();
            ps.Play();
        }
    }
}