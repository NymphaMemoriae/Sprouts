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
            plantController = Object.FindAnyObjectByType<PlantController>();

        if (backgroundTileManager == null)
            backgroundTileManager = Object.FindAnyObjectByType<BackgroundTileManager>();

        if (obstacleSpawner == null)
            obstacleSpawner = Object.FindAnyObjectByType<ObstacleSpawner>();

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
        if (biome == null) return;

        currentBiome = biome;
        Debug.Log($"Transitioning to biome: {biome.biomeName} at height {displayHeight}m");

        // Update background tile manager with the new biome's tile prefab
        if (backgroundTileManager != null)
        {
            backgroundTileManager.SetBiomeTilePrefab(biome.tilePrefab);
        }

        // Update obstacle spawner with the new biome's obstacles and segments
        if (obstacleSpawner != null)
        {
            obstacleSpawner.SetBiomeObstacles(
                biome.biomeObstacles,
                biome.biomeSideSegments,
                biome.obstacleSpawnRateMultiplier
            );

            if (biome.biomeObstacles != null)
            {
                Debug.Log($"Sending {biome.biomeObstacles.Count} obstacles to spawner:");
                foreach (var obstacleData in biome.biomeObstacles)
                {
                    if (obstacleData != null)
                    {
                        Debug.Log($"  - {obstacleData.name} (prefab: {(obstacleData.prefab ? obstacleData.prefab.name : "null")})");
                    }
                }
            }

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
    }
}
