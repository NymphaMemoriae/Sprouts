// PlantSkinApplier.cs (Refactored)
using UnityEngine;
using UnityEngine.UI;

public class PlantSkinApplier : MonoBehaviour
{
    [Header("Component References")]
    public Image plantHeadVisualComponent;
    public Animator plantHeadAnimator;
    public PlantLife plantLife;
    public PlantController plantController;
    public ProceduralStem proceduralStem;
    public LeafSpawner leafSpawner;

    private void OnEnable()
    {
        // Listen for when a skin is equipped from the shop.
        ShopManager.OnSkinEquipped += LoadAndApplySkin;
    }

    private void OnDisable()
    {
        // Unsubscribe from the event.
        ShopManager.OnSkinEquipped -= LoadAndApplySkin;
    }

    void Start()
    {
        // When the game scene starts, apply the skin saved in PlayerPrefs.
        string savedSkinName = PlayerPrefs.GetString("CurrentPlantType", "DefaultSkin");
        PlantSkinData skinData = Resources.Load<PlantSkinData>("PlantSkins/" + savedSkinName);
        if (skinData != null)
        {
            LoadAndApplySkin(skinData);
        }
    }

    /// <summary>
    /// Applies a PlantSkinData's properties to all relevant components.
    /// This is now called by events or on Start.
    /// </summary>
    public void LoadAndApplySkin(PlantSkinData skinData)
    {
        if (skinData == null)
        {
            Debug.LogError("[PlantSkinApplier] Received null skin data. Cannot apply skin.");
            return;
        }

        // Apply visuals and stats from the ScriptableObject
        try
        {
            plantLife.SetStartingLives(skinData.startingLives);
            plantHeadAnimator.runtimeAnimatorController = skinData.plantHeadAnimatorController;
            plantController.SetMaxVelocity(skinData.maxVelocity);
            proceduralStem.SetStemMaterial(skinData.stemMaterial);
            leafSpawner.leafPrefab = skinData.leafPrefab;
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[PlantSkinApplier] Error applying skin data: {ex.Message}");

        }
        
        plantHeadVisualComponent.sprite = skinData.plantHeadSprite;

        Debug.Log($"[PlantSkinApplier] Applied skin: {skinData.skinName}");
    }
}