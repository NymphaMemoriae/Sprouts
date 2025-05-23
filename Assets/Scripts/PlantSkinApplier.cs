using UnityEngine;
using UnityEngine.UI; // Required if you have Image component for plant head

public class PlantSkinApplier : MonoBehaviour
{
    [Header("Component References (Assign in Inspector)")]
    [Tooltip("SpriteRenderer or Image component for the plant head.")]
    public Image plantHeadVisualComponent; // Can be SpriteRenderer or Image
    public Animator plantHeadAnimator;
    public PlantLife plantLife;
    public PlantController plantController;
    public ProceduralStem proceduralStem;
    public LeafSpawner leafSpawner;

    private const string SelectedSkinPlayerPrefsKey = "CurrentPlantType"; // Using your specified key name

    /// <summary>
    /// Sets the selected plant skin type name to PlayerPrefs and applies it.
    /// Call this from your UI buttons in the Main Menu.
    /// </summary>

    public void SetPlantType(string plantTypeName)
    {
        PlayerPrefs.SetString(SelectedSkinPlayerPrefsKey, plantTypeName);
        PlayerPrefs.Save();
        Debug.Log($"[PlantSkinApplier] PlantType '{plantTypeName}' saved to PlayerPrefs.");

        // Apply the skin immediately (useful for menu previews)
        LoadAndApplySkin(plantTypeName);
    }

    /// <summary>
    /// Loads the skin name from PlayerPrefs and applies the skin.
    /// Typically called on Start/Awake.
    /// </summary>
    private void ApplySkinFromPrefs()
    {
        // Use "DefaultSkin" as a fallback if no preference is set.
        // Ensure you have a "DefaultSkin.asset" PlantSkinData in "Resources/PlantSkins/".
        string skinNameFromPrefs = PlayerPrefs.GetString(SelectedSkinPlayerPrefsKey, "DefaultSkin");
        LoadAndApplySkin(skinNameFromPrefs);
    }

    void Start()
    {
        // Apply the skin when the game starts or the preview object is initialized.
        ApplySkinFromPrefs();
    }

    /// <summary>
    /// Loads the specified PlantSkinData from Resources and applies its properties.
    /// </summary>
    public void LoadAndApplySkin(string skinName)
    {
        string resourcePath = "PlantSkins/" + skinName;
        PlantSkinData skinData = Resources.Load<PlantSkinData>(resourcePath);

        // --- Direct Application - No If Checks as Requested ---
        // This section assumes skinData is NOT null and all component references ARE assigned.
        // If skinData is null (e.g., skinName not found), NullReferenceExceptions will occur here.
        // If component references are null, NullReferenceExceptions will also occur.

        // Apply Plant Head Visuals
       
        plantHeadVisualComponent.sprite = skinData.plantHeadSprite;
        
        plantHeadAnimator.runtimeAnimatorController = skinData.plantHeadAnimatorController;

        // Apply Gameplay Stats
        plantLife.SetStartingLives(skinData.startingLives); // Method to be added to PlantLife
        plantController.SetMaxVelocity(skinData.maxVelocity); // Method to be added to PlantController

        // Apply Stem Visuals
        proceduralStem.SetStemMaterial(skinData.stemMaterial); // Method to be added to ProceduralStem
        
        leafSpawner.leafPrefab = skinData.leafPrefab; // <-- Apply the new leaf prefab

        Debug.Log($"[PlantSkinApplier] Applied skin: {skinName}");
    }
}