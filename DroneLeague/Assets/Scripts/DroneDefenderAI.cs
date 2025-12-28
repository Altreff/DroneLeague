using UnityEngine;

public class DroneDefenderAI : DroneAIBrain
{
    [Header("Defend Setup")]
    public GateTarget defendGate;       // гейт, который защищаем
    public Transform threatTarget;      // враг (игрок)


    public float ringRadius = 15f;  // distance from center to hold
    public float verticalOffset = 3f;   // preferred height over defend center
    public float approachDistance = 10f;  // how far from desired point we start slowing

    protected override Vector3 ComputeDesiredInput()
    {
        if (defendGate == null)
            return Vector3.zero;

        Vector3 centerPos = defendGate.GetTargetPosition();
        Vector3 desiredPos;

        if (threatTarget != null)
        {
            // Point on the line center → threat, at ringRadius distance from center
            Vector3 dirCenterToThreat = threatTarget.position - centerPos;
            if (dirCenterToThreat.sqrMagnitude < 0.001f)
                dirCenterToThreat = transform.forward;

            dirCenterToThreat.Normalize();
            desiredPos = centerPos + dirCenterToThreat * ringRadius;
        }
        else
        {
            // No threat assigned → just hover on ring in front of goal
            desiredPos = centerPos + transform.forward * ringRadius;
        }

        // desired height
        desiredPos.y = centerPos.y + verticalOffset;

        Vector3 toDesired = desiredPos - transform.position;
        float dist = toDesired.magnitude;

        if (dist < 0.5f)
            return Vector3.zero; // already in place

        Vector3 dirWorld = toDesired / dist;
        Vector3 dirLocal = transform.InverseTransformDirection(dirWorld);

        // Scale forward input with distance so it slows when close
        float moveFactor = Mathf.Clamp01(dist / approachDistance);

        float forwardInput = Mathf.Clamp(dirLocal.z * moveFactor, -1f, 1f);
        float strafeInput = Mathf.Clamp(dirLocal.x * moveFactor, -1f, 1f);
        float verticalInput = Mathf.Clamp(dirLocal.y * moveFactor, -1f, 1f);

        return new Vector3(strafeInput, forwardInput, verticalInput);
    }
}
