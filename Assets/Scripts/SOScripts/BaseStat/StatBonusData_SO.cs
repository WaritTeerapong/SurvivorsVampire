using UnityEngine;

[CreateAssetMenu(fileName = "StatBonusData_SO", menuName = "DataSO/StatBonusData_SO")]
public class StatBonusData_SO : ScriptableObject
{
    public StatData[] StatLevels;
    public BaseStat GetBonusStat(int level)
    {
        foreach (var stat in StatLevels)
        {
            if (stat.Level == level) return stat.FlatBonus;
        }
        Debug.LogWarning($"Level {level} not found in LevelData_SO");
        return new BaseStat(); // Not found
    }
}

[System.Serializable]
public struct StatData
{
    public int Level;
    public BaseStat FlatBonus;
}



