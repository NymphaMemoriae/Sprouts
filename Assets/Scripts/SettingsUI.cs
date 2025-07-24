using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    // OnEnable is still the best place to set the initial values of the sliders.
    void OnEnable()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogError("[SettingsUI] AudioManager instance not found!");
            return;
        }

        // Set slider values from AudioManager's current state.
        // No need to manage listeners here anymore.
        masterSlider.value = AudioManager.Instance.GetVolume(AudioManager.MASTER_VOLUME_KEY, 1f);
        musicSlider.value = AudioManager.Instance.GetVolume(AudioManager.MUSIC_VOLUME_KEY, 0.75f);
        sfxSlider.value = AudioManager.Instance.GetVolume(AudioManager.SFX_VOLUME_KEY, 0.8f);
    }
    
    // --- Public Methods for Unity Inspector ---
    // These methods must be public to be assigned to the slider's OnValueChanged event.

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

    public void HandleResetPlayerData()
    {
        // This functionality remains the same.
        // NOTE: This will not reset volume unless you add that to your ResetAllPlayerData method.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetAllPlayerData();
        }
    }
}