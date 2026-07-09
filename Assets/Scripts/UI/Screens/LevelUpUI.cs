using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LevelUpUI : NetworkBehaviour
{
    [Header("=== UI Elements ===")]
    [SerializeField] private GameObject _levelUpScreen;
    [SerializeField] private GameObject _waitingOverlay;
    [SerializeField] private TMP_Text _pendingCountText;
    [SerializeField] private UpgradeCard[] _upgradeCard;

    [Header("=== Animation Settings ===")]
    [SerializeField] private float _fadeDuration = 0.35f;
    [SerializeField] private float _cardAnimDuration = 0.3f;
    [SerializeField] private float _cardStaggerDelay = 0.1f;

    [Space]
    [SerializeField] private StatType[] IntStatArray;

    private PlayerRunTimeStats OwnerStat;
    private Player _localPlayer;

    private bool _isChoosing = false;
    private bool _wasDead = false;
    private PopupUI popupUI;

    // Progression Tracker
    private int _currentUpgradeIndex = 1;
    private int _totalPendingInSession = 1;
    private Vector3 _originalPendingTextScale = Vector3.one;
    private Color _originalPendingTextColor = Color.white;

    void Awake()
    {
        if (popupUI == null && _levelUpScreen != null)
        {
            popupUI = _levelUpScreen.GetComponent<PopupUI>();
        }

        if (_pendingCountText != null)
        {
            _originalPendingTextScale = _pendingCountText.transform.localScale;
            _originalPendingTextColor = _pendingCountText.color;
            _pendingCountText.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        UpdateUI();
        IntStatArray = new StatType[3] { StatType.MaxHealth, StatType.MoveSpeed, StatType.ATKDamage };

        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.OnPendingUpgradesAdded += HandlePendingUpgradesAdded;
        }
    }

    public override void OnDestroy()
    {
        foreach (var card in _upgradeCard)
        {
            if (card != null) card.transform.DOKill();
        }

        if (_pendingCountText != null)
        {
            _pendingCountText.transform.DOKill();
            _pendingCountText.DOKill();
        }

        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.OnPendingUpgradesAdded -= HandlePendingUpgradesAdded;
        }

        base.OnDestroy();
    }

    private void Update()
    {
        if (_localPlayer != null && _isChoosing)
        {
            bool isDead = _localPlayer.CurrentState is PlayerDiedState;

            if (_wasDead && !isDead)
            {
                _wasDead = false;
                HandleRevivedDuringLevelUp();
            }
            else if (!_wasDead && isDead)
            {
                _wasDead = true;
            }
        }
    }

    private void HandleRevivedDuringLevelUp()
    {
        if (_waitingOverlay != null)
        {
            CanvasGroup canvasGroup = _waitingOverlay.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.DOFade(0f, _fadeDuration).SetUpdate(true).OnComplete(() =>
                {
                    _waitingOverlay.SetActive(false);
                    canvasGroup.alpha = 1f;
                    ProceedToShowCards();
                });
            }
            else
            {
                _waitingOverlay.SetActive(false);
                ProceedToShowCards();
            }
        }
        else
        {
            ProceedToShowCards();
        }
    }

    private void ProceedToShowCards()
    {
        if (_levelUpScreen != null) _levelUpScreen.SetActive(true);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            PauseManager.Instance.ToggleLevelUpPauseServerRpc(NetworkManager.Singleton.LocalClientId, true);
        }

        _currentUpgradeIndex = 1;
        _totalPendingInSession = Mathf.Max(1, PlayerLevelManager.Instance != null ? PlayerLevelManager.Instance.LocalPendingUpgrades : 1);
        UpdatePendingCountText(false);

        ShowNextCards();
    }

    private Player GetLocalPlayer()
    {
        if (_localPlayer == null && NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.SpawnManager != null)
            {
                var localPlayerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                if (localPlayerObj != null)
                {
                    _localPlayer = localPlayerObj.GetComponent<Player>();
                }
            }

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
        Player player = GetLocalPlayer();

        bool isDead = (player != null && player.CurrentState is PlayerDiedState);
        _wasDead = isDead;

        OpenLevelUpScreen(isDead);
    }

    private void OpenLevelUpScreen(bool isDead)
    {
        if (PauseMenuUI.Instance != null)
        {
            PauseMenuUI.Instance.ForceCloseMenu();
            PauseMenuUI.Instance.IsLevelUpActive = true;
        }

        if (isDead)
        {
            if (_levelUpScreen != null) _levelUpScreen.SetActive(false);
            if (_waitingOverlay != null)
            {
                _waitingOverlay.SetActive(true);
                CanvasGroup canvasGroup = _waitingOverlay.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.DOKill();
                    canvasGroup.alpha = 1f;
                }
            }
            _isChoosing = true;
        }
        else
        {
            if (_waitingOverlay != null) _waitingOverlay.SetActive(false);
            if (_levelUpScreen != null) _levelUpScreen.SetActive(true);

            if (!_isChoosing)
            {
                StartChoosing();
            }
        }
    }

    private void StartChoosing()
    {
        _isChoosing = true;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            PauseManager.Instance.ToggleLevelUpPauseServerRpc(NetworkManager.Singleton.LocalClientId, true);
        }

        _currentUpgradeIndex = 1;
        _totalPendingInSession = Mathf.Max(1, PlayerLevelManager.Instance != null ? PlayerLevelManager.Instance.LocalPendingUpgrades : 1);
        UpdatePendingCountText(false);

        ShowNextCards();
    }

    private void HandlePendingUpgradesAdded(int amount)
    {
        if (!_isChoosing || _pendingCountText == null) return;

        _totalPendingInSession += amount;
        UpdatePendingCountText(true);
    }

    private void UpdatePendingCountText(bool animate)
    {
        if (_pendingCountText == null) return;

        _pendingCountText.gameObject.SetActive(true);
        _pendingCountText.text = $"Upgrade: {_currentUpgradeIndex} / {_totalPendingInSession}";

        if (animate)
        {
            _pendingCountText.transform.DOKill();
            _pendingCountText.DOKill();

            _pendingCountText.transform.localScale = _originalPendingTextScale;
            _pendingCountText.color = _originalPendingTextColor;

            _pendingCountText.transform.DOPunchScale(Vector3.one * 0.2f, 0.35f, 5, 1).SetUpdate(true);
            _pendingCountText.DOColor(Color.yellow, 0.15f).SetLoops(2, LoopType.Yoyo).SetUpdate(true);
        }
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
    }

    private void CreateCards(Dictionary<string, int> itemList)
    {
        foreach (UpgradeCard card in _upgradeCard)
        {
            card.transform.DOKill();
            card.gameObject.SetActive(false);
        }

        int cardIndex = 0;
        Player deadPlayer = FindDeadPlayer();
        int respawnCardIndex = (deadPlayer != null && _upgradeCard.Length > 0)
            ? Random.Range(0, _upgradeCard.Length)
            : -1;

        foreach (KeyValuePair<string, int> kvp in itemList)
        {
            if (cardIndex >= _upgradeCard.Length) break;

            if (cardIndex == respawnCardIndex && deadPlayer != null)
            {
                SetupRespawnCard(_upgradeCard[cardIndex], deadPlayer);
                cardIndex++;
                continue;
            }

            SetupUpgradeCard(_upgradeCard[cardIndex], kvp.Key, kvp.Value);
            cardIndex++;
        }

        Sequence inSeq = DOTween.Sequence().SetUpdate(true);
        int activeIndex = 0;

        foreach (UpgradeCard card in _upgradeCard)
        {
            if (card.gameObject.activeSelf)
            {
                card.transform.localScale = Vector3.zero;
                inSeq.Insert(activeIndex * _cardStaggerDelay, card.transform.DOScale(Vector3.one, _cardAnimDuration).SetEase(Ease.OutBack));
                activeIndex++;
            }
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

    private void SetupRespawnCard(UpgradeCard card, Player deadPlayer)
    {
        card.gameObject.SetActive(true);
        card.UpgradeButton.interactable = true;
        card.SetupCard();

        TMP_Text buttonText = card.UpgradeButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null) buttonText.text = "Respawn";

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

        if (itemData == null) return;

        card.gameObject.SetActive(true);
        card.UpgradeButton.interactable = true;

        string statName = "";
        float increaseAmount = 0f;
        float totalValue = 0f;
        StatType activeStatType = StatType.MaxHealth;

        ResolveItemStats(itemData, nextLevel, currentLevel, ref activeStatType, ref statName, ref increaseAmount, ref totalValue);

        bool isFloat = IntStatArray == null || !IntStatArray.Contains(activeStatType);

        if (isFloat)
        {
            card.SetupCard(itemData.ItemName, nextLevel, statName, increaseAmount, totalValue, itemData.Icon);
        }
        else
        {
            card.SetupCard(itemData.ItemName, nextLevel, statName, Mathf.RoundToInt(increaseAmount), Mathf.RoundToInt(totalValue), itemData.Icon);
        }

        card.UpgradeButton.onClick.RemoveAllListeners();
        card.UpgradeButton.onClick.AddListener(() => { OnUpgradeClicked(itemId); });
        TMP_Text buttonText = card.UpgradeButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null) buttonText.text = "Upgrade";
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
            int syncedQueues = (PlayerLevelManager.Instance != null) ? Mathf.Max(0, PlayerLevelManager.Instance.LocalPendingUpgrades - 1) : 0;
            PlayerLevelManager.Instance.ForceSyncPendingUpgradesServerRpc(playerToRevive.OwnerClientId, syncedQueues);

            if (NetworkManager.Singleton != null)
            {
                PauseManager.Instance.ToggleLevelUpPauseServerRpc(playerToRevive.OwnerClientId, true);
            }

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
        foreach (var card in _upgradeCard)
        {
            card.UpgradeButton.onClick.RemoveAllListeners();
            card.UpgradeButton.interactable = false;
        }

        if (PlayerLevelManager.Instance != null)
        {
            PlayerLevelManager.Instance.ConsumePendingUpgrade();
        }

        int remainingUpgrades = PlayerLevelManager.Instance != null ? PlayerLevelManager.Instance.LocalPendingUpgrades : 0;

        if (remainingUpgrades > 0)
        {
            StartCoroutine(WaitServerSyncAndShowNextCard());
        }
        else
        {
            if (popupUI != null)
            {
                popupUI.ClosePopup(() =>
                {
                    _isChoosing = false;
                    if (_pendingCountText != null) _pendingCountText.gameObject.SetActive(false);

                    if (PauseMenuUI.Instance != null) PauseMenuUI.Instance.IsLevelUpActive = false;

                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
                    {
                        PauseManager.Instance.ToggleLevelUpPauseServerRpc(NetworkManager.Singleton.LocalClientId, false);
                    }

                    if (PauseManager.Instance != null && PauseManager.Instance.IsGamePaused.Value && PauseMenuUI.Instance != null)
                    {
                        PauseMenuUI.Instance.ResumeGame();
                    }

                    if (PlayerLevelManager.Instance != null)
                    {
                        PlayerLevelManager.Instance.OnUpgradeSelected();
                    }
                });
            }
        }
    }

    private IEnumerator WaitServerSyncAndShowNextCard()
    {
        Sequence outSeq = DOTween.Sequence().SetUpdate(true);
        int activeIndex = 0;

        foreach (UpgradeCard card in _upgradeCard)
        {
            if (card.gameObject.activeSelf)
            {
                card.transform.DOKill();
                outSeq.Insert(activeIndex * _cardStaggerDelay, card.transform.DOScale(Vector3.zero, _cardAnimDuration).SetEase(Ease.InBack));
                activeIndex++;
            }
        }

        yield return outSeq.WaitForCompletion();
        yield return new WaitForSecondsRealtime(0.1f);

        // Update the current index and play animation right before showing new cards
        _currentUpgradeIndex++;
        UpdatePendingCountText(true);

        ShowNextCards();
    }
}