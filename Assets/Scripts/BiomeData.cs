using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Biome", menuName = "Plant Game/Biome Data", order = 0)]
public class BiomeData : ScriptableObject
{
    [Header("Biome Information")]
    public string biomeName = "Default Biome";
    public float minHeight = 0f;
    public float maxHeight = 100f;
    
    [Header("Visual Settings")]
    public Material backgroundMaterial;
    [Tooltip("Reference tile prefab for this biome")]
    public GameObject tilePrefab;
    
    [Header("Obstacles")]
    [Tooltip("List of obstacles that can spawn in this biome")]
    public List<ObstacleData> biomeObstacles = new List<ObstacleData>();
    
    [Header("Difficulty Settings")]
    [Range(0.5f, 2f)]
    [Tooltip("Multiplier for obstacle spawn rate in this biome")]
    public float obstacleSpawnRateMultiplier = 1f;
    
    [TextArea(3, 5)]
    public string description;
}