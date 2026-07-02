using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("=== Visibility ===")]
    [SerializeField] private GameObject _uiContainer;

    [Header("=== Sliders ===")]
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private Slider _easeSlider;
    [SerializeField] private float _lerpSpeed = 5f;

    [Header("=== Texts ===")]
    [SerializeField] private TMP_Text _bossNameText;
    [SerializeField] private TMP_Text _hpText;

    private Boss _currentBoss;

    void Awake()
    {
        if (_uiContainer != null) _uiContainer.SetActive(false);
    }

    void OnEnable()
    {
        Boss.OnBossSpawnedGlobal += HandleBossSpawned;
        Boss.OnBossDespawnedGlobal += HandleBossDespawned;
    }

    void OnDisable()
    {
        Boss.OnBossSpawnedGlobal -= HandleBossSpawned;
        Boss.OnBossDespawnedGlobal -= HandleBossDespawned;

        if (_currentBoss != null)
        {
            _currentBoss.CurrentHealth.OnValueChanged -= HandleHealthChanged;
        }
    }

    void Update()
    {
        if (_uiContainer != null && _uiContainer.activeInHierarchy && _hpSlider != null && _easeSlider != null)
        {
            if (_easeSlider.value != _hpSlider.value)
            {
                _easeSlider.value = Mathf.Lerp(_easeSlider.value, _hpSlider.value, Time.deltaTime * _lerpSpeed);
            }
        }
    }

    private void HandleHealthChanged(int previousValue, int newValue)
    {
        UpdateHealthUI(previousValue, newValue);
    }

    private void UpdateHealthUI(int prev, int current)
    {
        if (_hpSlider != null) _hpSlider.value = current;

        if (_hpText != null && _currentBoss != null && _currentBoss.BossData != null)
        {
            _hpText.text = $"{current} / {_currentBoss.BossData.MaxHealth}";
        }
    }

    private void HandleBossSpawned(Boss boss)
    {
        _currentBoss = boss;

        if (_currentBoss.BossData != null)
        {
            int maxHealth = _currentBoss.BossData.MaxHealth;

            if (_hpSlider != null) _hpSlider.maxValue = maxHealth;
            if (_easeSlider != null) _easeSlider.maxValue = maxHealth;

            if (_bossNameText != null) _bossNameText.text = _currentBoss.BossData.BossName;
        }

        UpdateHealthUI(0, _currentBoss.CurrentHealth.Value);

        if (_easeSlider != null) _easeSlider.value = _currentBoss.CurrentHealth.Value;

        _currentBoss.CurrentHealth.OnValueChanged += HandleHealthChanged;

        if (_uiContainer != null) _uiContainer.SetActive(true);
    }

    private void HandleBossDespawned(Boss boss)
    {
        if (_currentBoss == null)
        {
            _currentBoss.CurrentHealth.OnValueChanged -= HandleHealthChanged;
            _currentBoss = null;

            if (_uiContainer != null) _uiContainer.SetActive(false);
        }
    }
}