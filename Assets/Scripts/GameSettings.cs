using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Plant Game/Game Settings", order = 0)]
public class GameSettings : ScriptableObject
{
    
    [Header("Default Obstacles")]
    public List<ObstacleData> defaultObstacles = new List<ObstacleData>();
    
    [Header("Difficulty")]
    public float initialSpawnRate = 0.7f;
    public float maxSpawnRate = 0.9f;
    public float difficultyScalingFactor = 0.0001f;
    
    [Header("Spawning")]
    public float spawnHeight = 10f;
    public float despawnHeight = 15f;
    public float horizontalSpawnRange = 5f;
    
}
