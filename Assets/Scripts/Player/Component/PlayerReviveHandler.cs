using DG.Tweening;
using UnityEngine;

public class PlayerReviveHandler : MonoBehaviour
{
    private Player _player;
    private CircleCollider2D _reviveCol;

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
        _player = GetComponentInParent<Player>();
        _reviveCol = GetComponent<CircleCollider2D>();

        _reviveCol.radius = ReviveZoneRadius > 0 ? ReviveZoneRadius : 2f;
        if (!_reviveCol.isTrigger) _reviveCol.isTrigger = true;

        _reviveCol.enabled = false;

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
        _reviveCol.enabled = _isZoneOpen;

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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Player reviver = other.GetComponentInParent<Player>();

        if (reviver != null && reviver.IsOwner && !reviver.IsDowned)
        {
            _player.UpdateReviverCountServerRpc(1);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Player reviver = other.GetComponentInParent<Player>();

        if (reviver != null && reviver.IsOwner && !reviver.IsDowned)
        {
            _player.UpdateReviverCountServerRpc(-1);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _isZoneOpen ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, ReviveZoneRadius > 0 ? ReviveZoneRadius : 2f);
    }
}