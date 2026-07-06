using UnityEngine;

public interface IDamageble
{
    Transform TargetPoint { get; }
    void TakeDamage(int damage);
}