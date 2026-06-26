using Unity.Netcode;
using UnityEngine;

public class VFXManager : NetworkBehaviour
{
    public static VFXManager Instance { get; private set; }

    public GameObject BloodVFXPrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlayVFXAtPostion(Vector3 targetPos)
    {
        if (!IsServer) return;
        PlayVFXClientRpc(targetPos);
    }

    [Rpc(SendTo.Everyone)]
    private void PlayVFXClientRpc(Vector3 pos)
    {
        if (ObjectPoolManager.Instance == null) return;

        ParticleSystem ps = ObjectPoolManager.Instance.SpawnObject<ParticleSystem>(
            BloodVFXPrefab,
            pos,
            Quaternion.identity,
            PoolCategory.VFX
        );

        if (ps != null)
        {
            ps.Play();
        }
    }
}