using TMPro;
using UnityEngine;

public class WaveUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _waveText;
    [SerializeField] private TMP_Text _timerText;

    private void Start()
    {
        if (EnemySpawnManager.Instance != null)
        {
            EnemySpawnManager.Instance.CurrentWave.OnValueChanged += OnWaveChanged;
            EnemySpawnManager.Instance.IsResting.OnValueChanged += OnRestingChanged;
            EnemySpawnManager.Instance.TimeRemaining.OnValueChanged += OnTimeChanged;

            UpdateWaveText(EnemySpawnManager.Instance.CurrentWave.Value, EnemySpawnManager.Instance.IsResting.Value);
            UpdateTimeText(EnemySpawnManager.Instance.TimeRemaining.Value);
        }
    }

    private void OnDestroy()
    {
        if (EnemySpawnManager.Instance != null)
        {
            EnemySpawnManager.Instance.CurrentWave.OnValueChanged -= OnWaveChanged;
            EnemySpawnManager.Instance.IsResting.OnValueChanged -= OnRestingChanged;
            EnemySpawnManager.Instance.TimeRemaining.OnValueChanged -= OnTimeChanged;
        }
    }

    private void OnWaveChanged(int previousValue, int newValue)
    {
        UpdateWaveText(newValue, EnemySpawnManager.Instance.IsResting.Value);
    }

    private void OnRestingChanged(bool previousValue, bool newValue)
    {
        UpdateWaveText(EnemySpawnManager.Instance.CurrentWave.Value, newValue);
    }

    private void OnTimeChanged(int previousValue, int newValue)
    {
        UpdateTimeText(newValue);
    }

    private void UpdateWaveText(int wave, bool isResting)
    {
        if (isResting)
        {
            _waveText.text = "RESTING...";
            _waveText.color = Color.green;
        }
        else
        {
            _waveText.text = $"WAVE {wave}";
            _waveText.color = Color.white;
        }
    }

    private void UpdateTimeText(int time)
    {
        if (_timerText == null) return;

        // Process flag sent from EnemySpawnManager
        if (time < 0)
        {
            _timerText.text = "KILL BOSS!";
            _timerText.color = Color.red;
        }
        else
        {
            _timerText.text = time.ToString() + " s";
            _timerText.color = Color.white;
        }
    }
}