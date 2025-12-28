using UnityEngine;

public class DroneVisualLean : MonoBehaviour
{
    [Header("References")]
    public Transform visualModel;   // child with the mesh

    [Header("Lean Angles")]
    public float maxSideLean = 25f; // roll left/right (Z)
    public float maxForwardLean = 15f; // nose up/down (X)

    [Header("Smoothing / Bounce")]
    public float leanSmoothTime = 0.08f;  // lower = snappier, more bounce

    private Quaternion baseLocalRotation;

    private float currentSide;
    private float currentForward;
    private float sideVel;
    private float forwardVel;

    void Awake()
    {
        if (visualModel == null && transform.childCount > 0)
            visualModel = transform.GetChild(0);

        if (visualModel != null)
            baseLocalRotation = visualModel.localRotation;
    }

    void Update()
    {
        if (visualModel == null) return;

        // Read input directly (doesn’t affect physics)
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S

        // Desired lean:
        // - Press A → lean left, D → lean right
        float targetSide = -h * maxSideLean;

        // - Press W → nose slightly down, S → nose slightly up
        float targetForward = -v * maxForwardLean;

        // SmoothDamp gives that little overshoot / “bouncy” feel
        currentSide = Mathf.SmoothDampAngle(
            currentSide, targetSide, ref sideVel, leanSmoothTime);

        currentForward = Mathf.SmoothDampAngle(
            currentForward, targetForward, ref forwardVel, leanSmoothTime);

        Quaternion leanRot = Quaternion.Euler(currentForward, 0f, currentSide);
        visualModel.localRotation = baseLocalRotation * leanRot;
    }
}
