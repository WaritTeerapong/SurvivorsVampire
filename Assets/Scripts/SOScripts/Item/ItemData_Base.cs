using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

public abstract class ItemData_Base<T> : ScriptableObject
{
    [Header("Base Info")]
    public string Id;
    public string ItemName;
    public Sprite Icon;
    public List<T> BonusPerLevel;

    [Header("Upgrade Info")]
    public int MaxLevel => BonusPerLevel != null ? BonusPerLevel.Count : 0;
    public T GetBonusForLevel(int level)
    {
        // Handle empty data in BonusPerLevel
        if (BonusPerLevel == null || BonusPerLevel.Count == 0)
        {
            Debug.LogWarning($"BonusPerLevel in {ItemName} is Empty!");
            // return default of that Type (ex. null)
            return default(T);
        }

        // Handle negative level
        if (level <= 1) return BonusPerLevel[0];

        // Handle Max level & level out of range
        if (level >= BonusPerLevel.Count) return BonusPerLevel[BonusPerLevel.Count - 1];

        // Handle Normal case
        return BonusPerLevel[level - 1];

    }
}
