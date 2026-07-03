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
        if (PlayerManager.Instance == null || PlayerManager.Instance.ActiveTargets.Count == 0)
        {
            NearestTarget = null;
            SqrDistanceToTarget = Mathf.Infinity;
            return;
        }

        Vector3 myPos = transform.position;
        if (TryGetComponent<Enemy>(out Enemy e)) myPos = e.TargetPoint.position;
        else if (TryGetComponent<Boss>(out Boss b)) myPos = b.TargetPoint.position;

        float shortestDistanceSqr = Mathf.Infinity;
        Transform nearestPlayer = null;

        foreach (Transform playerTransform in PlayerManager.Instance.ActiveTargets)
        {
            if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy) continue;

            Player p = playerTransform.GetComponent<Player>();
            Vector3 targetPos = p != null ? p.TargetPoint.position : playerTransform.position;

            float sqrDistance = (targetPos - myPos).sqrMagnitude;

            if (sqrDistance < shortestDistanceSqr)
            {
                shortestDistanceSqr = sqrDistance;
                nearestPlayer = playerTransform; // Store the root to support logic that requires it
            }
        }

        NearestTarget = nearestPlayer;
        SqrDistanceToTarget = shortestDistanceSqr;
    }
}