using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeCard : MonoBehaviour
{
    public TMP_Text ItemNameText;
    public TMP_Text ItemLevelText;
    public TMP_Text StatNameText;
    public TMP_Text StatBonusText;
    public Button UpgradeButton;
    public Image Icon;

    public Sprite ReviveIcon;

    // increaseAmount = newLevel.bonus - lastLevel.bonus
    // totalValue = baseValue + newLevel.bonus
    public void SetupCard(string itemName, int newLevel, string statName, int increaseAmount, int totalValue, Sprite icon)
    {
        ItemNameText.text = itemName;
        ItemLevelText.text = $"Lv.{newLevel}";
        StatNameText.text = statName;
        StatBonusText.text = $" +{increaseAmount} ({totalValue})";
        Icon.sprite = icon;
    }

    public void SetupCard(string itemName, int newLevel, string statName, float increaseAmount, float totalValue, Sprite icon)
    {
        ItemNameText.text = itemName;
        ItemLevelText.text = $"Lv.{newLevel}";
        StatNameText.text = statName;
        StatBonusText.text = $" +{increaseAmount:F1} ({totalValue:F1})";
        Icon.sprite = icon;
    }

    // For Respawn Event
    public void SetupCard()
    {
        ItemNameText.text = $"Respawn Friend";
        ItemLevelText.text = "";
        StatNameText.text = "";
        StatBonusText.text = "";

        TMP_Text UpgradeButtonText = UpgradeButton.GetComponentInChildren<TMP_Text>();
        UpgradeButtonText.text = "Respawn";

        Icon.sprite = ReviveIcon;
        return;
    }
}
