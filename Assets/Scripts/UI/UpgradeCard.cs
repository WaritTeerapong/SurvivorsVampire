using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeCard : MonoBehaviour
{
    public TMP_Text StatName;
    public TMP_Text StatLevel;
    public TMP_Text StatBonus;

    public void Setup(string name, int level, int bonus)
    {
        StatName.text = name;
        StatLevel.text = $"Lv.{level}";
        StatBonus.text = $" +{bonus}";
    }
}
