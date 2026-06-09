using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeCard : MonoBehaviour
{
    public TMP_Text StatNameText;
    public TMP_Text StatLevelText;
    public TMP_Text StatBonusText;
    public Button UpgradeButton;

    // increaseAmount = newLevel.bonus - lastLevel.bonus
    // totalValue = baseValue + newLevel.bonus
    public void Setup(string statName, int newLevel, int increaseAmount, int totalValue)
    {
        StatNameText.text = statName;
        StatLevelText.text = $"Lv.{newLevel}";
        StatBonusText.text = $" +{increaseAmount} ({totalValue})";
    }

    public void Setup(string statName, int newLevel, float increaseAmount, float totalValue)
    {
        StatNameText.text = statName;
        StatLevelText.text = $"Lv.{newLevel}";
        StatBonusText.text = $" +{increaseAmount:F1} ({totalValue:F1})";
    }
}
