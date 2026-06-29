using System;
using DG.Tweening;
using UnityEngine;

public class PopupUI : MonoBehaviour
{
    public float OpenDuration = 0.35f;
    public float CloseDuration = 0.2f;
    public Ease OpenEase = Ease.OutBack;
    public Ease CloseEase = Ease.InBack;

    private Vector3 _originalScale;

    void Awake()
    {
        _originalScale = transform.localScale;
    }

    void OnEnable()
    {
        transform.DOKill();
        transform.localScale = Vector3.zero;
        transform.DOScale(_originalScale, OpenDuration).SetEase(OpenEase).SetUpdate(true);
    }

    public void ClosePopup(Action onCompleate = null)
    {
        transform.DOKill();

        transform.DOScale(Vector3.zero, CloseDuration).SetEase(CloseEase).SetUpdate(true)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                transform.localScale = _originalScale;
                onCompleate?.Invoke();
            });
    }

    public void CloseAndOpenPopup(GameObject targetPanel)
    {
        ClosePopup(() => targetPanel.SetActive(true));
    }

    public void CloseSelectPopup(GameObject targetPanel)
    {
        ClosePopup(() => targetPanel.SetActive(false));
    }
}