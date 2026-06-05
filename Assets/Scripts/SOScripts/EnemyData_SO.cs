using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData_SO", menuName = "Data/EnemyData_SO", order = 0)]
public class EnemyData_SO : ScriptableObject
{
    public EnemyType[] enemyType;
}

[System.Serializable]
public struct EnemyType
{
    public string EnemyName;
    public int EnemyID;
    public EnemyTier[] enemyTiers;

}

[System.Serializable]
public struct EnemyTier
{
    public int Tier;
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
