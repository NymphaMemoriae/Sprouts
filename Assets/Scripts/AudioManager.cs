using UnityEngine;
using UnityEngine.Audio; // Required for AudioMixer

public class AudioManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [Tooltip("Assign your main AudioMixer asset here.")]
    [SerializeField] private AudioMixer mainMixer;

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

    // PlayerPrefs keys for saving/loading settings (optional, but good for persistence)
    public const string MASTER_VOLUME_PREF = "MasterVolumePreference";
    public const string MUSIC_VOLUME_PREF  = "MusicVolumePreference";
    public const string SFX_VOLUME_PREF    = "SFXVolumePreference";

    public static AudioManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Makes AudioManager persist across scene loads
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate if one already exists
            return;
        }

        // Load saved volumes or apply inspector defaults
        LoadVolumeSettings();
    }

    /// <summary>
    /// Loads volume settings from PlayerPrefs or applies Inspector defaults if none are saved.
    /// </summary>
    public void LoadVolumeSettings()
    {
        float masterVol = PlayerPrefs.GetFloat(MASTER_VOLUME_PREF, defaultMasterVolume);
        float musicVol  = PlayerPrefs.GetFloat(MUSIC_VOLUME_PREF, defaultMusicVolume);
        float sfxVol    = PlayerPrefs.GetFloat(SFX_VOLUME_PREF, defaultSFXVolume);

        SetMasterVolume(masterVol, false); // Don't save when just loading
        SetMusicVolume(musicVol, false);
        SetSFXVolume(sfxVol, false);

        Debug.Log($"[AudioManager] Loaded volumes: Master={masterVol:F2}, Music={musicVol:F2}, SFX={sfxVol:F2}");
    }

    /// <summary>
    /// Saves the current volume settings to PlayerPrefs.
    /// </summary>
    public void SaveCurrentVolumeSettings()
    {
        // Retrieve current linear volumes from mixer (or store them if you update them from UI)
        float currentMaster = GetVolume(MASTER_VOLUME_KEY, defaultMasterVolume);
        float currentMusic = GetVolume(MUSIC_VOLUME_KEY, defaultMusicVolume);
        float currentSFX = GetVolume(SFX_VOLUME_KEY, defaultSFXVolume);

        PlayerPrefs.SetFloat(MASTER_VOLUME_PREF, currentMaster);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_PREF, currentMusic);
        PlayerPrefs.SetFloat(SFX_VOLUME_PREF, currentSFX);
        PlayerPrefs.Save();
        Debug.Log($"[AudioManager] Saved volumes: Master={currentMaster:F2}, Music={currentMusic:F2}, SFX={currentSFX:F2}");
    }

    // --- Public Methods to Set Volumes (callable from UI sliders, game events, etc.) ---

    public void SetMasterVolume(float linearVolume, bool savePreference = true)
    {
        SetMixerVolume(MASTER_VOLUME_KEY, linearVolume);
        if (savePreference) PlayerPrefs.SetFloat(MASTER_VOLUME_PREF, Mathf.Clamp(linearVolume, 0.0001f, 1f));
    }

    public void SetMusicVolume(float linearVolume, bool savePreference = true)
    {
        SetMixerVolume(MUSIC_VOLUME_KEY, linearVolume);
        if (savePreference) PlayerPrefs.SetFloat(MUSIC_VOLUME_PREF, Mathf.Clamp(linearVolume, 0.0001f, 1f));
    }

    public void SetSFXVolume(float linearVolume, bool savePreference = true)
    {
        SetMixerVolume(SFX_VOLUME_KEY, linearVolume);
        if (savePreference) PlayerPrefs.SetFloat(SFX_VOLUME_PREF, Mathf.Clamp(linearVolume, 0.0001f, 1f));
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