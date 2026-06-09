using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;


public class LevelUpUI : NetworkBehaviour
{

    [SerializeField] private GameObject _levelUpScreen;
    [SerializeField] private UpgradeCard[] _upgradeCard;

    // player stat level
    // public StatBonusData_SO StatBonusData;


    void Start()
    {
        foreach (var _card in _upgradeCard)
        {
            _card.UpgradeButton.onClick.AddListener(OnUpgradeClicked);
        }
        _levelUpScreen.SetActive(false);

        PlayerLevelManager.Instance.OnLevelUp += UpdateUI;
    }

    private void UpdateUI()
    {
        Time.timeScale = 0;
        _levelUpScreen.SetActive(true);
        

    }

    private void OnDestroy()
    {
        foreach (var _card in _upgradeCard)
        {
            _card.UpgradeButton.onClick.RemoveListener(OnUpgradeClicked);
        }
    }

    private void OnUpgradeClicked()
    {
        // upgrade choose Stat
        _levelUpScreen.SetActive(false);
        Time.timeScale = 1;
    }

}
