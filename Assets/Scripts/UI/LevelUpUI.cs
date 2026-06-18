using System.Collections;
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

    private int _pendingLevelUps = 0;
    private bool _isChoosing = false;

    void Start()
    {
        _levelUpScreen.SetActive(false);
        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.OnLevelUp += UpdateUI;
        }
        IntStatArray = new StatType[3] { StatType.MaxHealth,StatType.MoveSpeed,StatType.ATKDamage};
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
        _pendingLevelUps++;

        if (PauseMenuUI.Instance != null)
        {
            PauseMenuUI.Instance.ForceCloseMenu();
            PauseMenuUI.Instance.IsLevelUpActive = true;
        }

        if (!_isChoosing)
        {
            StartChoosing();
        }
    }

    private void StartChoosing()
    {
        _isChoosing = true;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            PauseManager.Instance.ToggleLevelUpPauseServerRpc(NetworkManager.Singleton.LocalClientId, true);
        }

        ShowNextCards();
    }

    private void ShowNextCards()
    {
        if (OwnerStat == null)
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

        CreateCards(PlayerLevelManager.Instance.RandomUpgradeItem(3));
        _levelUpScreen.SetActive(true);
    }

    private void CreateCards(Dictionary<string, int> itemList)
    {
        foreach (UpgradeCard card in _upgradeCard)
        {
            card.gameObject.SetActive(false);
        }
        int cardIndex = 0;

        // if( otherPlayer.isDown){
        //      randomCardIndex = Random.Range(0,_upgradeCard.Length)
        // 
        //  }

        foreach (KeyValuePair<string, int> kvp in itemList)
        {
            if (cardIndex >= _upgradeCard.Length)
            {
                Debug.LogWarning($"[LevelUpUI] Received more items than available cards on screen! Skipping item ID: {kvp.Key}");
                break;
            }

            // if (otherPlayer.isDown)
            //  Draw a card
            //      cardIndex++;
            //}

            string itemId = kvp.Key;
            int nextLevel = kvp.Value;
            int currentLevel = nextLevel - 1;

            // Retrieve the item definition from databases
            ItemData_Base itemData = (ItemData_Base)PlayerLevelManager.Instance.WeaponDatabase?.GetItemByID(itemId) ?? 
                                     (ItemData_Base)PlayerLevelManager.Instance.PassiveItemDatabase?.GetItemByID(itemId);

            if (itemData == null)
            {
                Debug.LogWarning($"[LevelUpUI] Item with ID {itemId} not found in databases!");
                continue;
            }

            string itemName = itemData.ItemName;
            string statName = "";
            float increaseAmount = 0f;
            float totalValue = 0f;
            StatType activeStatType = StatType.MaxHealth;

            if (itemData is PassiveItemData_SO passiveItem)
            {
                BaseStat nextBonus = passiveItem.GetBonusForLevel(nextLevel);
                BaseStat currentBonus = currentLevel > 0 ? passiveItem.GetBonusForLevel(currentLevel) : new BaseStat();

                if (nextBonus.MaxHealth != currentBonus.MaxHealth)
                {
                    activeStatType = StatType.MaxHealth;
                    statName = "Health";
                    increaseAmount = nextBonus.MaxHealth - currentBonus.MaxHealth;
                    totalValue = OwnerStat.CurrentStats.Value.MaxHealth + increaseAmount;
                }
                else if (nextBonus.MoveSpeed != currentBonus.MoveSpeed)
                {
                    activeStatType = StatType.MoveSpeed;
                    statName = "Speed";
                    increaseAmount = nextBonus.MoveSpeed - currentBonus.MoveSpeed;
                    totalValue = OwnerStat.CurrentStats.Value.MoveSpeed + increaseAmount;
                }
                else if (nextBonus.ATKDamage != currentBonus.ATKDamage)
                {
                    activeStatType = StatType.ATKDamage;
                    statName = "ATK Damage";
                    increaseAmount = nextBonus.ATKDamage - currentBonus.ATKDamage;
                    totalValue = OwnerStat.CurrentStats.Value.ATKDamage + increaseAmount;
                }
                else if (nextBonus.ATKSpeed != currentBonus.ATKSpeed)
                {
                    activeStatType = StatType.ATKSpeed;
                    statName = "ATK Speed";
                    increaseAmount = nextBonus.ATKSpeed - currentBonus.ATKSpeed;
                    totalValue = OwnerStat.CurrentStats.Value.ATKSpeed + increaseAmount;
                }
                else if (nextBonus.ATKRange != currentBonus.ATKRange)
                {
                    activeStatType = StatType.ATKRange;
                    statName = "ATK Range";
                    increaseAmount = nextBonus.ATKRange - currentBonus.ATKRange;
                    totalValue = OwnerStat.CurrentStats.Value.ATKRange + increaseAmount;
                }
            }
            else if (itemData is WeaponItemData_SO weaponItem)
            {
                WeaponStat nextBonus = weaponItem.GetBonusForLevel(nextLevel);
                WeaponStat currentBonus = currentLevel > 0 ? weaponItem.GetBonusForLevel(currentLevel) : new WeaponStat();

                if (nextBonus.ATKDamage != currentBonus.ATKDamage)
                {
                    activeStatType = StatType.ATKDamage;
                    statName = "Damage";
                    increaseAmount = nextBonus.ATKDamage - currentBonus.ATKDamage;
                    totalValue = OwnerStat.CurrentStats.Value.ATKDamage + nextBonus.ATKDamage;
                }
                else if (nextBonus.ATKRange != currentBonus.ATKRange)
                {
                    activeStatType = StatType.ATKRange;
                    statName = "Range";
                    increaseAmount = nextBonus.ATKRange - currentBonus.ATKRange;
                    totalValue = OwnerStat.CurrentStats.Value.ATKRange + nextBonus.ATKRange;
                }
                else if (nextBonus.ATKSpeed != currentBonus.ATKSpeed)
                {
                    activeStatType = StatType.ATKSpeed;
                    statName = "Attack Speed";
                    increaseAmount = nextBonus.ATKSpeed - currentBonus.ATKSpeed;
                    totalValue = OwnerStat.CurrentStats.Value.ATKSpeed + nextBonus.ATKSpeed;
                }
            }

            bool isFloat = true;
            if (IntStatArray != null)
            {
                isFloat = !IntStatArray.Contains(activeStatType);
            }

            UpgradeCard targetCard = _upgradeCard[cardIndex];
            targetCard.gameObject.SetActive(true);

            if (isFloat)
            {
                targetCard.SetupCard(
                    itemName,
                    nextLevel,
                    statName,
                    increaseAmount,
                    totalValue
                );
            }
            else
            {
                targetCard.SetupCard(
                    itemName,
                    nextLevel,
                    statName,
                    Mathf.RoundToInt(increaseAmount),
                    Mathf.RoundToInt(totalValue)
                );
            }

            targetCard.UpgradeButton.onClick.RemoveAllListeners();
            targetCard.UpgradeButton.onClick.AddListener(() => { OnUpgradeClicked(itemId); });

            cardIndex++;
        }
    }

    private void OnReviveClick() { return; }
    private void OnUpgradeClicked(string itemId)
    {
        PlayerInventoryManager inventory = OwnerStat.GetComponent<PlayerInventoryManager>();
        if (inventory != null)
        {
            if (PlayerLevelManager.Instance.WeaponDatabase?.GetItemByID(itemId) != null)
            {
                inventory.AddOrUpgradeWeaponServerRpc(itemId);
            }
            else if (PlayerLevelManager.Instance.PassiveItemDatabase?.GetItemByID(itemId) != null)
            {
                inventory.AddOrUpgradePassiveServerRpc(itemId);
            }
        }

        foreach (var card in _upgradeCard) card.UpgradeButton.onClick.RemoveAllListeners();

        _pendingLevelUps--;

        if (_pendingLevelUps > 0)
        {
            StartCoroutine(WaitServerSyncAndShowNextCard());
        }
        else
        {
            _isChoosing = false;
            _levelUpScreen.SetActive(false);

            if (PauseMenuUI.Instance != null) PauseMenuUI.Instance.IsLevelUpActive = false;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                PauseManager.Instance.ToggleLevelUpPauseServerRpc(NetworkManager.Singleton.LocalClientId, false);
            }

            if (PauseManager.Instance.IsGamePaused.Value && PauseMenuUI.Instance != null)
            {
                PauseMenuUI.Instance.ResumeGame();
            }
        }
    }

    private IEnumerator WaitServerSyncAndShowNextCard()
    {
        foreach (var card in _upgradeCard) card.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(0.2f);

        ShowNextCards();
    }

}
