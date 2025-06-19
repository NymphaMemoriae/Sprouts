using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.UI; // Required for Button
using TMPro; // Optional: if you use TextMeshPro for button labels

public class StartingBiomeSelection : MonoBehaviour
{
    [System.Serializable]
    public struct LevelEntry
    {
        public string displayName;     // Name to show on the button
        public BiomeData biomeData;    // The BiomeData for this level
        public Button uiButton;        // Assign the UI Button from the Inspector
    }

    [Header("Level Setup")]
    public List<LevelEntry> selectableLevels = new List<LevelEntry>();
    public string gameSceneName = "GameScene"; // Ensure this matches your game scene's name

    [Header("Optional Default Start Button")]
    public Button defaultStartButton; // Assign if you have a "Start from Beginning" button

    void Start()
    {
        // First, ensure the very first biome is marked as unlocked if it's the default
        if (selectableLevels.Count > 0)
        {
            var firstLevel = selectableLevels[0];
            if (firstLevel.biomeData != null && firstLevel.biomeData.isUnlockedByDefault)
            {
                if (!PlayerPrefsManager.Instance.IsBiomeUnlocked(firstLevel.biomeData.biomeName))
                {
                     PlayerPrefsManager.Instance.UnlockBiome(firstLevel.biomeData.biomeName);
                }
            }
        }
        foreach (var levelEntry in selectableLevels)
        {
            if (levelEntry.uiButton != null && levelEntry.biomeData != null)
            {
                // Check if the biome is unlocked
                bool isUnlocked = PlayerPrefsManager.Instance.IsBiomeUnlocked(levelEntry.biomeData.biomeName);
                
                // The button is interactable only if the biome is unlocked
                levelEntry.uiButton.interactable = isUnlocked;
                
                // Capture the biomeData for the listener
                BiomeData currentBiomeData = levelEntry.biomeData;
                levelEntry.uiButton.onClick.AddListener(() => OnLevelSelected(currentBiomeData));

                // Optional: Set button text
                TextMeshProUGUI buttonText = levelEntry.uiButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null && !string.IsNullOrEmpty(levelEntry.displayName))
                {
                    buttonText.text = levelEntry.displayName;
                }
            }
            else
            {
                Debug.LogWarning($"[StartingBiomeSelection] A level entry is misconfigured (Button or BiomeData is null). DisplayName: '{levelEntry.displayName}'", this);
            }
        }

        if (defaultStartButton != null)
        {
            defaultStartButton.onClick.AddListener(OnDefaultStartSelected);
        }
    }

    void OnLevelSelected(BiomeData selectedBiome)
    {
        if (selectedBiome == null)
        {
            Debug.LogError("[StartingBiomeSelection] Selected biome is null!", this);
            return;
        }

        Debug.Log($"[StartingBiomeSelection] Level '{selectedBiome.biomeName}' selected.", this);
        GameManager.SetNextStartingLevel(selectedBiome); // Inform GameManager
        LoadGameScene();
    }

    public void OnDefaultStartSelected()
    {
        Debug.Log("[StartingBiomeSelection] Default start selected.", this);
        GameManager.SetNextStartingLevel(null); // Null signifies default start behavior
        LoadGameScene();
    }

    private void LoadGameScene()
    {
        if (GameManager.Instance != null)
        {
            Time.timeScale = 1f; // Ensure time scale is normal
            GameManager.Instance.SetGameState(GameState.Playing); // Set state before loading
            if (GameSceneLoader.Instance != null)
            {
                GameSceneLoader.Instance.LoadScene(gameSceneName); // Use the loader for fade
            }
            else
            {
                Debug.LogError("[StartingBiomeSelection] GameSceneLoader.Instance is null. Cannot start game with fade. Loading directly.", this);
                SceneManager.LoadScene(gameSceneName); // Fallback to direct load
            }
        }
        else
        {
            Debug.LogError("[StartingBiomeSelection] GameManager.Instance is null. Cannot start game.", this);
        }
    }
}