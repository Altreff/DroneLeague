using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;          // DroneRigid here

    [Header("Camera Offset")]
    public float distance = 8f;       // how far behind the drone
    public float heightOffset = 1f;   // small vertical offset if you want

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float minPitch = -30f;
    public float maxPitch = 60f;

    [Header("Smoothing")]
    public float positionSmooth = 5f;
    public float rotationSmooth = 10f;

    private float yaw;
    private float pitch;

    void Start()
    {
        // initialise angles from current camera rotation
        Vector3 euler = transform.rotation.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Mouse only controls camera orientation
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw   += mouseX * mouseSensitivity;
        pitch -= mouseY * mouseSensitivity;
        pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion desiredRot = Quaternion.Euler(pitch, yaw, 0f);

        // 2. Camera sits behind the drone along the look direction
        Vector3 desiredPos =
            target.position
            - desiredRot * Vector3.forward * distance
            + Vector3.up * heightOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            positionSmooth * Time.deltaTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRot,
            rotationSmooth * Time.deltaTime
        );
    }

    // Invisible target in the centre of the screen, 'distance' units in front
    public Vector3 GetAimPoint(float targetDistance)
    {
        return transform.position + transform.forward * targetDistance;
    }
}
