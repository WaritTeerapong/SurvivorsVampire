using System.Collections.Generic;
using Unity.Netcode;

public static class GameSessionData
{
    public static Dictionary<ulong, int> PlayerSelections = new Dictionary<ulong, int>();
    public static Dictionary<ulong, NetworkObject> SpawnedDummies = new Dictionary<ulong, NetworkObject>();
}