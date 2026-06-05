using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class XPBarUI : NetworkBehaviour
{
    [SerializeField] private GameObject _xpBar;

    [Header("Experience")]
    [SerializeField] AnimationCurve experienceCurve;

    public PlayerRunTimeStats CurrentStatsScript;
    private int _currentLevel, _currentXP, _XPneeded;

    [Header("Interface")]
    [SerializeField] TextMeshProUGUI LevelText;
    [SerializeField] TextMeshProUGUI ExperienceText;
    [SerializeField] Image ExperienceFill;

    private void Awake()
    {
        gameObject.TryGetComponent<PlayerRunTimeStats>(out PlayerRunTimeStats CurrentStatsScript);
    }
    private void Start()
    {

        if (CurrentStatsScript != null)
        {
            CurrentStatsScript.OnStatChanged += OnPlayerStatsChanged;
            PlayerStats current = CurrentStatsScript.CurrentStats.Value;
            UpdateXPBarUI(current);
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnected;
        }

        _xpBar.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
        }
    }
    void OnConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            _xpBar.gameObject.SetActive(true);
        }
    }

    void OnDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            _xpBar.gameObject.SetActive(false);

        }
    }

    private void OnDisable()
    {
        if (CurrentStatsScript != null)
        {
            CurrentStatsScript.OnStatChanged -= OnPlayerStatsChanged;
        }
    }

    private void OnPlayerStatsChanged(PlayerStats stats)
    {
        UpdateXPBarUI(stats);
    }

    public void UpdateXPBarUI(PlayerStats stats)
    { 
        _currentLevel = stats.Level;
        _currentXP = stats.XP;
        _XPneeded = stats.XPNeeded;

        LevelText.text = _currentLevel.ToString();

        if (_XPneeded == -1)
        {
            ExperienceText.text = "MAX LEVEL";
            ExperienceFill.fillAmount = 1f;
        }
        else if (_XPneeded > 0)
        {
            ExperienceText.text = $"{_currentXP} exp / {_XPneeded} exp";
            ExperienceFill.fillAmount = (float)_currentXP / (float)_XPneeded;
        }
        else
        {
            ExperienceText.text = "0 exp";
            ExperienceFill.fillAmount = 0f;
        }
    }
}
