using UnityEngine;

public class RiverAudioReshaper : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Audio Source attached to this river area")]
    public AudioSource riverAudioSource;

    [Header("Audio Settings")]
    [Tooltip("The volume of the river when you are standing inside the zone")]
    [Range(0f, 1f)] public float targetVolume = 1.0f;
    [Tooltip("How fast the sound fades in and out when entering/leaving the zone")]
    public float fadeSpeed = 2.0f;

    private bool isPlayerInsideZone = false;

    void Start()
    {
        if (riverAudioSource == null)
        {
            riverAudioSource = GetComponent<AudioSource>();
        }

        // Set up the audio source to behave as a controlled ambient loop
        if (riverAudioSource != null)
        {
            riverAudioSource.loop = true;
            riverAudioSource.spatialBlend = 0f; // 2D flat sound so the trigger handles the volume directly
            riverAudioSource.volume = 0f;
            if (!riverAudioSource.isPlaying) riverAudioSource.Play();
        }
    }

    void Update()
    {
        if (riverAudioSource == null) return;

        // Smoothly fade the volume up or down depending on the trigger state
        if (isPlayerInsideZone)
        {
            riverAudioSource.volume = Mathf.MoveTowards(riverAudioSource.volume, targetVolume, Time.deltaTime * fadeSpeed);
        }
        else
        {
            riverAudioSource.volume = Mathf.MoveTowards(riverAudioSource.volume, 0f, Time.deltaTime * fadeSpeed);
        }
    }

    // Automatically runs when an object with a Rigidbody + Collider sets foot inside
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering is your player character
        if (other.CompareTag("Player") || other.name.Contains("Player") || other.GetComponent<CharacterController>() != null)
        {
            isPlayerInsideZone = true;
        }
    }

    // Automatically runs when you step completely outside the zone
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.name.Contains("Player") || other.GetComponent<CharacterController>() != null)
        {
            isPlayerInsideZone = false;
        }
    }
}