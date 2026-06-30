using UnityEngine.UI;
public interface IItem
{
    string Id { get; }
    string ItemName { get; }
    Image Icon { get; }
    int MaxLevel { get; }
}
