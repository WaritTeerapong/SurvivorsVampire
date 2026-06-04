using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData_SO", menuName = "PlayerData_SO", order = 0)]
public class PlayerData_SO : ScriptableObject
{
    public int PlayerId;
    public int MaxHealth;
    public int MoveSpeed;
    public int ATKDamage;
    public float ATKSpeed; // Attacks per second
    public float ATKRange;
}