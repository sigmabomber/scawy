using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundEmitter : MonoBehaviour
{
    [Header("Sound Properties")]
    [SerializeField] private float baseSoundStrength = 1f;
    [SerializeField] private string soundTag = "generic";
    [SerializeField] private float minVolumeForAI = 0.1f;
    [SerializeField] private bool isPlayerSound = false; // Set to true for player sounds

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// Play a sound and notify AI
    /// </summary>
    public void PlaySound(AudioClip clip, float volumeScale = 1f, string customTag = "")
    {
        if (clip == null) return;

        // Calculate effective volume
        float effectiveVolume = audioSource.volume * volumeScale;
        float soundStrength = baseSoundStrength * effectiveVolume;

        // Only notify AI if sound is loud enough
        if (soundStrength >= minVolumeForAI && SoundManager.Instance != null)
        {
            // Play the sound
            audioSource.PlayOneShot(clip, volumeScale);

            // Notify AI about the sound
            SoundManager.Instance?.EmitSound(
                gameObject,
                transform.position,
                soundStrength,
                string.IsNullOrEmpty(customTag) ? soundTag : customTag,
                isPlayerSound
            );
        }
        else
        {
            // Play sound without AI notification
            audioSource.PlayOneShot(clip, volumeScale);
        }
    }

    /// <summary>
    /// Play sound without AI notification
    /// </summary>
    public void PlaySoundSilent(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip, volumeScale);
    }

    /// <summary>
    /// Emit a silent sound that only AI can detect
    /// </summary>
    public void EmitSilentSound(float customStrength = 1f, string customTag = "")
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance?.EmitSound(
                gameObject,
                transform.position,
                customStrength,
                string.IsNullOrEmpty(customTag) ? soundTag : customTag,
                isPlayerSound
            );
        }
    }

    /// <summary>
    /// Set sound properties for this emitter
    /// </summary>
    public void SetSoundProperties(float strength, string tag, bool playerSound = false)
    {
        baseSoundStrength = Mathf.Max(0.1f, strength);
        soundTag = tag;
        isPlayerSound = playerSound;
    }

    private void OnDestroy()
    {
        // Clean up from SoundManager cache
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.UnregisterAudioSource(gameObject);
        }
    }
}