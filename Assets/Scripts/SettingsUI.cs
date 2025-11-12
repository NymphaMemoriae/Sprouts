using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Audio UI")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Input UI")]
    [SerializeField] private Toggle joystickToggle;
    [SerializeField] private Toggle touchToggle;

    // We no longer need references to the input GameObjects here
    // We will also use the centralized keys from PlayerPrefsManager

    void Start()
    {
        // Load and apply settings to the *toggles only*
        LoadSettingsToUI();
    }
    
    void OnEnable()
    {
        // --- Audio Section ---
        if (AudioManager.Instance == null)
        {
            Debug.LogError("[SettingsUI] AudioManager instance not found!");
        }
        else
        {
            masterSlider.value = AudioManager.Instance.GetVolume(PlayerPrefsManager.MASTER_VOLUME_KEY, 1f);
            musicSlider.value = AudioManager.Instance.GetVolume(PlayerPrefsManager.MUSIC_VOLUME_KEY, 0.75f);
            sfxSlider.value = AudioManager.Instance.GetVolume(PlayerPrefsManager.SFX_VOLUME_KEY, 0.8f);
        }

        // --- Input Section ---
        // Sync toggles to the saved PlayerPrefs state
        LoadSettingsToUI();
    }

    /// <summary>
    /// Loads input settings from PlayerPrefs and applies them to the UI toggles.
    /// </summary>
    private void LoadSettingsToUI()
    {
        if (joystickToggle == null || touchToggle == null)
        {
            Debug.LogError("[SettingsUI] Input Toggles are not set!");
            return;
        }

        // Load saved settings. Default to Touch ON, Joystick ON
        bool joystickEnabled = PlayerPrefs.GetInt(PlayerPrefsManager.JOYSTICK_ENABLED_KEY, 1) == 1;
        bool touchEnabled = PlayerPrefs.GetInt(PlayerPrefsManager.TOUCH_ENABLED_KEY, 1) == 1;

        // Update toggle visuals to match
        joystickToggle.isOn = joystickEnabled;
        touchToggle.isOn = touchEnabled;
    }
    
    // --- Audio Public Methods ---
    public void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value, savePreference: true);
        }
    }

    public void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value, savePreference: true);
        }
    }

    public void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value, savePreference: true);
        }
    }

    // --- Input Public Methods ---

    public void OnJoystickToggleChanged(bool isOn)
    {
        // ONLY save the setting
        PlayerPrefs.SetInt(PlayerPrefsManager.JOYSTICK_ENABLED_KEY, isOn ? 1 : 0);
        
        // Enforce the rule: "can't both be off"
        if (!isOn && touchToggle != null && !touchToggle.isOn)
        {
            touchToggle.isOn = true; // This will automatically fire OnTouchToggleChanged
        }
    }

    public void OnTouchToggleChanged(bool isOn)
    {
        // ONLY save the setting
        PlayerPrefs.SetInt(PlayerPrefsManager.TOUCH_ENABLED_KEY, isOn ? 1 : 0);
        
        // Enforce the rule: "can't both be off"
        if (!isOn && joystickToggle != null && !joystickToggle.isOn)
        {
            joystickToggle.isOn = true; // This will automatically fire OnJoystickToggleChanged
        }
    }

    // --- Modified Reset Method ---
    public void HandleResetPlayerData()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetAllPlayerData();
        }

        // Reset input settings UI to default (both ON)
        if (joystickToggle != null) joystickToggle.isOn = true;
        if (touchToggle != null) touchToggle.isOn = true;
        
        // Manually save defaults to PlayerPrefs
        PlayerPrefs.SetInt(PlayerPrefsManager.JOYSTICK_ENABLED_KEY, 1);
        PlayerPrefs.SetInt(PlayerPrefsManager.TOUCH_ENABLED_KEY, 1);
    }
}