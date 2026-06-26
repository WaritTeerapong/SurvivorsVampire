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
        _levelUpScreen.SetActive(true);
        UpdateUI();
        IntStatArray = new StatType[3] { StatType.MaxHealth, StatType.MoveSpeed, StatType.ATKDamage };
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
    }

    private Player GetLocalPlayer()
    {
        if (_localPlayer == null && NetworkManager.Singleton != null)
        {
            
            // 1. Try using Netcode SpawnManager (works on both Client and Host/Server)
            if (NetworkManager.Singleton.SpawnManager != null)
            {
                var localPlayerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                if (localPlayerObj != null)
                {
                    _localPlayer = localPlayerObj.GetComponent<Player>();
                }
            }

            // 2. Fallback to scanning registered players via IsOwner
            if (_localPlayer == null && PlayerManager.Instance != null)
            {
                foreach (Player player in PlayerManager.Instance.AllPlayers)
                {
                    if (player != null && player.IsOwner)
                    {
                        _localPlayer = player;
                        break;
                    }
                }
            }

            if (_localPlayer != null)
            {
                OwnerStat = _localPlayer.Stats;
            }
            
        }
        return _localPlayer;
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
    }

    private void CreateCards(Dictionary<string, int> itemList)
    {
        foreach (UpgradeCard card in _upgradeCard)
        {
            card.gameObject.SetActive(false);
        }
        int cardIndex = 0;

        Player deadPlayer = FindDeadPlayer();
        int respawnCardIndex = (deadPlayer != null && _upgradeCard.Length > 0)
            ? Random.Range(0, _upgradeCard.Length)
            : -1;

        foreach (KeyValuePair<string, int> kvp in itemList)
        {
            if (cardIndex >= _upgradeCard.Length)
            {
                Debug.LogWarning($"[LevelUpUI] Received more items than available cards on screen! Skipping item ID: {kvp.Key}");
                break;
            }

            if (cardIndex == respawnCardIndex && deadPlayer != null)
            {
                SetupReviveCard(_upgradeCard[cardIndex], deadPlayer);
                cardIndex++;
                continue;
            }

            SetupUpgradeCard(_upgradeCard[cardIndex], kvp.Key, kvp.Value);
            cardIndex++;
        }
    }

    private Player FindDeadPlayer()
    {
        if (PlayerManager.Instance != null)
        {
            foreach (Player otherPlayer in PlayerManager.Instance.AllPlayers)
            {
                if (otherPlayer.IsOwner) continue;
                if (otherPlayer.CurrentState is PlayerDiedState)
                {
                    return otherPlayer;
                }
            }
        }
        return null;
    }

    private void SetupReviveCard(UpgradeCard card, Player deadPlayer)
    {
        card.gameObject.SetActive(true);
        card.SetupCard(false);

        TMP_Text buttonText = card.UpgradeButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = "Revive";
        }

        card.UpgradeButton.onClick.RemoveAllListeners();
        card.UpgradeButton.onClick.AddListener(() => { OnRespawnFriendClicked(deadPlayer); });
    }

    private void SetupUpgradeCard(UpgradeCard card, string itemId, int nextLevel)
    {
        int currentLevel = nextLevel - 1;
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
            return;
        }

        card.gameObject.SetActive(true);

        string statName = "";
        float increaseAmount = 0f;
        float totalValue = 0f;
        StatType activeStatType = StatType.MaxHealth;

        ResolveItemStats(itemData, nextLevel, currentLevel, ref activeStatType, ref statName, ref increaseAmount, ref totalValue);

        bool isFloat = IntStatArray == null || !IntStatArray.Contains(activeStatType);

        if (isFloat)
        {
            card.SetupCard(itemData.ItemName, nextLevel, statName, increaseAmount, totalValue);
        }
        else
        {
            card.SetupCard(itemData.ItemName, nextLevel, statName, Mathf.RoundToInt(increaseAmount), Mathf.RoundToInt(totalValue));
        }

        card.UpgradeButton.onClick.RemoveAllListeners();
        card.UpgradeButton.onClick.AddListener(() => { OnUpgradeClicked(itemId); });
        TMP_Text buttonText = card.UpgradeButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = "Upgrade";
        }
    }

    private void ResolveItemStats(ItemData_Base itemData, int nextLevel, int currentLevel, ref StatType activeStatType, ref string statName, ref float increaseAmount, ref float totalValue)
    {
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
    }

    private void OnRespawnFriendClicked(Player playerToRevive)
    {
        if (playerToRevive != null)
        {
            playerToRevive.RevivePlayerRpc(true);
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