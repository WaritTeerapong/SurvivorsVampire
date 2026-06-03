using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform Target;
    public Vector3 Offset;

    void LateUpdate()
    {
        if (Target != null)
        {
            transform.position = Target.position + Offset;
        }
    }
}
