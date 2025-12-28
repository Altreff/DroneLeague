using UnityEngine;

public enum DroneControlMode
{
    Player,
    AI
}


[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class DroneController : MonoBehaviour
{

    [Header("Control")]
    public DroneControlMode controlMode = DroneControlMode.Player;

    // These are set by AI when controlMode == AI
    [HideInInspector] public float aiInputH;       // -1..1 (A/D)
    [HideInInspector] public float aiInputV;       // -1..1 (W/S)
    [HideInInspector] public float aiInputUpDown;  // -1..1 (Space/Ctrl)

    // Helper for AI scripts
    public void SetAIInput(float h, float v, float upDown)
    {
        aiInputH = Mathf.Clamp(h, -1f, 1f);
        aiInputV = Mathf.Clamp(v, -1f, 1f);
        aiInputUpDown = Mathf.Clamp(upDown, -1f, 1f);
    }

    [Header("References")]
    public CameraController cameraController; // Main Camera script

    [Header("Strafe")]
    public float maxHorizontalSpeed = 20f;  // A/D + backward
    public float maxVerticalSpeed = 8f;   // Space / Ctrl
    public float strafeAcceleration = 40f;  // how fast to reach strafe/hover speed

    [Header("Forward Flight")]
    public float planeMaxForwardSpeed = 25f;  // forward speed when W is held
    public float planeForwardAccel = 20f;  // acceleration for forward speed
    public float turnSpeed = 90f;  // deg/sec rotation toward camera target
    public float aimDistance = 40f;  // distance of invisible target in front of camera

    [Header("Upright Stabilization")]
    public float uprightStrength = 5f;   // how fast it tries to stand up (bigger = snappier)
    public float maxUprightAngle = 80f;  // safety clamp for extreme tilts (degrees)

    [Header("Damping")]
    public float lateralDamping = 2f;    // extra damping of unwanted drift

    private Rigidbody rb;

    // inputs
    private float inputH;       // A/D
    private float inputV;       // W/S
    private float inputUpDown;  // Space/Ctrl

    private float throttle;     // 0..1 forward throttle when W is held

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // 🔹 Auto-find CameraController if not set in Inspector
        if (cameraController == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraController = mainCam.GetComponent<CameraController>();
            }
        }

        rb.useGravity = false;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 3f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }


    void Update()
    {
        if (controlMode == DroneControlMode.Player)
        {
            // --- PLAYER INPUT (same as before) ---
            inputH = Input.GetAxis("Horizontal");  // A/D
            inputV = Input.GetAxis("Vertical");    // W/S

            inputUpDown = 0f;
            if (Input.GetKey(KeyCode.Space))
                inputUpDown += 1f;
            if (Input.GetKey(KeyCode.LeftControl))
                inputUpDown -= 1f;
        }
        else // AI mode
        {
            // --- AI INPUT (comes from another script) ---
            inputH = aiInputH;
            inputV = aiInputV;
            inputUpDown = aiInputUpDown;
        }

        // W = airplane forward throttle (works for both Player and AI)
        bool forwardPressed = inputV > 0.01f;
        float desiredThrottle = forwardPressed ? 1f : 0f;

        // Smooth throttle so it ramps in/out
        throttle = Mathf.MoveTowards(throttle, desiredThrottle, Time.deltaTime * 2f);
    }


    void FixedUpdate()
    {
        // Try to recover if cameraController somehow got lost
        if (cameraController == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cameraController = mainCam.GetComponent<CameraController>();
        }

        // If still nothing → we can't use camera-relative movement, so just bail
        if (cameraController == null)
            return;

        float dt = Time.fixedDeltaTime;
        Transform cam = cameraController.transform;

        // ======================================================
        // 1. ROTATION BASE: turn toward camera target when W is held
        //    (but DON'T move rotation yet; just compute desiredRot)
        // ======================================================
        Quaternion currentRot = rb.rotation;
        Quaternion desiredRot = currentRot;

        if (throttle > 0.01f)
        {
            Vector3 aimPoint = cameraController.GetAimPoint(aimDistance);
            Vector3 toTarget = aimPoint - transform.position;

            if (toTarget.sqrMagnitude > 0.001f)
            {
                Vector3 desiredForward = toTarget.normalized;
                Quaternion targetRot = Quaternion.LookRotation(desiredForward, Vector3.up);

                desiredRot = Quaternion.RotateTowards(
                    currentRot,
                    targetRot,
                    turnSpeed * dt
                );
            }
        }

        // ======================================================
        // 1.5. UPRIGHT STABILIZATION (springy)
        // Always try to align drone's UP with world UP, softly.
        // ======================================================
        // Get current up from desiredRot (after camera-turn)
        Vector3 currentUp = desiredRot * Vector3.up;
        float angleFromUp = Vector3.Angle(currentUp, Vector3.up);

        if (angleFromUp > 0.01f)
        {
            // Rotation that would fully align up to world up
            Quaternion uprightCorrection = Quaternion.FromToRotation(currentUp, Vector3.up);
            Quaternion uprightTargetRot = uprightCorrection * desiredRot;

            // Optional safety: don't allow insane flips (> maxUprightAngle)
            if (angleFromUp > maxUprightAngle)
            {
                // clamp by slerping halfway toward uprightTargetRot
                uprightTargetRot = Quaternion.Slerp(desiredRot, uprightTargetRot, 0.5f);
            }

            // "Springy" blend toward upright using exponential-like smoothing
            float t = 1f - Mathf.Exp(-uprightStrength * dt); // 0..1
            desiredRot = Quaternion.Slerp(desiredRot, uprightTargetRot, t);
        }

        // Now we finally apply the rotation once
        rb.MoveRotation(desiredRot);

        // Use this rotation as forward direction for velocity logic
        Vector3 forwardDir = desiredRot * Vector3.forward;

        // ======================================================
        // 2. STRAFE / HOVER (A/D, S, Space, Ctrl)  — classic mode for sideways / vertical
        //    These affect only the "lateral" part of velocity.
        // ======================================================

        // Current velocity split into forward + sideways components
        Vector3 vel = rb.linearVelocity;
        float forwardSpeed = Vector3.Dot(vel, forwardDir);
        Vector3 forwardVel = forwardDir * forwardSpeed;
        Vector3 lateralVel = vel - forwardVel;

        // Move in camera space for strafe/backwards, like before
        Vector3 camForward = cam.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cam.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 moveHorizontal = Vector3.zero;

        // A/D strafing always allowed
        moveHorizontal += camRight * inputH;

        // Backwards (S) uses camera forward (classic)
        if (inputV < -0.01f)
            moveHorizontal += camForward * inputV; // note: inputV is negative here

        if (moveHorizontal.sqrMagnitude > 1f)
            moveHorizontal.Normalize();

        Vector3 desiredLateralHorizontal = moveHorizontal * maxHorizontalSpeed;
        Vector3 desiredLateralVertical = Vector3.up * (inputUpDown * maxVerticalSpeed);
        Vector3 desiredLateralVel = desiredLateralHorizontal + desiredLateralVertical;

        // Move lateralVel towards desiredLateralVel
        Vector3 lateralDelta = desiredLateralVel - lateralVel;
        float maxLateralChange = strafeAcceleration * dt;
        lateralDelta = Vector3.ClampMagnitude(lateralDelta, maxLateralChange);

        rb.AddForce(lateralDelta, ForceMode.VelocityChange);

        // ======================================================
        // 3. AIRPLANE FORWARD FLIGHT (arch toward target)
        //    (forwardDir is now based on our final desiredRot)
        // ======================================================
        float targetForwardSpeed = throttle * planeMaxForwardSpeed; // 0 when W is not held

        float forwardDelta = targetForwardSpeed - forwardSpeed;
        float maxForwardChange = planeForwardAccel * dt;
        forwardDelta = Mathf.Clamp(forwardDelta, -maxForwardChange, maxForwardChange);

        rb.AddForce(forwardDir * forwardDelta, ForceMode.VelocityChange);

        // ======================================================
        // 4. Extra damping of unused drift (optional but helps the "arched" feel)
        // ======================================================
        // Recalculate velocity after our forces
        vel = rb.linearVelocity;
        forwardSpeed = Vector3.Dot(vel, forwardDir);
        forwardVel = forwardDir * forwardSpeed;
        lateralVel = vel - forwardVel;

        rb.AddForce(-lateralVel * lateralDamping * dt, ForceMode.VelocityChange);
    }
}
