using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class DroneController : MonoBehaviour
{
    [Header("References")]
    public CameraController cameraController; // Main Camera script

    [Header("Strafe")]
    public float maxHorizontalSpeed = 20f;  // A/D + backward
    public float maxVerticalSpeed   = 8f;   // Space / Ctrl
    public float strafeAcceleration = 40f;  // how fast to reach strafe/hover speed

    [Header("Forward Flight")]
    public float planeMaxForwardSpeed   = 25f;  // forward speed when W is held
    public float planeForwardAccel      = 20f;  // acceleration for forward speed
    public float turnSpeed              = 90f;  // deg/sec rotation toward camera target
    public float aimDistance            = 40f;  // distance of invisible target in front of camera

    [Header("Damping")]
    public float lateralDamping = 2f;          // extra damping of unwanted drift

    private Rigidbody rb;

    // inputs
    private float inputH;       // A/D
    private float inputV;       // W/S
    private float inputUpDown;  // Space/Ctrl

    private float throttle;     // 0..1 forward throttle when W is held

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 3f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void Update()
    {
        // --- INPUT (same as before) ---
        inputH = Input.GetAxis("Horizontal");  // A/D
        inputV = Input.GetAxis("Vertical");    // W/S

        inputUpDown = 0f;
        if (Input.GetKey(KeyCode.Space))
            inputUpDown += 1f;
        if (Input.GetKey(KeyCode.LeftControl))
            inputUpDown -= 1f;

        // W = airplane forward throttle
        bool forwardPressed = inputV > 0.01f;
        float desiredThrottle = forwardPressed ? 1f : 0f;

        // Smooth throttle so it ramps in/out
        throttle = Mathf.MoveTowards(throttle, desiredThrottle, Time.deltaTime * 2f);
    }

    void FixedUpdate()
    {
        if (cameraController == null) return;

        float dt = Time.fixedDeltaTime;
        Transform cam = cameraController.transform;

        // ======================================================
        // 1. ROTATION: turn toward invisible camera target WHEN W is held
        // ======================================================
        if (throttle > 0.01f)
        {
            Vector3 aimPoint = cameraController.GetAimPoint(aimDistance);
            Vector3 toTarget = aimPoint - transform.position;

            if (toTarget.sqrMagnitude > 0.001f)
            {
                Vector3 desiredForward = toTarget.normalized;
                Quaternion targetRot   = Quaternion.LookRotation(desiredForward, Vector3.up);

                Quaternion newRot = Quaternion.RotateTowards(
                    rb.rotation,
                    targetRot,
                    turnSpeed * dt
                );

                rb.MoveRotation(newRot);
            }
        }
        // If W not pressed → no auto-turning; you can hover / strafe and just look around.

        // Current velocity split into forward + sideways components
        Vector3 vel        = rb.linearVelocity;
        Vector3 forwardDir = transform.forward;
        float   forwardSpeed = Vector3.Dot(vel, forwardDir);
        Vector3 forwardVel   = forwardDir * forwardSpeed;
        Vector3 lateralVel   = vel - forwardVel;

        // ======================================================
        // 2. STRAFE / HOVER (A/D, S, Space, Ctrl)  — classic mode for sideways / vertical
        //    These affect only the "lateral" part of velocity.
        // ======================================================

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
        Vector3 desiredLateralVertical   = Vector3.up * (inputUpDown * maxVerticalSpeed);
        Vector3 desiredLateralVel        = desiredLateralHorizontal + desiredLateralVertical;

        // Move lateralVel towards desiredLateralVel
        Vector3 lateralDelta = desiredLateralVel - lateralVel;
        float maxLateralChange = strafeAcceleration * dt;
        lateralDelta = Vector3.ClampMagnitude(lateralDelta, maxLateralChange);

        rb.AddForce(lateralDelta, ForceMode.VelocityChange);

        // ======================================================
        // 3. AIRPLANE FORWARD FLIGHT (arch toward target)
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
        forwardVel   = forwardDir * forwardSpeed;
        lateralVel   = vel - forwardVel;

        rb.AddForce(-lateralVel * lateralDamping * dt, ForceMode.VelocityChange);
    }
}
