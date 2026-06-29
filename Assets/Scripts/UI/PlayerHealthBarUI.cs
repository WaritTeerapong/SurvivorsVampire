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
    private float _targetHealth;

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

    private void OnHealthChanged(PlayerStats previousValue, PlayerStats newValue)
    {
        UpdateHealthBar(newValue.MaxHealth, newValue.CurrentHealth, false);
    }

    private void UpdateHealthBar(int maxHealth, int currentHealth, bool isInit)
    {
        FrontHealthSlider.maxValue = maxHealth;
        BackSmoothSlider.maxValue = maxHealth;

        _targetHealth = currentHealth;

        if (isInit)
        {
            FrontHealthSlider.value = currentHealth;
            BackSmoothSlider.value = currentHealth;
        }
        else if (_targetHealth > FrontHealthSlider.value) // Healing / Reviving
        {
            BackSmoothSlider.value = _targetHealth; // Back slider jumps instantly
        }
        else if (_targetHealth < FrontHealthSlider.value) // Taking Damage
        {
            FrontHealthSlider.value = _targetHealth; // Front slider drops instantly
        }
    }

    private void LateUpdate()
    {
        // Healing Lerp (Front catches up to Back)
        if (_targetHealth > FrontHealthSlider.value)
        {
            FrontHealthSlider.value = Mathf.Lerp(FrontHealthSlider.value, BackSmoothSlider.value, Time.deltaTime * SmoothSpeed);
            if (Mathf.Abs(BackSmoothSlider.value - FrontHealthSlider.value) < 0.1f)
            {
                FrontHealthSlider.value = BackSmoothSlider.value;
            }
        }
        // Damaged Lerp (Back catches up to Front)
        else if (_targetHealth < BackSmoothSlider.value)
        {
            BackSmoothSlider.value = Mathf.Lerp(BackSmoothSlider.value, FrontHealthSlider.value, Time.deltaTime * SmoothSpeed);
            if (Mathf.Abs(BackSmoothSlider.value - FrontHealthSlider.value) < 0.1f)
            {
                BackSmoothSlider.value = FrontHealthSlider.value;
            }
        }

        // Handle character flipping
        if (CharacterTransform != null)
        {
            float parentSign = Mathf.Sign(CharacterTransform.localScale.x);
            transform.localScale = new Vector3(_originalScale.x * parentSign, _originalScale.y, _originalScale.z);
        }
    }
}