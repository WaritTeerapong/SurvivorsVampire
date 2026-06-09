using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData_SO", menuName = "DataSO/PlayerData_SO", order = 0)]
public class PlayerData_SO : BaseData_SO
{
    public int PlayerId;
    public StatLevelCheckpoint StatLevel;
}
public struct StatLevelCheckpoint
{
    public int MaxHealthLevel;
    public int MoveSpeedLevel;
    public int ATKDamageLevel;
    public int ATKSpeedLevel; 
    public int ATKRangeLevel;

}
