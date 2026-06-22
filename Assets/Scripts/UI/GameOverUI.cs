using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private Button _reToLobbyButton;
    [SerializeField] private TMP_Text _reToLobbyText;

    void Start()
    {
        if (_reToLobbyButton == null) _reToLobbyButton = GetComponentInChildren<Button>();
        if (_reToLobbyText == null) _reToLobbyText = _reToLobbyButton.GetComponentInChildren<TMP_Text>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartReTimer();
            GameManager.Instance.ReturnToLobbyTimer.OnValueChanged += UpdateUI;
            UpdateUI(0f, GameManager.Instance.ReturnToLobbyTimer.Value);
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.ReturnToLobbyTimer.OnValueChanged -= UpdateUI;
    }

    private void UpdateUI(float prevValue, float newValue)
    {
        if (newValue > 0)
        {
            _reToLobbyText.text = $"Return to Lobby ({Mathf.CeilToInt(newValue)}s)";
        }
        else
        {
            _reToLobbyText.text = "Going to Lobby...";
        }
    }


}
