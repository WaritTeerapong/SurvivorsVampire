using DG.Tweening;
using UnityEngine;

public class PlayerReviveHandler : MonoBehaviour
{
    [Header("=== Revive Settings ===")]
    public float ReviveZoneRadius = 2f;
    private bool _isZoneOpen = false;

    [Header("=== Visual Settings ===")]
    public GameObject ReviveZoneVisual;
    public float PulseDuration = 0.8f;
    public float PulseScaleMultiplier = 1.1f;

    private float _visualBaseScale = 1f;

    void Awake()
    {
        if (ReviveZoneVisual != null)
        {
            SpriteRenderer sr = ReviveZoneVisual.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                float spriteBaseWidth = sr.sprite.bounds.size.x;
                float targetDiameter = ReviveZoneRadius * 2f;
                _visualBaseScale = targetDiameter / spriteBaseWidth;
            }

            ReviveZoneVisual.SetActive(false);
        }
    }

    public void SetReviveZoneActive(bool isActive)
    {
        if (_isZoneOpen == isActive) return;

        _isZoneOpen = isActive;

        if (ReviveZoneVisual != null)
        {
            ReviveZoneVisual.SetActive(_isZoneOpen);

            if (_isZoneOpen)
            {
                ReviveZoneVisual.transform.localScale = Vector3.one * _visualBaseScale;

                float targetPulseScale = _visualBaseScale * PulseScaleMultiplier;

                ReviveZoneVisual.transform
                    .DOScale(targetPulseScale, PulseDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
            else
            {
                ReviveZoneVisual.transform.DOKill();
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _isZoneOpen ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, ReviveZoneRadius > 0 ? ReviveZoneRadius : 2f);
    }
}