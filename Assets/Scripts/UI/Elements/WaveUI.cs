using TMPro;
using UnityEngine;

public class WaveUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _waveText;
    [SerializeField] private TMP_Text _timerText;

    private void Start()
    {
        // 1. เช็กว่ามี Manager อยู่ในฉากหรือไม่
        if (EnemySpawnManager.Instance != null)
        {
            // 2. สมัครรับ Event (Subscribe) เมื่อค่าใน NetworkVariable มีการเปลี่ยนแปลง
            EnemySpawnManager.Instance.CurrentWave.OnValueChanged += OnWaveChanged;
            EnemySpawnManager.Instance.IsResting.OnValueChanged += OnRestingChanged;
            EnemySpawnManager.Instance.TimeRemaining.OnValueChanged += OnTimeChanged;

            // 3. อัปเดต UI ครั้งแรกสุด (เพื่อไม่ให้เป็นข้อความเปล่าๆ ตอนเริ่มเกม)
            UpdateWaveText(EnemySpawnManager.Instance.CurrentWave.Value, EnemySpawnManager.Instance.IsResting.Value);
            UpdateTimeText(EnemySpawnManager.Instance.TimeRemaining.Value);
        }
    }

    private void OnDestroy()
    {
        // 🚨 กฎเหล็กของ Event: สมัครแล้วต้องยกเลิก (Unsubscribe) ตอนถูกทำลาย เพื่อป้องกัน Memory Leak!
        if (EnemySpawnManager.Instance != null)
        {
            EnemySpawnManager.Instance.CurrentWave.OnValueChanged -= OnWaveChanged;
            EnemySpawnManager.Instance.IsResting.OnValueChanged -= OnRestingChanged;
            EnemySpawnManager.Instance.TimeRemaining.OnValueChanged -= OnTimeChanged;
        }
    }

    // --- ฟังก์ชันที่จะทำงานก็ต่อเมื่อ "Server สั่งเปลี่ยนค่า" เท่านั้น ---

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

    // --- ฟังก์ชันจัดการแสดงผล UI ---

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

        _timerText.text = time.ToString() + " s";
    }
}