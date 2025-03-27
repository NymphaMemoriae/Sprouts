using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private List<GameObject> regularObstaclePrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> sideObstaclePrefabs = new List<GameObject>();
    [SerializeField] private int poolSize = 30;

    [Tooltip("Horizontal range for regular obstacle spawn positions.")]
    [SerializeField] private float spawnXRange = 5f;

    [Tooltip("The vertical distance between each newly spawned obstacle.")]
    [SerializeField] private float spawnYIncrement = 10f;

    [Tooltip("Starting Y coordinate where the first obstacle is placed.")]
    [SerializeField] private float startYPosition = 0f;
    
    [Header("Side Obstacle Settings")]
    [Tooltip("X position for obstacles on the left side.")]
    [SerializeField] private float leftSideX = -5.5f;
    
    [Tooltip("X position for obstacles on the right side.")]
    [SerializeField] private float rightSideX = 5.5f;
    
    [Tooltip("Percentage chance of spawning a side obstacle.")]
    [Range(0f, 100f)]
    [SerializeField] private float sideObstacleChance = 30f;

    // Dictionary to track pools of different obstacle types
    private Dictionary<GameObject, List<GameObject>> obstaclePools = new Dictionary<GameObject, List<GameObject>>();
    
    // All active obstacles
    private List<GameObject> activeObstacles = new List<GameObject>();
    
    // Track the next vertical position for spawning
    private float currentSpawnY;

    private void Start()
    {
        currentSpawnY = startYPosition;

        // Initialize pools for each prefab
        InitializeObstaclePools();
        
        // Spawn initial obstacles
        for (int i = 0; i < poolSize; i++)
        {
            SpawnObstacle();
        }
    }

    private void Update()
    {
        // Clean up the list while iterating backwards
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            GameObject obstacle = activeObstacles[i];
            if (obstacle == null)
            {
                activeObstacles.RemoveAt(i);
                continue;
            }
            
            // Check if obstacle is below camera view
            if (obstacle.transform.position.y < (Camera.main.transform.position.y - 30f))
            {
                ReturnToPool(obstacle);
                activeObstacles.RemoveAt(i);
                SpawnObstacle(); // Spawn a new one at the top
            }
        }
    }
    
    // Initialize all obstacle pools
    private void InitializeObstaclePools()
    {
        obstaclePools.Clear();
        
        // Create initial pools for regular prefabs
        InitializePoolsForList(regularObstaclePrefabs);
        
        // Create initial pools for side prefabs
        InitializePoolsForList(sideObstaclePrefabs);
        
        Debug.Log($"Initialized pools: {regularObstaclePrefabs.Count} regular and {sideObstaclePrefabs.Count} side obstacle types");
    }
    
    private void InitializePoolsForList(List<GameObject> prefabList)
    {
        foreach (GameObject prefab in prefabList)
        {
            if (prefab == null) continue;
            
            List<GameObject> pool = new List<GameObject>();
            obstaclePools[prefab] = pool;
            
            // Create a few instances of each prefab to start with
            for (int i = 0; i < 3; i++)
            {
                GameObject instance = Instantiate(prefab, transform);
                instance.SetActive(false);
                pool.Add(instance);
            }
        }
    }
    
    // Get an obstacle from the pool, or create a new one if needed
    private GameObject GetFromPool(GameObject prefab)
    {
        if (prefab == null) return null;
        
        // Ensure a pool exists for this prefab
        if (!obstaclePools.ContainsKey(prefab))
        {
            obstaclePools[prefab] = new List<GameObject>();
        }
        
        List<GameObject> pool = obstaclePools[prefab];
        
        // Look for an inactive object in the pool
        foreach (GameObject obj in pool)
        {
            if (obj != null && !obj.activeInHierarchy)
            {
                return obj;
            }
        }
        
        // No inactive objects found, create a new one
        GameObject newInstance = Instantiate(prefab, transform);
        newInstance.SetActive(false);
        pool.Add(newInstance);
        
        Debug.Log($"Created new instance of {prefab.name}, pool size now: {pool.Count}");
        
        return newInstance;
    }
    
    // Return an obstacle to its pool
    private void ReturnToPool(GameObject obstacle)
    {
        obstacle.SetActive(false);
        
        // Reset transform
        obstacle.transform.rotation = Quaternion.identity;
        obstacle.transform.position = Vector3.zero;
    }
    
    // Spawn an obstacle
    private void SpawnObstacle()
    {
        // Decide whether to spawn a side obstacle
        bool spawnSideObstacle = Random.Range(0f, 100f) < sideObstacleChance && sideObstaclePrefabs.Count > 0;
        
        GameObject obstacle;
        
        if (spawnSideObstacle)
        {
            // Choose a random side obstacle prefab
            GameObject sidePrefab = GetRandomPrefab(sideObstaclePrefabs);
            if (sidePrefab == null)
            {
                Debug.LogWarning("No side obstacle prefabs available");
                return;
            }
            
            // Get from pool and position as side obstacle
            obstacle = GetFromPool(sidePrefab);
            PositionAsSideObstacle(obstacle);
            
            Debug.Log($"Spawned side obstacle {obstacle.name} from prefab {sidePrefab.name}");
        }
        else
        {
            // Choose a random regular obstacle prefab
            GameObject regularPrefab = GetRandomPrefab(regularObstaclePrefabs);
            if (regularPrefab == null)
            {
                Debug.LogWarning("No regular obstacle prefabs available");
                return;
            }
            
            // Get from pool and position as regular obstacle
            obstacle = GetFromPool(regularPrefab);
            PositionAsRegularObstacle(obstacle);
            
            Debug.Log($"Spawned regular obstacle {obstacle.name} from prefab {regularPrefab.name}");
        }
        
        // Activate and track the obstacle
        if (obstacle != null)
        {
            obstacle.SetActive(true);
            activeObstacles.Add(obstacle);
            
            // Increment the spawn position
            currentSpawnY += spawnYIncrement;
        }
    }
    
    // Position an obstacle at the side of the screen
   // Inside ObstacleSpawner.cs
private void PositionAsSideObstacle(GameObject obstacle)
{
    DamageObstacle damageObstacle = obstacle.GetComponent<DamageObstacle>();

    // Default angle range
    float minAngle = 20f;
    float maxAngle = 60f;

    if (damageObstacle != null && damageObstacle.obstacleData != null)
    {
        maxAngle = damageObstacle.obstacleData.maxRotationAngle;
        // Optional: You can define a min angle in ObstacleData too if needed
    }

    bool spawnOnLeft = Random.Range(0f, 1f) < 0.5f;

    float xPos = spawnOnLeft ? leftSideX : rightSideX;
    obstacle.transform.position = new Vector3(xPos, currentSpawnY, 0);

    float randomAngle;
    if (spawnOnLeft)
    {
        // LEFT: -60 to -20
        randomAngle = Random.Range(-maxAngle, -minAngle);
    }
    else
    {
        // RIGHT: +20 to +60
        randomAngle = Random.Range(minAngle, maxAngle);
    }

    obstacle.transform.rotation = Quaternion.identity;
    obstacle.transform.rotation = Quaternion.Euler(0f, 0f, randomAngle);

    Debug.Log($"[Fixed Range] Positioned side obstacle {obstacle.name} at ({xPos}, {currentSpawnY}) with rotation {randomAngle} (side: {(spawnOnLeft ? "Left" : "Right")})");
}


    
    // Position an obstacle in the regular play area
    private void PositionAsRegularObstacle(GameObject obstacle)
    {
        float xPos = Random.Range(-spawnXRange, spawnXRange);
        obstacle.transform.position = new Vector3(xPos, currentSpawnY, 0);
        
        Debug.Log($"Positioned regular obstacle {obstacle.name} at ({xPos}, {currentSpawnY})");
    }
    
    // Get a random prefab from a list, weighted by spawn weights
    private GameObject GetRandomPrefab(List<GameObject> prefabs)
    {
        if (prefabs.Count == 0) return null;
        
        // Simple random selection if just one prefab
        if (prefabs.Count == 1) return prefabs[0];
        
        // Weighted random selection
        float totalWeight = 0f;
        Dictionary<GameObject, float> weights = new Dictionary<GameObject, float>();
        
        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null) continue;
            
            // Get weight from obstacle data if available
            float weight = 1f; // Default weight
            DamageObstacle damageObstacle = prefab.GetComponent<DamageObstacle>();
            if (damageObstacle != null && damageObstacle.obstacleData != null)
            {
                weight = damageObstacle.obstacleData.spawnWeight;
            }
            
            weights[prefab] = weight;
            totalWeight += weight;
        }
        
        // Choose a prefab based on weights
        float randomValue = Random.Range(0f, totalWeight);
        float currentSum = 0f;
        
        foreach (var kvp in weights)
        {
            currentSum += kvp.Value;
            if (randomValue <= currentSum)
            {
                return kvp.Key;
            }
        }
        
        // Fallback
        return prefabs[0];
    }
    
    // Set the obstacle prefabs from biome data
    public void SetBiomeObstacles(List<ObstacleData> biomeObstacles, float spawnRateMultiplier)
    {
        if (biomeObstacles == null || biomeObstacles.Count == 0)
        {
            Debug.LogWarning("Empty obstacle list provided to SetBiomeObstacles");
            return;
        }
        
        // Clear existing prefab lists
        regularObstaclePrefabs.Clear();
        sideObstaclePrefabs.Clear();
        
        // Categorize prefabs from obstacle data
        foreach (ObstacleData data in biomeObstacles)
        {
            if (data == null || data.prefab == null) continue;
            
            // Check the prefab's configuration to determine category
            if (data.isSideObstacle)
            {
                sideObstaclePrefabs.Add(data.prefab);
                Debug.Log($"Added SIDE obstacle: {data.prefab.name}");
            }
            else
            {
                regularObstaclePrefabs.Add(data.prefab);
                Debug.Log($"Added REGULAR obstacle: {data.prefab.name}");
            }
        }
        
        // Initialize pools for any new prefabs
        InitializePoolsForList(regularObstaclePrefabs);
        InitializePoolsForList(sideObstaclePrefabs);
        
        Debug.Log($"Updated obstacle lists: {regularObstaclePrefabs.Count} regular, {sideObstaclePrefabs.Count} side");
    }
}