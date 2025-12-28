using UnityEngine;

public class DronePropellers : MonoBehaviour
{
    [Header("Propeller Transforms")]
    public Transform[] propellers;

    [Header("Spin Settings")]
    public float idleRpm = 800f;   // when standing still
    public float maxExtraRpm = 1200f;  // added on top when at high speed
    public float speedForMaxRpm = 20f;    // drone speed at which we get full extra RPM
    public float rpmSmooth = 5f;     // how quickly RPM responds
    public Vector3 localAxis = Vector3.up;

    [Header("Sound")]
    public AudioSource propellerAudio;    // looped propeller sound
    public float minPitch = 0.8f;        // at idle RPM
    public float maxPitch = 2.0f;        // at max RPM
    public float minVolume = 0.2f;
    public float maxVolume = 1.0f;

    private Rigidbody rb;
    private float currentRpm;

    void Awake()
    {
        // Find Rigidbody on this object or in parents
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = GetComponentInParent<Rigidbody>();

        if (rb == null)
            Debug.LogWarning($"{nameof(DronePropellers)} on {name} couldn't find a Rigidbody in parents.");

        // If propellers not assigned, auto-find by name
        if (propellers == null || propellers.Length == 0)
        {
            var list = new System.Collections.Generic.List<Transform>();
            foreach (Transform t in GetComponentsInChildren<Transform>())
            {
                string n = t.name.ToLower();
                if (n.Contains("prop") || n.Contains("rotor"))
                    list.Add(t);
            }

            propellers = list.ToArray();

            if (propellers.Length == 0)
                Debug.LogWarning($"{nameof(DronePropellers)} on {name} found no child transforms with 'prop' or 'rotor' in name.");
        }

        // Try to grab AudioSource if not set
        if (propellerAudio == null)
            propellerAudio = GetComponent<AudioSource>();

        if (propellerAudio != null)
        {
            propellerAudio.loop = true;
            if (!propellerAudio.isPlaying)
                propellerAudio.Play();
        }
        else
        {
            Debug.LogWarning($"{nameof(DronePropellers)} on {name} has no AudioSource assigned.");
        }
    }

    void Update()
    {
        if (propellers == null || propellers.Length == 0 || rb == null) return;

        // 1. Compute target RPM from current speed
        float speed = rb.linearVelocity.magnitude;      // use rb.velocity if needed
        float t = Mathf.Clamp01(speed / speedForMaxRpm);
        float targetRpm = idleRpm + t * maxExtraRpm;

        // 2. Smooth RPM
        currentRpm = Mathf.Lerp(currentRpm, targetRpm, Time.deltaTime * rpmSmooth);

        // 3. Rotate props
        float degreesPerSecond = currentRpm * 360f / 60f;
        float angle = degreesPerSecond * Time.deltaTime;

        foreach (var p in propellers)
        {
            if (p == null) continue;
            p.Rotate(localAxis, angle, Space.Self);
        }

        // 4. Update sound based on RPM
        if (propellerAudio != null)
        {
            float minRpm = idleRpm;
            float maxRpm = idleRpm + maxExtraRpm;

            // 0..1 value based on current RPM
            float rpm01 = Mathf.InverseLerp(minRpm, maxRpm, currentRpm);

            propellerAudio.pitch = Mathf.Lerp(minPitch, maxPitch, rpm01);
            propellerAudio.volume = Mathf.Lerp(minVolume, maxVolume, rpm01);
        }
    }
}
