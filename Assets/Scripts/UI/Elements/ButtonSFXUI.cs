using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class ButtonSFXUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("=== Juice Settings ===")]
    public bool EnableJuice = true;
    public float HoverScale = 1.1f;
    public float ClickScale = 0.9f;
    public float AnimationDuration = 0.2f;

    private Vector3 _originalScale;
    private Button _button;

    private void Awake()
    {
        _originalScale = transform.localScale;
        _button = GetComponent<Button>();
    }

    private void OnDisable()
    {
        transform.DOKill();
        transform.localScale = _originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!EnableJuice || (_button != null && !_button.interactable)) return;

        transform.DOKill();
        transform.DOScale(_originalScale * HoverScale, AnimationDuration)
                 .SetEase(Ease.OutBack)
                 .SetUpdate(true);

        // PLAY HOVER SFX
        AudioManager.Instance?.PlayUI("Hover");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!EnableJuice || (_button != null && !_button.interactable)) return;

        transform.DOKill();
        transform.DOScale(_originalScale, AnimationDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!EnableJuice || (_button != null && !_button.interactable)) return;

        transform.DOKill();
        transform.DOScale(_originalScale * ClickScale, AnimationDuration / 2f).SetEase(Ease.OutQuad).SetUpdate(true);

        if (UIVFXManager.Instance != null)
        {
            Debug.Log("Created VFX");
            UIVFXManager.Instance.PlayClickVFX(eventData.position, transform);
        }

        // PLAY CLICK SFX
        AudioManager.Instance?.PlayUI("Click");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!EnableJuice || (_button != null && !_button.interactable)) return;

        transform.DOKill();
        transform.DOScale(_originalScale * HoverScale, AnimationDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }
}