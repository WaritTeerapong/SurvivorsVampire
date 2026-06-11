using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;


public class LevelUpUI : NetworkBehaviour
{

    [SerializeField] private GameObject _levelUpScreen;
    [SerializeField] private UpgradeCard[] _upgradeCard;
    [SerializeField] private StatType[] IntStatArray;

    private PlayerRunTimeStats OwnerStat;

    void Start()
    {
        _levelUpScreen.SetActive(false);
        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.OnLevelUp += UpdateUI;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.OnLevelUp -= UpdateUI;
        }
    }

    private void UpdateUI()
    {

        if(OwnerStat == null)
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.LocalClient != null &&
                NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                OwnerStat = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerRunTimeStats>();
            }
        }
        if (OwnerStat == null)
        {
            Debug.LogError("[LevelUpUI] Failed to open screen: Local Client's PlayerRunTimeStats not found!");
            return;
        }

        
        CreateCards(PlayerLevelManager.Instance.RandomUpgradeStats(OwnerStat.CurrentStatsLevel.Value));
        _levelUpScreen.SetActive(true);
    }


    private void CreateCards(Dictionary<StatType,int> statList)
    {
        foreach (UpgradeCard card in _upgradeCard)
        {
            card.gameObject.SetActive(false);
        }
        int cardIndex = 0;

        foreach (KeyValuePair<StatType, int> kvp in statList)
        {
            if (cardIndex >= _upgradeCard.Length) {
                Debug.LogWarning($"[LevelUpUI] Received more stats than available cards on screen! Skipping stat: {kvp.Key}");
                break; };

            // Get Key,Value
            StatType stat = kvp.Key;
            int currentLevel = kvp.Value;
            int nextLevel = currentLevel + 1;

            // Get Data
            StatUpgrade info = PlayerLevelManager.Instance.StatUpgradeData.GetStatUpgradeInfo(stat);
            string statName = info.UpgradeName;
            
            float currentLevelBonus = info.GetBonusForLevel(currentLevel);
            float nextLevelBonus = info.GetBonusForLevel(nextLevel);
            float increaseAmount = nextLevelBonus - currentLevelBonus;

            float currentStatValue = OwnerStat.CurrentStats.Value.GetCurrentStat(stat);
            float totalValue = currentStatValue + increaseAmount;
            
            // Active Card Component
            UpgradeCard targetCard = _upgradeCard[cardIndex];
            targetCard.gameObject.SetActive(true);
            // Draw Card
            if (IntStatArray.Contains(stat))
            {
                targetCard.SetupCard(
                    statName,
                    nextLevel,
                    Mathf.RoundToInt(increaseAmount),
                    Mathf.RoundToInt(totalValue)
                );
            }
            else
            {
                targetCard.SetupCard(
                    statName,
                    nextLevel,
                    increaseAmount,
                    totalValue
                );
            }
            targetCard.UpgradeButton.onClick.RemoveAllListeners();
            targetCard.UpgradeButton.onClick.AddListener(() => { OnUpgradeClicked(stat); });
            
            cardIndex++;
        } 
    }

    private void OnUpgradeClicked(StatType chosenStat)
    {
        OwnerStat.RequestUpgradeServerRpc(chosenStat);
        foreach (var card in _upgradeCard)
        {
            card.UpgradeButton.onClick.RemoveAllListeners();
        }
        _levelUpScreen.SetActive(false);
    }

}
