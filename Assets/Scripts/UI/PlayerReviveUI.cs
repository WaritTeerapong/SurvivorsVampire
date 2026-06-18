using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerReviveUI : MonoBehaviour
{
    [Header("=== References ===")]
    public Player PlayerTarget;
    public Transform CharacterTransform;

    [Header("=== UI Panels ===")]
    public GameObject DieUIPanel;
    public GameObject ReviveUIPanel;

    [Header("=== Die UI Elements ===")]
    public Slider DieSlider;
    public TMP_Text DieText;

    [Header("=== Revive UI Elements ===")]
    public Slider ReviveSlider;
    public TMP_Text ReviveText;

    private Vector3 _originalScale;
    private float _maxReviveTime = 3f;

    void Start()
    {
        if (PlayerTarget == null) PlayerTarget = GetComponentInParent<Player>();
        if (CharacterTransform == null) CharacterTransform = PlayerTarget.transform;

        _originalScale = transform.localScale;

        if (DieSlider != null) DieSlider.maxValue = 10f;
        if (ReviveSlider != null) ReviveSlider.maxValue = _maxReviveTime;
    }

    void Update()
    {
        if (PlayerTarget == null) return;

        // Handle character flipping
        if (CharacterTransform != null)
        {
            float parentSign = Mathf.Sign(CharacterTransform.localScale.x);
            transform.localScale = new Vector3(_originalScale.x * parentSign, _originalScale.y, _originalScale.z);
        }

        // Hide UI if player is alive
        if (!PlayerTarget.IsDownOrDied)
        {
            if (DieUIPanel.activeSelf) DieUIPanel.SetActive(false);
            if (ReviveUIPanel.activeSelf) ReviveUIPanel.SetActive(false);
            return;
        }

        // Handle UI toggling based on revive status
        if (PlayerTarget.IsBeingRevived)
        {
            if (DieUIPanel.activeSelf) DieUIPanel.SetActive(false);
            if (!ReviveUIPanel.activeSelf) ReviveUIPanel.SetActive(true);

            // Invert the value so the revive bar fills up (0 to 3)
            if (ReviveSlider != null) ReviveSlider.value = _maxReviveTime - PlayerTarget.ReviveTimer.Value;
            if (ReviveText != null) ReviveText.text = $"{PlayerTarget.ReviveTimer.Value:F1}s";
        }
        else
        {
            if (!DieUIPanel.activeSelf) DieUIPanel.SetActive(true);
            if (ReviveUIPanel.activeSelf) ReviveUIPanel.SetActive(false);

            // Deplete the die bar (10 to 0)
            if (DieSlider != null) DieSlider.value = PlayerTarget.DiedTimer.Value;
            if (DieText != null) DieText.text = $"{PlayerTarget.DiedTimer.Value:F1}s";
        }
    }
}