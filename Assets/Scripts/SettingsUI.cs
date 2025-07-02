using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Slider

/// <summary>
/// Manages the UI sliders for audio settings. It loads initial values from
/// the AudioManager when the panel is enabled and saves changes to PlayerPrefs
/// in real-time as the sliders are moved.
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The slider for controlling Master Volume.")]
    [SerializeField] private Slider masterSlider;
    [Tooltip("The slider for controlling Music Volume.")]
    [SerializeField] private Slider musicSlider;
    [Tooltip("The slider for controlling SFX Volume.")]
    [SerializeField] private Slider sfxSlider;

    /// <summary>
    /// OnEnable is called every time the settings panel GameObject is activated.
    /// This is the perfect place to load current settings and set up listeners.
    /// </summary>
    void OnEnable()
    {
        LoadAndSetupSliderValues();
    }

    /// <summary>
    /// Reads the current volume levels from the AudioManager and sets the sliders.
    /// It also ensures the sliders are correctly linked to update the audio settings.
    /// </summary>
    private void LoadAndSetupSliderValues()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogError("[SettingsUI] AudioManager instance not found! Cannot initialize settings UI.");
            return;
        }

        // --- Step 1: Remove old listeners to prevent them from firing while we set initial values ---
        masterSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();

        // --- Step 2: Set slider values from AudioManager's current state ---
        // GetVolume returns the linear value (0-1), which is perfect for sliders.
        masterSlider.value = AudioManager.Instance.GetVolume(AudioManager.MASTER_VOLUME_KEY, defaultLinearVal: 1f);
        musicSlider.value = AudioManager.Instance.GetVolume(AudioManager.MUSIC_VOLUME_KEY, defaultLinearVal: 0.75f);
        sfxSlider.value = AudioManager.Instance.GetVolume(AudioManager.SFX_VOLUME_KEY, defaultLinearVal: 0.8f);

        // --- Step 3: Add new listeners that will call the AudioManager ---
        // Now, the listeners will only fire on user interaction.
        masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    // These methods are called directly by the sliders' onValueChanged events.
    private void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            // The 'true' flag tells the AudioManager to save this new value to PlayerPrefs immediately.
            AudioManager.Instance.SetMasterVolume(value, savePreference: true);
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value, savePreference: true);
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value, savePreference: true);
        }
    }

    /// <summary>
    /// Public method to be linked to the OnClick event of a 'Reset Data' button.
    /// It calls the central reset logic in the GameManager.
    /// </summary>
    public void HandleResetPlayerData()
    {
        if (GameManager.Instance != null)
        {
            // It's good practice to show a confirmation dialog to the user before this call.
            Debug.LogWarning("[SettingsUI] Reset button clicked. Calling GameManager.ResetAllPlayerData().");
            GameManager.Instance.ResetAllPlayerData();
        }
        else
        {
            Debug.LogError("[SettingsUI] GameManager instance not found. Cannot reset player data.");
        }
    }

    /// <summary>
    // OnDisable is called when the panel is hidden. It's good practice to remove listeners
    // to prevent potential memory leaks or unintended behavior.
    /// </summary>
    void OnDisable()
    {
        masterSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();
    }
}