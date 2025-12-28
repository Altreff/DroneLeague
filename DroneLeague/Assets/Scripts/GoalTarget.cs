using UnityEngine;

public class GateTarget : MonoBehaviour
{
    [Tooltip("If empty, will use this object's position as the target.")]
    public Transform targetPoint;

    public Vector3 GetTargetPosition()
    {
        return targetPoint != null ? targetPoint.position : transform.position;
    }
}
