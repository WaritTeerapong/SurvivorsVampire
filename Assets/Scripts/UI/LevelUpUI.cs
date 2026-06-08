using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;


public class LevelUpUI : NetworkBehaviour
{

    [SerializeField] private GameObject _levelUpScreen;
    [SerializeField] private Button[] _upgradeBtns = new Button[3];


    void Start()
    {
        foreach (var _button in _upgradeBtns)
        {
            _button.onClick.AddListener(OnUpgradeClicked);
        }
        _levelUpScreen.SetActive(false);

        PlayerLevelManager.Instance.OnLevelUp += UpdateUI;
    }

    private void UpdateUI()
    {
        _levelUpScreen.SetActive(true);

        // Random stat on to the botton
        

    }

    private void OnDestroy()
    {
        foreach (var _button in _upgradeBtns)
        {
            _button.onClick.RemoveListener(OnUpgradeClicked);
        }
    }



    private void OnUpgradeClicked()
    {
        // upgrade choose Stat
        _levelUpScreen.SetActive(false);
    }
}
