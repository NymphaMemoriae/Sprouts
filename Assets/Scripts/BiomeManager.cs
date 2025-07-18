using UnityEngine;
using System.Collections.Generic;
using System;

public class BiomeManager : MonoBehaviour
{
    [Header("Biome Settings")]
    [SerializeField] private List<BiomeData> biomes = new List<BiomeData>();
    [SerializeField] private BiomeData defaultBiome;

    [Header("References")]
    [SerializeField] private PlantController plantController;
    [SerializeField] private BackgroundTileManager backgroundTileManager;
    [SerializeField] private ObstacleSpawner obstacleSpawner;

    private BiomeData currentBiome;
    private float displayHeight = 0f;
    public event Action<BiomeData, bool> OnBiomeTransitionComplete; // Sends BiomeData and a flag indicating if it's the very first biome
    
    [Header("Coin Settings")]
    [Tooltip("Coins awarded when entering a new biome for the first time in a run.")]
    [SerializeField] private int coinsPerBiomeTransition = 25;
    

    // ✅ Public access to current biome
    public BiomeData CurrentBiome => currentBiome;

    void Start()
    {

        if (plantController == null)
            plantController = FindFirstObjectByType<PlantController>();

        if (backgroundTileManager == null)
            backgroundTileManager = FindFirstObjectByType<BackgroundTileManager>();

        if (obstacleSpawner == null)
            obstacleSpawner = FindFirstObjectByType<ObstacleSpawner>();

        BiomeData initialBiomeToSet = null;
        // Check GameManager for a pre-selected starting biome
        if (GameManager.SelectedStartBiomeForNextRun != null)
        {
            initialBiomeToSet = GameManager.SelectedStartBiomeForNextRun;
            Debug.Log($"[BiomeManager] Starting with pre-selected biome from GameManager: {initialBiomeToSet.biomeName}");
        }
        else if (defaultBiome != null) // Fallback to default biome if no level selected
        {
            initialBiomeToSet = defaultBiome;
        }
        else if (biomes.Count > 0) // Fallback to the first in the list if no default
        {
            initialBiomeToSet = biomes[0];
        }

        if (initialBiomeToSet != null)
        {
            // The plant's height is set by GameManager.OnSceneLoaded using GetRespawnPosition() *before*
            // BiomeManager.Start() typically runs.
            // So, when SetCurrentBiome is called, plantController.DisplayHeight should be appropriate.
            SetCurrentBiome(initialBiomeToSet);
        }
        else
        {
            Debug.LogError("[BiomeManager] No biome could be determined to start with!");
        }
    }

    void Update()
    {
        if (plantController == null) return;

        displayHeight = plantController.DisplayHeight;
        CheckBiomeTransition();
    }

    private void CheckBiomeTransition()
    {
        // ✅ Calculate tile index from height (20m per tile)
        int currentTileIndex = Mathf.FloorToInt((plantController.DisplayHeight + 500f) / 20f);
        BiomeData targetBiome = FindBiomeForTile(currentTileIndex);

        if (targetBiome != null && targetBiome != currentBiome)
        {
            SetCurrentBiome(targetBiome);
        }
    }

    private BiomeData FindBiomeForTile(int tileIndex)
    {
        foreach (BiomeData biome in biomes)
        {
            if (tileIndex >= biome.minTileIndex && tileIndex <= biome.maxTileIndex)
            {
                return biome;
            }
        }

        return defaultBiome;
    }

    private void SetCurrentBiome(BiomeData biome)
    {
        if (biome == null || biome == currentBiome) return; // Avoid redundant calls
         // Award coins for the transition, but not for the very first biome the player starts in
        if (currentBiome != null && plantController != null)
        {
            plantController.AddWorldCoins(coinsPerBiomeTransition);
        }

        Debug.Log($"Transitioning to biome: {biome.biomeName} at height {displayHeight}m");

        // If this biome has not been unlocked yet, unlock it now.
        if (!PlayerPrefsManager.Instance.IsBiomeUnlocked(biome.biomeName))
        {
            PlayerPrefsManager.Instance.UnlockBiome(biome.biomeName);
        }
        
        BiomeData previousBiome = currentBiome; // Store previous biome before overwriting

        currentBiome = biome;

        if (backgroundTileManager != null)
        {
            // ✅ Queue new prefab without replacing visible ones
            backgroundTileManager.QueueNextBiomeTilePrefab(biome.tilePrefab);
            
        }

        if (obstacleSpawner != null)
        {
            obstacleSpawner.SetBiomeClusters(
                biome.startingClusters,
                biome.middleClusters,
                biome.endingClusters,
                biome.biomeSideSegments,
                biome.obstacleSpawnRateMultiplier
            );

            if (biome.biomeSideSegments != null)
            {
                Debug.Log($"Sending {biome.biomeSideSegments.Count} tiling segments to spawner:");
                foreach (var segment in biome.biomeSideSegments)
                {
                    if (segment != null)
                    {
                        Debug.Log($"  - Segment: {segment.name} (trunk: {(segment.trunkPrefab ? segment.trunkPrefab.name : "null")}, cap: {(segment.capPrefab ? segment.capPrefab.name : "null")})");
                    }
                }
            }
        }
        // Determine if this is the very first biome being set
        bool isFirstBiomeOverall = (previousBiome == null && (biome == defaultBiome || (biomes.Count > 0 && biome == biomes[0])));

        // Invoke the event AFTER all other setup for the new biome is done
        OnBiomeTransitionComplete?.Invoke(currentBiome, isFirstBiomeOverall);
        Debug.Log($"[BiomeManager] OnBiomeTransitionComplete invoked for {biome.biomeName}. Is First Biome Overall: {isFirstBiomeOverall}");
    }
}
