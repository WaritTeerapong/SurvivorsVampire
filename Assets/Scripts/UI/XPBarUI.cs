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

    void Start()
    {
        _xpBar.SetActive(false);

        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.OnLevelUp += UpdateUI;
            PlayerLevelManager.Instance.OnGainXP += UpdateUI;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnected;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.OnLevelUp -= UpdateUI;
            PlayerLevelManager.Instance.OnGainXP -= UpdateUI;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
        }
    }

    void UpdateUI()
    {
        if (PlayerLevelManager.Instance == null) return;

        int currentXP = PlayerLevelManager.Instance.SharedXP.Value;
        int xpNeeded = PlayerLevelManager.Instance.SharedXPNeeded.Value;

        string xp = $"{currentXP} / {xpNeeded}";

        _levelText.text = PlayerLevelManager.Instance.SharedLevel.Value.ToString();

        if (xpNeeded == -1)
        {
            _xpText.text = "MAX";

            _xpSlider.maxValue = 1;
            _xpSlider.value = 1;
        }
        else
        {
            _xpText.text = xp;
            _xpSlider.maxValue = xpNeeded;
            _xpSlider.value = currentXP;
        }
    }

    void OnConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            _xpBar.SetActive(true);
        }
    }

    void OnDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            _xpBar.SetActive(false);
        }
    }


}
