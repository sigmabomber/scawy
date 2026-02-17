using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Volume Settings")]
    [SerializeField][Range(0f, 1f)] private float musicVolume = 0.7f;
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("Audio Source Pool")]
    [SerializeField] private int maxAudioSources = 20;
    private Dictionary<GameObject, AudioSource> objectAudioSources = new Dictionary<GameObject, AudioSource>();

    // Event that AI systems can subscribe to
    public event Action<SoundInfo> OnSoundPlayed;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSources()
    {
        // Create audio sources if not assigned
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;
    }

    #region Music Methods
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PauseMusic()
    {
        musicSource.Pause();
    }

    public void ResumeMusic()
    {
        musicSource.UnPause();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
    }
    #endregion

    #region Sound Effects Methods
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volumeScale);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
    }
    #endregion

    #region AI Sound System

    /// <summary>
    /// Emit a sound that AI can hear
    /// </summary>
    public void EmitSound(GameObject source, Vector3 position, float strength, string tag, bool isPlayerSound = false)
    {
        SoundInfo info = new SoundInfo
        {
            position = position,
            strength = Mathf.Clamp01(strength),
            soundTag = tag,
            timestamp = Time.time,
            source = source,
            isPlayerSound = isPlayerSound
        };

        OnSoundPlayed?.Invoke(info);
    }

    /// <summary>
    /// Emit player sound (convenience method)
    /// </summary>
    public void EmitPlayerSound(Vector3 position, float strength, string tag)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        EmitSound(player, position, strength, tag, true);
    }

    /// <summary>
    /// Emit sound at position without source
    /// </summary>
    public void EmitSoundAtPosition(Vector3 position, float strength, string tag)
    {
        SoundInfo info = new SoundInfo
        {
            position = position,
            strength = Mathf.Clamp01(strength),
            soundTag = tag,
            timestamp = Time.time,
            source = null,
            isPlayerSound = false
        };

        OnSoundPlayed?.Invoke(info);
    }

    #endregion

    #region Permission-Based Audio System
    public bool RequestPlayAudio(GameObject requester, AudioClip clip, float volumeScale = 1f,
        float spatialBlend = 1f, float minDistance = 1f, float maxDistance = 50f)
    {
        if (requester == null || clip == null)
        {
            Debug.LogWarning("SoundManager: Invalid request - requester or clip is null");
            return false;
        }

        if (objectAudioSources.Count >= maxAudioSources)
        {
            Debug.LogWarning($"SoundManager: Max audio sources ({maxAudioSources}) reached. Request denied.");
            return false;
        }

        AudioSource source = GetOrCreateAudioSource(requester);

        if (source == null)
        {
            Debug.LogError("SoundManager: Failed to create audio source");
            return false;
        }

        source.volume = volumeScale * sfxVolume;
        source.spatialBlend = spatialBlend;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.PlayOneShot(clip);

        return true;
    }

    private AudioSource GetOrCreateAudioSource(GameObject obj)
    {
        if (objectAudioSources.TryGetValue(obj, out AudioSource existingSource))
        {
            if (existingSource != null)
                return existingSource;
            else
                objectAudioSources.Remove(obj);
        }

        AudioSource source = obj.GetComponent<AudioSource>();

        if (source == null)
        {
            source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
        }

        objectAudioSources[obj] = source;

        return source;
    }

    public void UnregisterAudioSource(GameObject obj)
    {
        if (objectAudioSources.ContainsKey(obj))
        {
            objectAudioSources.Remove(obj);
        }
    }

    public void ClearAudioSourceCache()
    {
        objectAudioSources.Clear();
    }
    #endregion

    private void OnDestroy()
    {
        ClearAudioSourceCache();
    }
}

// In your SoundManager.cs file, update the SoundInfo struct:
[System.Serializable]
public struct SoundInfo
{
    public Vector3 position;      // Where the sound was made
    public float strength;        // How loud/detectable (0-1 or custom range)
    public string soundTag;       // Optional tag for categorizing sounds
    public float timestamp;       // When it occurred

    // NEW FIELDS:
    public GameObject source;     
    public bool isPlayerSound;   
}