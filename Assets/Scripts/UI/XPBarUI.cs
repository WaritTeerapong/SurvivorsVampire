using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class XPBarUI : NetworkBehaviour
{
    [SerializeField] private GameObject _xpBar;

    [Header("Interface")]
    [SerializeField] private TMP_Text _levelText; // Current Level
    [SerializeField] private TMP_Text _xpText; // Current XP
    [SerializeField] private Slider _xpSlider; // Current XP Value

    private int _visualLevel = -1;
    private bool _needUpdate = false;
    private Image _fillImage;
    private Color _originalFillColor;

    void Start()
    {
        if (_xpSlider.fillRect != null)
        {
            _fillImage = _xpSlider.fillRect.GetComponent<Image>();
            if (_fillImage != null) _originalFillColor = _fillImage.color;
        }

        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.SharedLevel.OnValueChanged += OnXPChanged;
            PlayerLevelManager.Instance.SharedXP.OnValueChanged += OnXPChanged;
            PlayerLevelManager.Instance.SharedXPNeeded.OnValueChanged += OnXPChanged;
        }

        _needUpdate = true;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.SharedLevel.OnValueChanged -= OnXPChanged;
            PlayerLevelManager.Instance.SharedXP.OnValueChanged -= OnXPChanged;
            PlayerLevelManager.Instance.SharedXPNeeded.OnValueChanged -= OnXPChanged;
        }
    }

    private void OnXPChanged(int previosValue, int newValue)
    {
        _needUpdate = true;
    }

    void LateUpdate()
    {
        if (_needUpdate)
        {
            _needUpdate = false;
            ProcessUIUpdate();
        }
    }

    void ProcessUIUpdate()
    {
        if (PlayerLevelManager.Instance == null) return;

        int currentLevel = PlayerLevelManager.Instance.SharedLevel.Value;
        int currentXP = PlayerLevelManager.Instance.SharedXP.Value;
        int xpNeeded = PlayerLevelManager.Instance.SharedXPNeeded.Value;

        if (_visualLevel == -1)
        {
            _visualLevel = currentLevel;
            _xpSlider.maxValue = xpNeeded;
            _xpSlider.value = currentXP;
            _levelText.text = currentLevel.ToString();
            _xpText.text = $"{currentXP} / {xpNeeded}";
            return;
        }

        if (xpNeeded == -1) // MAX LEVEL
        {
            _xpText.text = "MAX";
            _xpSlider.maxValue = 1;
            _xpSlider.DOValue(1, 0.3f).SetUpdate(true);
            return;
        }

        if (currentLevel > _visualLevel)
        {
            _visualLevel = currentLevel;
            _xpText.text = $"{currentXP} / {xpNeeded}";

            Sequence seq = DOTween.Sequence();

            seq.SetUpdate(true);

            seq.Append(_xpSlider.DOValue(_xpSlider.maxValue, 0.15f).SetEase(Ease.OutQuad));

            if (_fillImage != null)
            {
                seq.Append(_fillImage.DOColor(Color.white, 0.05f));
                seq.Append(_fillImage.DOColor(_originalFillColor, 0.1f));
            }

            seq.AppendCallback(() =>
            {
                _xpSlider.maxValue = xpNeeded;
                _xpSlider.value = 0;
                _levelText.text = currentLevel.ToString();

                _levelText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 1).SetUpdate(true);
            });

            seq.Append(_xpSlider.DOValue(currentXP, 0.2f).SetEase(Ease.OutQuad));
        }
        else
        {
            _xpText.text = $"{currentXP} / {xpNeeded}";
            _xpSlider.maxValue = xpNeeded;
            _xpSlider.DOValue(currentXP, 0.2f).SetEase(Ease.OutCubic).SetUpdate(true);
        }
    }
}
