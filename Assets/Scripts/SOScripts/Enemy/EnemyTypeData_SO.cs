using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData_SO", menuName = "DataSO/EnemyTypeData_SO", order = 0)]
public class EnemyTypeData_SO : ScriptableObject
{
    public string EnemyName;
    public GameObject EnemyPrefab;
    public bool IsRange;
    public EnemyTier[] enemyTiers;
    public int XPValue = 10;

    [Header("Boids Flocking Settings")]
    public bool UseBoids = true; // toggle boids
    public LayerMask EnemyLayer; // for only enemy layer detection

    public float BoidsDetectionRadius = 2.5f; // Radius for neighbour detection
    [Range(0f, 3f)] public float SeparationWeight = 2f; // Separation Force (ดันออกจากกัน)
    [Range(0f, 3f)] public float AlignmentWeight = 1f; // Alignment Force (เดินเรียงแถว)
    [Range(0f, 3f)] public float CohesionWeight = 1f; // Cohesion Force (เกาะกลุ่ม)
    [Range(0f, 3f)] public float TargetWeight = 2f; // Target Force (ความอยากในการเดินหา Player)

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
