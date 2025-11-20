using UnityEngine;

public class DroneVisuals : MonoBehaviour
{
    public Transform visualModel;

    [Header("Lean Angles")]
    public float maxSideLean = 30f;
    public float maxForwardLean = 15f;

    [Header("Sensitivity")]
    public float lateralLeanSensitivity = 1f;    // how strongly velocity affects lean
    public float forwardLeanSensitivity = 1f;   // for acceleration or speed
    public float turnLeanSensitivity = 10f;     // how much turning adds roll

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

    void Update()
    {
        if (visualModel == null || rb == null) return;

        // 1. Velocity in local space (relative to drone)
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);

        // left/right velocity -> side lean
        float lateral = localVel.x;
        // forward speed -> forward lean (optional)
        float forward = localVel.z;

        // 2. Angular velocity around Y (turning)
        float turnRate = rb.angularVelocity.y; // rad/s

        // 3. Compute target lean
        float targetSideLean =
            -lateral * lateralLeanSensitivity +
            -turnRate * Mathf.Rad2Deg * turnLeanSensitivity * 0.01f;

        float targetForwardLean =
            -forward * forwardLeanSensitivity;

        // Clamp to max values
        targetSideLean = Mathf.Clamp(targetSideLean, -maxSideLean, maxSideLean);
        targetForwardLean = Mathf.Clamp(targetForwardLean, -maxForwardLean, maxForwardLean);

        // 4. Smooth damp
        currentSideLean = Mathf.SmoothDampAngle(
            currentSideLean, targetSideLean, ref sideLeanVel, leanSmoothTime
        );
        currentForwardLean = Mathf.SmoothDampAngle(
            currentForwardLean, targetForwardLean, ref forwardLeanVel, leanSmoothTime
        );

        // 5. Apply to visual
        Quaternion leanRot = Quaternion.Euler(currentForwardLean, 0f, currentSideLean);
        visualModel.localRotation = leanRot;
    }
}
