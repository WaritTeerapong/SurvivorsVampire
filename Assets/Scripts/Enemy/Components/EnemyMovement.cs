using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public void MoveToward(Transform target, float speed)
    {
        if (target == null) return;

        transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
    }
}