using UnityEngine;
using UnityEngine.Audio; // Required for AudioMixer

public class AudioManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [Tooltip("Assign your main AudioMixer asset here.")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Audio Sources")]
    [Tooltip("This AudioSource is used for playing one-shot SFX like collisions.")]
    [SerializeField] private AudioSource sfxOneShotSource;

    [Header("Default Volume Levels (Linear: 0.0 to 1.0)")]
    [Tooltip("Default master volume. 0 is silent, 1 is full volume.")]
    [Range(0.0001f, 1f)]
    [SerializeField] private float defaultMasterVolume = 1f;

    [Tooltip("Default music volume. 0 is silent, 1 is full volume.")]
    [Range(0.0001f, 1f)]
    [SerializeField] private float defaultMusicVolume = 0.75f;

    [Tooltip("Default SFX volume. 0 is silent, 1 is full volume.")]
    [Range(0.0001f, 1f)]
    [SerializeField] private float defaultSFXVolume = 0.8f;

    // These const strings MUST match the names of your exposed parameters in the AudioMixer
    public const string MASTER_VOLUME_KEY = "MasterVolume";
    public const string MUSIC_VOLUME_KEY = "MusicVolume";
    public const string SFX_VOLUME_KEY = "SFXVolume";

   

   public static AudioManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        LoadVolumeSettings();
    }

    /// <summary>
    /// Loads volume settings from PlayerPrefsManager or applies Inspector defaults.
    /// </summary>
    public void LoadVolumeSettings()
    {
        // Delegate loading to the central PlayerPrefsManager
        float masterVol = PlayerPrefsManager.Instance.LoadVolume(PlayerPrefsManager.MASTER_VOLUME_KEY, defaultMasterVolume);
        float musicVol  = PlayerPrefsManager.Instance.LoadVolume(PlayerPrefsManager.MUSIC_VOLUME_KEY, defaultMusicVolume);
        float sfxVol    = PlayerPrefsManager.Instance.LoadVolume(PlayerPrefsManager.SFX_VOLUME_KEY, defaultSFXVolume);

        // Apply the loaded values to the mixer without re-saving them.
        SetMasterVolume(masterVol, false);
        SetMusicVolume(musicVol, false);
        SetSFXVolume(sfxVol, false);

        Debug.Log($"[AudioManager] Loaded volumes via PlayerPrefsManager: Master={masterVol:F2}, Music={musicVol:F2}, SFX={sfxVol:F2}");
    }

    

    /// <summary>
    /// Plays a one-shot sound effect through the dedicated SFX AudioSource.
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1.0f)
    {
        if (sfxOneShotSource == null || clip == null)
        {
            if (sfxOneShotSource == null) Debug.LogWarning("[AudioManager] SFX OneShot Source is not assigned!");
            return;
        }
        sfxOneShotSource.PlayOneShot(clip, volume);     
    }

     // --- Public Methods to Set Volumes ---
    // These methods now delegate the saving responsibility to the PlayerPrefsManager.

    public void SetMasterVolume(float linearVolume, bool savePreference = true)
    {
        SetMixerVolume(MASTER_VOLUME_KEY, linearVolume);
        if (savePreference)
        {
            PlayerPrefsManager.Instance.SaveVolume(PlayerPrefsManager.MASTER_VOLUME_KEY, linearVolume);
        }
    }

    public void SetMusicVolume(float linearVolume, bool savePreference = true)
    {
        SetMixerVolume(MUSIC_VOLUME_KEY, linearVolume);
        if (savePreference)
        {
            PlayerPrefsManager.Instance.SaveVolume(PlayerPrefsManager.MUSIC_VOLUME_KEY, linearVolume);
        }
    }

    public void SetSFXVolume(float linearVolume, bool savePreference = true)
    {
        SetMixerVolume(SFX_VOLUME_KEY, linearVolume);
        if (savePreference)
        {
            PlayerPrefsManager.Instance.SaveVolume(PlayerPrefsManager.SFX_VOLUME_KEY, linearVolume);
        }
    }

    private void SetMixerVolume(string parameterName, float linearVolume)
    {
        if (mainMixer == null)
        {
            Debug.LogError($"[AudioManager] MainMixer is not assigned! Cannot set {parameterName}.", this);
            return;
        }
        // AudioMixer volume is in decibels (dB). 0dB is full volume, -80dB is silence.
        // We need to convert our linear slider value (0 to 1) to dB.
        float clampedVolume = Mathf.Clamp(linearVolume, 0.0001f, 1f); // Clamp to avoid issues with log(0)
        mainMixer.SetFloat(parameterName, LinearToDecibels(clampedVolume));
    }

    /// <summary>
    /// Gets the current linear volume for a given mixer parameter.
    /// </summary>
    public float GetVolume(string parameterName, float defaultLinearVal)
    {
        if (mainMixer != null && mainMixer.GetFloat(parameterName, out float decibelValue))
        {
            return DecibelsToLinear(decibelValue);
        }
        return defaultLinearVal; // Fallback if mixer or parameter not found
    }


    // --- Conversion Helper Methods ---

    /// <summary>
    /// Converts a linear volume scale (0.0 to 1.0) to decibels.
    /// </summary>
    public static float LinearToDecibels(float linear)
    {
        if (linear <= 0.0001f) // Treat values at or below 0.0001f as silent
            return -80.0f; // AudioMixer's minimum is -80dB
        return Mathf.Log10(linear) * 20.0f;
    }

    /// <summary>
    /// Converts a decibel value to a linear scale (0.0 to 1.0).
    /// </summary>
    public static float DecibelsToLinear(float decibels)
    {
        if (decibels <= -80.0f) // Treat -80dB or less as silent
            return 0.0f;
        return Mathf.Pow(10.0f, decibels / 20.0f);
    }
}