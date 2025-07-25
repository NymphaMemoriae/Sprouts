using UnityEngine;
using System.Collections.Generic; // For managing list of skin IDs if you go that route

public class PlayerPrefsManager : MonoBehaviour
{
    // Singleton instance
    private static PlayerPrefsManager _instance;

    // Public accessor for the instance
    public static PlayerPrefsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Look for an existing instance in the scene
                _instance = FindFirstObjectByType<PlayerPrefsManager>();

                // If no instance exists, create a new GameObject and add the script
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("PlayerPrefsManager");
                    _instance = singletonObject.AddComponent<PlayerPrefsManager>();
                }
            }
            return _instance;
        }
    }

    // Ensure the GameObject persists across scene loads
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // Makes the object persistent
        }
        else if (_instance != this)
        {
            // If another instance already exists, destroy this one
            Destroy(gameObject);
        }
    }

    // --- Keys for PlayerPrefs ---
    // It's good practice to use constants for keys to avoid typos.
    private const string MONEY_KEY = "PlayerMoney";
    private const string HIGHSCORE_KEY = "PlayerHighScore";
    // private const string MASTER_VOLUME_KEY = "Settings_MasterVolume";
    // private const string MUSIC_VOLUME_KEY = "Settings_MusicVolume";
    // private const string SFX_VOLUME_KEY = "Settings_SFXVolume";
    // Prefix for skin unlock status
    private const string SKIN_UNLOCKED_PREFIX = "SkinUnlocked_";
    private const string EQUIPPED_SOUNDTRACK_PREFIX = "EquippedSoundtrack_";
    private const string BIOME_UNLOCKED_PREFIX = "BiomeUnlocked_";
    // Centralized keys for volume settings
    public const string MASTER_VOLUME_KEY = "Settings_MasterVolume";
    public const string MUSIC_VOLUME_KEY = "Settings_MusicVolume";
    public const string SFX_VOLUME_KEY = "Settings_SFXVolume";

    // --- Default Values ---
    private const int DEFAULT_MONEY = 0;
    private const float DEFAULT_HIGHSCORE = 0f;
    // private const float DEFAULT_VOLUME = 0.75f;


    // --- Money ---
    public void SaveMoney(int amount)
    {
        PlayerPrefs.SetInt(MONEY_KEY, amount);
        PlayerPrefs.Save(); // Immediately writes to disk
        Debug.Log($"Saved Money: {amount}");
    }

    public int LoadMoney()
    {
        return PlayerPrefs.GetInt(MONEY_KEY, DEFAULT_MONEY);
    }

    // --- High Score ---
    // Your UIManager already has a LoadHighScore method. We can centralize it here.
    // Note: Your PlantController.DisplayHeight is a float.
    public void SaveHighScore(float score)
    {
        // Only save if the new score is higher
        if (score > LoadHighScore())
        {
            PlayerPrefs.SetFloat(HIGHSCORE_KEY, score);
            PlayerPrefs.Save();
            Debug.Log($"Saved New High Score: {score}");
        }
    }

    public float LoadHighScore()
    {
        return PlayerPrefs.GetFloat(HIGHSCORE_KEY, DEFAULT_HIGHSCORE);
    }

    // --- Skins ---
    // We'll save each skin's unlocked status individually.
    // skinID could be "PlantSkin_Fire", "PlantSkin_Ice", etc.
    public void SaveSkinUnlocked(string skinID, bool isUnlocked)
    {
        PlayerPrefs.SetInt(SKIN_UNLOCKED_PREFIX + skinID, isUnlocked ? 1 : 0); // Store bool as 0 or 1
        PlayerPrefs.Save();
        Debug.Log($"Saved Skin '{skinID}' Unlocked Status: {isUnlocked}");
    }

    public bool IsSkinUnlocked(string skinID)
    {
        // Default to false (0) if the skin key doesn't exist
        return PlayerPrefs.GetInt(SKIN_UNLOCKED_PREFIX + skinID, 0) == 1;
    }

    // --- Centralized Volume Settings ---
    public void SaveVolume(string key, float volumeLevel)
    {

        PlayerPrefs.SetFloat(key, Mathf.Clamp01(volumeLevel));
        PlayerPrefs.Save(); // Ensure data is written to disk immediately
         Debug.Log($"--- SAVING VOLUME TO PLAYERPREFS --- Key: {key}, Value: {volumeLevel}");
    }

    public float LoadVolume(string key, float defaultVolume)
    {
        return PlayerPrefs.GetFloat(key, defaultVolume);
    }

   // --- Soundtracks ---

/// <summary>
/// Saves the player's choice of soundtrack for a specific biome.
/// </summary>
/// <param name="biomeID">The ID of the biome (e.g., from BiomeData.biomeName).</param>
/// <param name="soundtrackID">The ID of the chosen soundtrack.</param>
    public void SetEquippedSoundtrackForBiome(string biomeID, string soundtrackID)
    {
        PlayerPrefs.SetString(EQUIPPED_SOUNDTRACK_PREFIX + biomeID, soundtrackID);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerPrefsManager] Set soundtrack for biome '{biomeID}' to '{soundtrackID}'.");
    }

    /// <summary>
    /// Retrieves the ID of the soundtrack the player chose for a specific biome.
    /// </summary>
    /// <param name="biomeID">The ID of the biome to check.</param>
    /// <returns>The chosen soundtrack ID, or null if none is set.</returns>
    public string GetEquippedSoundtrackForBiome(string biomeID)
    {
        // Returns the saved string, or an empty string if no key exists.
        string equippedID = PlayerPrefs.GetString(EQUIPPED_SOUNDTRACK_PREFIX + biomeID, string.Empty);
        return string.IsNullOrEmpty(equippedID) ? null : equippedID;
    }

     public void UnlockBiome(string biomeName)
    {
        PlayerPrefs.SetInt(BIOME_UNLOCKED_PREFIX + biomeName, 1);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerPrefsManager] Unlocked Biome: {biomeName}");
    }

    public bool IsBiomeUnlocked(string biomeName)
    {
        return PlayerPrefs.GetInt(BIOME_UNLOCKED_PREFIX + biomeName, 0) == 1;
    }
    // --- Utility ---
    public void DeleteAllPlayerPrefs()
    {
        // Use with caution! This will erase all saved data.
        // Good for testing or if you want to provide a "Reset Game" feature.
        PlayerPrefs.DeleteAll();
        Debug.LogWarning("All PlayerPrefs have been deleted!");
    }
}