using UnityEngine;
using System.Collections.Generic;

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
    
    void Start()
    {
        // Find references if not assigned
        if (plantController == null)
            plantController = FindObjectOfType<PlantController>();
            
        if (backgroundTileManager == null)
            backgroundTileManager = FindObjectOfType<BackgroundTileManager>();
            
        if (obstacleSpawner == null)
            obstacleSpawner = FindObjectOfType<ObstacleSpawner>();
            
        // Initialize with default biome
        if (defaultBiome != null)
        {
            SetCurrentBiome(defaultBiome);
        }
        else if (biomes.Count > 0)
        {
            SetCurrentBiome(biomes[0]);
        }
    }
    
    void Update()
    {
        if (plantController == null) return;
        
        // Get player's current height
        displayHeight = plantController.DisplayHeight;
        
        // Check if we need to change biomes
        CheckBiomeTransition();
    }
    
    private void CheckBiomeTransition()
    {
        // Find the appropriate biome for the current height
        BiomeData targetBiome = FindBiomeForHeight(displayHeight);
        
        // If different from current biome, switch to it
        if (targetBiome != null && targetBiome != currentBiome)
        {
            SetCurrentBiome(targetBiome);
        }
    }
    
    private BiomeData FindBiomeForHeight(float height)
    {
        foreach (BiomeData biome in biomes)
        {
            if (height >= biome.minHeight && height <= biome.maxHeight)
            {
                return biome;
            }
        }
        
        // If no matching biome is found, return default
        return defaultBiome;
    }
    
    private void SetCurrentBiome(BiomeData biome)
    {
        currentBiome = biome;
        Debug.Log($"Transitioning to biome: {biome.biomeName} at height {displayHeight}m");
        
        // Update background tile manager with the new biome's tile prefab
        if (backgroundTileManager != null)
        {
            backgroundTileManager.SetBiomeTilePrefab(biome.tilePrefab);
        }
        
        // Update obstacle spawner with the new biome's obstacles
        if (obstacleSpawner != null)
        {
            obstacleSpawner.SetBiomeObstacles(biome.biomeObstacles, biome.obstacleSpawnRateMultiplier);
        }
    }
}