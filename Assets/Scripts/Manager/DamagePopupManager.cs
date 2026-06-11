using Unity.Netcode;
using UnityEngine;

public class DamagePopupManager : NetworkBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    public GameObject DamagePopupPrefab;

    public Color PlayerHitColor = Color.red;
    public Color EnemyHitColor = Color.white;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowPopup(Vector3 position, int damage, bool isPlayerTarget)
    {
        if (!IsServer) return;
        ShowPopupClientRpc(position, damage, isPlayerTarget);
    }

    [Rpc(SendTo.Everyone)]
    private void ShowPopupClientRpc(Vector3 position, int damage, bool isPlayerTarget)
    {
        if (DamagePopupPrefab == null || ObjectPoolManager.Instance == null) return;

        GameObject popupObj = ObjectPoolManager.Instance.SpawnObject(DamagePopupPrefab, position, Quaternion.identity, PoolCategory.DamagePopup);

        if (popupObj != null)
        {
            DamagePopup popupScript = popupObj.GetComponent<DamagePopup>();
            if (popupScript != null)
            {
                Color targetColor = isPlayerTarget ? PlayerHitColor : EnemyHitColor;
                popupScript.Setup(damage, targetColor);
            }
        }
    }
}