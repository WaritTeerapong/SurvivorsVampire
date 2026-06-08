using System.Collections;
using UnityEngine;

public class EnemyDetector : MonoBehaviour
{
    public Transform NearestTarget { get; private set; }
    public float SqrDistanceToTarget { get; private set; }
    public float UpdateTargetInterval = 0.5f;

    private Coroutine _findTargetCoroutine;

    public void StartDetect()
    {
        if (_findTargetCoroutine == null) _findTargetCoroutine = StartCoroutine(FindTargetRoutine());
    }

    public void StopDetect()
    {
        if (_findTargetCoroutine != null)
        {
            StopCoroutine(_findTargetCoroutine);
            _findTargetCoroutine = null;
        }

        NearestTarget = null;
    }

    private IEnumerator FindTargetRoutine()
    {
        while (true)
        {
            FindNearestPlayer();
            yield return new WaitForSeconds(UpdateTargetInterval);
        }
    }

    private void FindNearestPlayer()
    {
        if (PlayerManager.Instance == null || PlayerManager.Instance.ActivePlayer.Count == 0)
        {
            NearestTarget = null;
            SqrDistanceToTarget = Mathf.Infinity;
            return;
        }

        float shortestDistanceSqr = Mathf.Infinity;
        Transform nearestPlayer = null;

        foreach (Transform player in PlayerManager.Instance.ActivePlayer)
        {
            if (player == null || !player.gameObject.activeInHierarchy) continue;

            float sqrDistance = (player.position - transform.position).sqrMagnitude;
            if (sqrDistance < shortestDistanceSqr)
            {
                shortestDistanceSqr = sqrDistance;
                nearestPlayer = player;
            }
        }

        NearestTarget = nearestPlayer;
        SqrDistanceToTarget = shortestDistanceSqr;
    }

}