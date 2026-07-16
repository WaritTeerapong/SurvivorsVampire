using UnityEngine;

[CreateAssetMenu(fileName = "LevelData_SO", menuName = "DataSO/LevelData_SO", order = 0)]
public class LevelData_SO : ScriptableObject
{
    public LevelData[] Levels = new LevelData[4];

    public int GetNeededXPForLevel(int level)
    {
        foreach (var lvlData in Levels)
        {
            if (lvlData.Level == level) return lvlData.XPNeeded;
        }
        Debug.LogWarning($"Level {level} not found in LevelData_SO");
        return -1; // Not found
    }
}

[System.Serializable]
public struct LevelData
{
    public int Level; // Level 1
    public int XPNeeded;
}
