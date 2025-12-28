using UnityEngine;

[RequireComponent(typeof(DroneController))]
public abstract class DroneAIBrain : MonoBehaviour
{
    [Header("Difficulty")]
    public AIDifficulty difficulty = AIDifficulty.Medium;

    protected DroneController controller;

    // x = strafe (A/D), y = forward (W/S), z = up/down (Space/Ctrl)
    protected Vector3 currentInput;

    // difficulty-tuned parameters
    float inputScale;
    float inputSmoothSpeed;
    float noiseAmount;

    protected virtual void Awake()
    {
        controller = GetComponent<DroneController>();
        controller.controlMode = DroneControlMode.AI;
        SetupDifficulty();
    }

    void Update()
    {
        // 1. Let child AI decide desired input in [-1..1]
        Vector3 desired = ComputeDesiredInput();  // local (x=H, y=V, z=UpDown)

        // 2. Apply difficulty scaling
        desired *= inputScale;

        // add small randomness so Easy bots are sloppy
        if (noiseAmount > 0f)
        {
            desired.x += Random.Range(-noiseAmount, noiseAmount);
            desired.y += Random.Range(-noiseAmount, noiseAmount);
            desired.z += Random.Range(-noiseAmount, noiseAmount);
        }

        desired = Vector3.ClampMagnitude(desired, 1f);

        // 3. Smooth input over time so bots aren’t twitchy
        currentInput = Vector3.Lerp(currentInput, desired, Time.deltaTime * inputSmoothSpeed);

        // 4. Send to controller
        controller.SetAIInput(currentInput.x, currentInput.y, currentInput.z);
    }

    protected abstract Vector3 ComputeDesiredInput();

    void SetupDifficulty()
    {
        switch (difficulty)
        {
            case AIDifficulty.Easy:
                inputScale = 0.6f;  // weaker thrust
                inputSmoothSpeed = 3f;    // slower reactions
                noiseAmount = 0.25f; // more “drunk”
                break;
            case AIDifficulty.Medium:
                inputScale = 0.9f;
                inputSmoothSpeed = 6f;
                noiseAmount = 0.12f;
                break;
            case AIDifficulty.Hard:
                inputScale = 1.1f;  // slightly aggressive
                inputSmoothSpeed = 10f;   // snappy
                noiseAmount = 0.04f; // almost no randomness
                break;
        }
    }
}
