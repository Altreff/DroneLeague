using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DroneCollisionSounds : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;       // NOT the looping prop sound
    public AudioClip[] collisionClips;    // different hit sounds

    [Header("Impact → Volume")]
    public float minImpactVelocity = 1f;  // below this = no sound
    public float maxImpactVelocity = 10f; // at/above this = max volume
    public float minVolume = 0.1f;
    public float maxVolume = 1.0f;

    [Header("Pitch Randomization")]
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;

    [Header("Spam Protection")]
    public float cooldown = 0.1f;         // seconds between sounds

    private Rigidbody rb;
    private float lastPlayTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Auto-grab AudioSource on same object if not set
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            Debug.LogWarning($"{nameof(DroneCollisionSounds)} on {name} has no AudioSource assigned.");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (audioSource == null || collisionClips == null || collisionClips.Length == 0)
            return;

        // Avoid too many sounds in one frame
        if (Time.time < lastPlayTime + cooldown)
            return;

        // How strong was the hit?
        float impact = collision.relativeVelocity.magnitude;
        if (impact < minImpactVelocity)
            return; // too soft, ignore

        // 0..1 based on impact strength
        float t = Mathf.InverseLerp(minImpactVelocity, maxImpactVelocity, impact);
        float volume = Mathf.Lerp(minVolume, maxVolume, t);

        // Random pitch each time so it doesn't sound identical
        float pitch = Random.Range(minPitch, maxPitch);

        AudioClip clip = collisionClips[Random.Range(0, collisionClips.Length)];

        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, volume);

        lastPlayTime = Time.time;
    }
}
