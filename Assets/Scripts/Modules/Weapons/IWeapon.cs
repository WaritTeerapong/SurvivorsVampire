using System.Collections;
using UnityEngine;

public interface IWeapon
{
    public float AtkRange { get; }
    public void PrepareToAttack();

}
