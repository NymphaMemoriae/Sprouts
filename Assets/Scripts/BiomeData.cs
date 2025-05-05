// In BiomeData.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Biome", menuName = "Plant Game/Biome Data", order = 0)]
public class BiomeData : ScriptableObject
{
    [Header("Biome Information")]
    public string biomeName = "Default Biome";

    [Header("Biome Range (in tiles)")]
    public int minTileIndex = 0;
    public int maxTileIndex = 10; // inclusive

    [Header("Visual Settings")]
    public Material backgroundMaterial;
    public GameObject tilePrefab;

    [Header("Optional Transition Tile")]
    public GameObject transitionTilePrefab;

    [Header("Checkpoint")] // <-- New Section
    public GameObject checkpointPrefab; // <-- Add this line

    [Header("Obstacles")]
    public List<SideObstacleSegment> biomeSideSegments = new List<SideObstacleSegment>();

    [Header("Obstacle Clusters")]
    public List<GameObject> startingClusters;
    public List<GameObject> middleClusters;
    public List<GameObject> endingClusters;
    
    [Header("Special First Tile")]
    [Tooltip("Optional: If assigned, this prefab will be used for the very first tile of this biome and will NOT be recycled. The checkpoint will be its child.")]
    public GameObject firstTilePrefab = null; // Prefab for the unique, non-recycled first tile

    [Header("Difficulty Settings")]
    [Range(0.5f, 2f)]
    public float obstacleSpawnRateMultiplier = 1f;

    [TextArea(3, 5)]
    public string description;
}