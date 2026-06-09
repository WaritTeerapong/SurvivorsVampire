using UnityEngine;

public class BaseData_SO : ScriptableObject
{
    public BaseStat Stat;
}

[System.Serializable]
public struct BaseStat
{
    public int MaxHealth;
    public int MoveSpeed;
    public int ATKDamage;
    public float ATKSpeed; // Attacks per second
    public float ATKRange;
    // 100
    // 5
    // 5
    // 1
    // 5
}

