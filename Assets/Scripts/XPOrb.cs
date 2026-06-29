using Unity.Netcode;
using UnityEngine;

public class XPOrb : NetworkBehaviour
{
    public int XPValue { get; private set; }

    public void Initialize(int xp)
    {
        XPValue = xp;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerLevelManager.Instance.RequestGainXPRpc(XPValue);

            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }
    }
}