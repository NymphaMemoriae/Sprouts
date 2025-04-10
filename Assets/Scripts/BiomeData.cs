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
    public GameObject tilePrefab;

    [Header("Obstacles")]
    //public List<ObstacleData> biomeObstacles = new List<ObstacleData>();
    public List<SideObstacleSegment> biomeSideSegments = new List<SideObstacleSegment>(); // NEW
    
    [Header("Obstacle Clusters")]
    public List<GameObject> startingClusters;
    public List<GameObject> middleClusters;
    public List<GameObject> endingClusters;

    [Header("Difficulty Settings")]
    [Range(0.5f, 2f)]
    public float obstacleSpawnRateMultiplier = 1f;

    [TextArea(3, 5)]
    public string description;
}
