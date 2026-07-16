using UnityEngine;

public class UIVFXManager : MonoBehaviour
{
    public static UIVFXManager Instance { get; private set; }

    public GameObject ButtonClickVFXPrefab;
    public Transform UIParticleCanvas;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlayClickVFX(Vector2 screenPosition, Transform clickedButtonTransform)
    {
        if (ButtonClickVFXPrefab == null || ObjectPoolManager.Instance == null) return;

        GameObject ps = ObjectPoolManager.Instance.SpawnObject<GameObject>(
            ButtonClickVFXPrefab,
            screenPosition,
            Quaternion.identity,
            PoolCategory.UIVFX,
            UIParticleCanvas
        );

        if (ps != null)
        {
            if (ps.TryGetComponent(out ParticleSystem pss))
            {
                var mainModule = pss.main;
                mainModule.useUnscaledTime = true;

                pss.transform.SetAsLastSibling();
                pss.Play();
            }
        }
    }
}