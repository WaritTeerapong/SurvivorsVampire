using UnityEngine;

[CreateAssetMenu(fileName = "StatBonusData_SO", menuName = "DataSO/StatBonusData_SO")]
public class StatBonusData_SO : ScriptableObject
{
    public StatData[] StatLevels;
}

public struct StatData
{
    public int Level;
    public BaseStat FlatBonus;
}



