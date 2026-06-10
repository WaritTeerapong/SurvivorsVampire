using UnityEngine;

[CreateAssetMenu(fileName = "StatUpgradeDatabase_SO", menuName = "DataSO/StatUpgradeDatabase_SO")]
public class StatUpgradeDatabase_SO : ScriptableObject
{
    public StatUpgrade[] Stat;
}

[System.Serializable]
public struct StatUpgrade {
    public string UpgradeName;
    public string Description;
    public Sprite UpgradeIcon;
    public StatType StatType;
    public float[] BonusPerLevel;
    public int MaxLevel => BonusPerLevel != null ? BonusPerLevel.Length : 0;
    public float GetBonusForLevel(int level)
    {
        if (level <= 0 || BonusPerLevel == null || BonusPerLevel.Length == 0) return 0f;
        int index = level - 1;

        if (index < BonusPerLevel.Length)
        {
            return BonusPerLevel[index];
        }

        // Max Level
        return BonusPerLevel[BonusPerLevel.Length - 1];
    }
}
