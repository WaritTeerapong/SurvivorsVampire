using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LevelUpUI : NetworkBehaviour
{

    [SerializeField] private GameObject _levelUpScreen;
    [SerializeField] private UpgradeCard[] _upgradeCard;
    [SerializeField] private StatType[] IntStatArray;

    private PlayerRunTimeStats OwnerStat;
    private Player _localPlayer;

    private int _pendingLevelUps = 0;
    private bool _isChoosing = false;

    void Start()
    {
        _levelUpScreen.SetActive(false);
        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.SharedLevel.OnValueChanged += OnLevelChange;
        }
        IntStatArray = new StatType[3] { StatType.MaxHealth, StatType.MoveSpeed, StatType.ATKDamage };
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.SharedLevel.OnValueChanged -= OnLevelChange;
        }
        if (_localPlayer != null)
        {
            _localPlayer.OnStateChanged -= OnLocalPlayerStateChanged;
        }
    }

    private Player GetLocalPlayer()
    {
        if (_localPlayer == null)
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.LocalClient != null &&
                NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                _localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Player>();
                if (_localPlayer != null)
                {
                    OwnerStat = _localPlayer.Stats;
                    _localPlayer.OnStateChanged += OnLocalPlayerStateChanged;
                }
            }
        }
        return _localPlayer;
    }

    private void OnLocalPlayerStateChanged(IPlayerState newState)
    {
        if (!(newState is PlayerDiedState) && _pendingLevelUps > 0 && !_isChoosing)
        {
            OpenLevelUpScreen();
        }
    }

    private void OnLevelChange(int previousValue, int newValue)
    {
        UpdateUI();
    }
    private void UpdateUI()
    {
        _pendingLevelUps++;

        Player player = GetLocalPlayer();
        if (player != null && player.CurrentState is PlayerDiedState)
        {
            return;
        }

        OpenLevelUpScreen();
    }

    private void OpenLevelUpScreen()
    {
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
        GetLocalPlayer();
        if (OwnerStat == null)
        {
            Debug.LogError("[LevelUpUI] Failed to open screen: Local Client's PlayerRunTimeStats not found!");
            return;
        }

        PlayerUpgradePool upgradePool = OwnerStat.GetComponent<PlayerUpgradePool>();
        if (upgradePool != null)
        {
            CreateCards(upgradePool.RandomUpgradeItem(3));
        }
        else
        {
            Debug.LogError("[LevelUpUI] PlayerUpgradePool not found on local player!");
        }
        _levelUpScreen.SetActive(true);
    }

    private void CreateCards(Dictionary<string, int> itemList)
    {
        foreach (UpgradeCard card in _upgradeCard)
        {
            card.gameObject.SetActive(false);
        }
        int cardIndex = 0;

        // Check if Teammate Died
        Player deadPlayer = null;
        if (PlayerManager.Instance != null)
        {
            foreach (Player otherPlayer in PlayerManager.Instance.AllPlayers)
            {
                if (otherPlayer.IsOwner) continue;
                if (otherPlayer.CurrentState is PlayerDiedState)
                {
                    deadPlayer = otherPlayer;
                    break;
                }
            }
        }

        // Random revive card index
        int respawnCardIndex = -1;
        if (deadPlayer != null && _upgradeCard.Length > 0)
        {
            respawnCardIndex = Random.Range(0, _upgradeCard.Length);
        }

        foreach (KeyValuePair<string, int> kvp in itemList)
        {
            if (cardIndex >= _upgradeCard.Length)
            {
                Debug.LogWarning($"[LevelUpUI] Received more items than available cards on screen! Skipping item ID: {kvp.Key}");
                break;
            }

            // if there is deadPlayer
            if (cardIndex == respawnCardIndex && deadPlayer != null)
            {
                UpgradeCard respawnCard = _upgradeCard[cardIndex];
                respawnCard.gameObject.SetActive(true);
                respawnCard.SetupCard(false);
                
                Player targetPlayer = deadPlayer;
                TMP_Text Buttontext = respawnCard.UpgradeButton.GetComponentInChildren<TMP_Text>();
                if (Buttontext != null)
                {
                    Buttontext.text = "Revive";
                }

                respawnCard.UpgradeButton.onClick.RemoveAllListeners();
                respawnCard.UpgradeButton.onClick.AddListener(() => { OnReviveClicked(targetPlayer); });
                
                cardIndex++;
                continue;
            }

            string itemId = kvp.Key;
            int nextLevel = kvp.Value;
            int currentLevel = nextLevel - 1;

            // Retrieve the item definition from databases on the inventory
            PlayerInventory inventory = OwnerStat.GetComponent<PlayerInventory>();
            ItemData_Base itemData = null;
            if (inventory != null)
            {
                itemData = (ItemData_Base)inventory.WeaponDatabase?.GetItemByID(itemId) ??
                           inventory.PassiveDatabase?.GetItemByID(itemId);
            }

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
            TMP_Text buttonText = targetCard.UpgradeButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = "Upgrade";
            }

            cardIndex++;
        }
    }

    private void OnReviveClicked(Player playerToRevive)
    {
        if (playerToRevive != null)
        {
            playerToRevive.RevivePlayerServerRpc();
        }

        FinishChoosing();
    }
    private void OnUpgradeClicked(string itemId)
    {
        PlayerInventory inventory = OwnerStat.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            if (inventory.WeaponDatabase?.GetItemByID(itemId) != null)
            {
                inventory.AddOrUpgradeWeaponRpc(itemId);
            }
            else if (inventory.PassiveDatabase?.GetItemByID(itemId) != null)
            {
                inventory.AddOrUpgradePassiveRpc(itemId);
            }
        }

        FinishChoosing();
    }

    private void FinishChoosing()
    {
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