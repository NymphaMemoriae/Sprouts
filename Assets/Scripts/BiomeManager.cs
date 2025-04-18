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

    [Header("Transition Settings")]
    [SerializeField] private float biomeTransitionBuffer = 10f; // preload biome early

    private BiomeData currentBiome;
    private float displayHeight = 0f;

    void Start()
    {
        if (plantController == null)
            plantController = Object.FindAnyObjectByType<PlantController>();

        if (backgroundTileManager == null)
            backgroundTileManager = Object.FindAnyObjectByType<BackgroundTileManager>();

        if (obstacleSpawner == null)
            obstacleSpawner = Object.FindAnyObjectByType<ObstacleSpawner>();

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

        displayHeight = plantController.DisplayHeight;
        CheckBiomeTransition();
    }

    private void CheckBiomeTransition()
    {
        float previewHeight = plantController.DisplayHeight + biomeTransitionBuffer;

        BiomeData targetBiome = FindBiomeForHeight(previewHeight);

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

        return defaultBiome;
    }

    private void SetCurrentBiome(BiomeData biome)
    {
        if (biome == null) return;

        Debug.Log($"Transitioning to biome: {biome.biomeName} at height {displayHeight}m");

        // Insert transition tile if defined on current biome
        if (currentBiome != null && currentBiome.transitionTilePrefab != null && backgroundTileManager != null)
        {
            backgroundTileManager.InsertTransitionTile(currentBiome.transitionTilePrefab);
        }

        currentBiome = biome;

        if (backgroundTileManager != null)
        {
            backgroundTileManager.SetBiomeTilePrefab(biome.tilePrefab);
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
    }
}
