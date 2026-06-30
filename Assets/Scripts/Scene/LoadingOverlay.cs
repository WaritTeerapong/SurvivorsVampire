using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class LoadingOverlay : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeInTime = 0.5f;
    [SerializeField] private float fadeOutTime = 0.5f;

    public IEnumerator FadeInBlack()
    {
        yield return FadeTo(1f, fadeInTime);
    }

    public IEnumerator FadeOutBlack()
    {
        yield return FadeTo(0f, fadeOutTime);
    }

    
    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        // Alpha is The opacity of the UI elements in canva group
        // 0 = fully transparent
        // 1 = fully opaque
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }
}
