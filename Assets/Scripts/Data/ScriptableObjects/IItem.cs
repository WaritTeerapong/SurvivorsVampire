using UnityEngine;

public interface IItem
{
    string Id { get; }
    string ItemName { get; }
    Sprite Icon { get; }
    int MaxLevel { get; }
}
