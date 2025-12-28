using UnityEngine;

public class DroneVisuals : MonoBehaviour
{
    public Transform visualModel;

    [Header("Lean Angles")]
    public float maxSideLean = 30f; // roll left/right
    public float maxForwardLean = 15f; // nose down/up

    [Header("Movement → Lean")]
    public float speedForFullLean = 15f; // speed at which we reach max lean

    [Header("Smoothing")]
    public float leanSmoothTime = 0.1f;

    private Rigidbody rb;
    private float currentSideLean;
    private float currentForwardLean;
    private float sideLeanVel;
    private float forwardLeanVel;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        if (visualModel == null || rb == null) return;

        Vector3 vel = rb.linearVelocity;
        float speed = vel.magnitude;

        float targetSideLean = 0f;
        float targetFwdLean = 0f;

        if (speed > 0.01f)
        {
            // 1. Direction of movement in local drone space
            Vector3 dir = vel / speed; // normalized world dir
            Vector3 localDir = transform.InverseTransformDirection(dir);

            // How strong lean should be based on speed (0..1)
            float speedFactor = Mathf.Clamp01(speed / speedForFullLean);

            // localDir.x: left/right component of movement
            // localDir.z: forward/back component of movement
            targetSideLean =
                -localDir.x * maxSideLean * speedFactor;     // moving right → lean right, etc.

            targetFwdLean =
                -localDir.z * maxForwardLean * speedFactor;  // moving forward → nose down a bit
        }

        // 2. Smooth towards target leans
        currentSideLean = Mathf.SmoothDampAngle(
            currentSideLean, targetSideLean,
            ref sideLeanVel, leanSmoothTime
        );

        currentForwardLean = Mathf.SmoothDampAngle(
            currentForwardLean, targetFwdLean,
            ref forwardLeanVel, leanSmoothTime
        );

        // 3. Apply to visuals (local rotation only)
        Quaternion leanRot = Quaternion.Euler(currentForwardLean, 0f, currentSideLean);
        visualModel.localRotation = leanRot;
    }
}
