using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarUI : MonoBehaviour
{
    public PlayerRunTimeStats PlayerStats;
    public Transform CharacterTransform;

    public Slider FrontHealthSlider;
    public Slider BackSmoothSlider;

    public float SmoothSpeed = 5f;

    private Vector3 _originalScale;

    void Start()
    {
        if (PlayerStats == null) PlayerStats = GetComponentInParent<PlayerRunTimeStats>();
        if (CharacterTransform == null) CharacterTransform = PlayerStats.transform;

        _originalScale = transform.localScale;

        if (PlayerStats != null)
        {
            PlayerStats.CurrentStats.OnValueChanged += OnHealthChanged;
            UpdateHealthBar(PlayerStats.CurrentStats.Value.MaxHealth, PlayerStats.CurrentStats.Value.CurrentHealth, true);
        }
    }

    private void OnDestroy()
    {
        if (PlayerStats != null)
        {
            PlayerStats.CurrentStats.OnValueChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged(PlayerStats previoysValue, PlayerStats newValue)
    {
        UpdateHealthBar(newValue.MaxHealth, newValue.CurrentHealth, false);
    }

    private void UpdateHealthBar(int maxHealth, int currentHealth, bool isInit)
    {
        FrontHealthSlider.maxValue = maxHealth;
        BackSmoothSlider.maxValue = maxHealth;

        FrontHealthSlider.value = currentHealth;

        if (isInit)
        {
            BackSmoothSlider.value = currentHealth;
        }
    }

    private void LateUpdate()
    {
        if (BackSmoothSlider.value > FrontHealthSlider.value)
        {
            BackSmoothSlider.value = Mathf.Lerp(BackSmoothSlider.value, FrontHealthSlider.value, Time.deltaTime * SmoothSpeed);
        }
        else
        {
            BackSmoothSlider.value = FrontHealthSlider.value;
        }

        if (CharacterTransform != null)
        {
            float parentSign = Mathf.Sign(CharacterTransform.localScale.x);

            transform.localScale = new Vector3(_originalScale.x * parentSign, _originalScale.y, _originalScale.z);
        }
    }
}
