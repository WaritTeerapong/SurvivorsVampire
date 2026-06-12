using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData_SO", menuName = "DataSO/EnemyTypeData_SO", order = 0)]
public class EnemyTypeData_SO : ScriptableObject
{
    public string EnemyName;
    public bool IsRange;
    public EnemyTier[] enemyTiers;
    public int XPValue = 10;
    public EnemyTier Setup(int tier)
    {
        if (enemyTiers == null || enemyTiers.Length == 0)
        {
            Debug.LogError($"[EnemyData] {EnemyName} has no tiers set up in the Inspector!");
            return new EnemyTier(); //return empty struct
        }

        foreach (EnemyTier currentTier in enemyTiers)
        {
            if (currentTier.Tier == tier)
            {
                return currentTier;
            }
        }

        Debug.LogWarning($"[EnemyData] {EnemyName} does not have Tier {tier}. Defaulting to the lowest tier.");
        return new EnemyTier();
    }
}

[System.Serializable]
public struct EnemyTier
{
    public int Tier;
    public int EnemyID;
    public Color color;
    public EnemyStats enemyStats;
}

[System.Serializable]
public struct EnemyStats
{
    public int MaxHealth;
    public int MoveSpeed;
    public int ATKDamage;
    public float ATKSpeed;
    public float ATKRange;
}
