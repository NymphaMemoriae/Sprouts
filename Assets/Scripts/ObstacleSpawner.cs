using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private List<ObstacleData> obstacleDataList;
    [SerializeField] private int poolSize = 30;

    [Tooltip("Horizontal range for obstacle spawn positions.")]
    [SerializeField] private float spawnXRange = 5f;

    [Tooltip("The vertical distance between each newly spawned obstacle.")]
    [SerializeField] private float spawnYIncrement = 10f;

    [Tooltip("Starting Y coordinate where the first obstacle is placed.")]
    [SerializeField] private float startYPosition = 0f;

    // Internal queue for the obstacle pool
    private Queue<GameObject> obstaclePool = new Queue<GameObject>();

    // Track the next vertical position for spawning
    private float currentSpawnY;

    private void Start()
    {
        currentSpawnY = startYPosition;

        // Initialize the pool by spawning up to poolSize obstacles
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewObstacle();
        }
    }

    private void Update()
    {
        // Example check: if the first obstacle in the queue is well below
        // the camera (or below some threshold), recycle it to the top.
        if (obstaclePool.Count > 0)
        {
            GameObject bottomObstacle = obstaclePool.Peek();
            if (bottomObstacle.transform.position.y < (Camera.main.transform.position.y - 30f))
            {
                RecycleObstacle();
            }
        }
    }

    /// <summary>
    /// Instantiates a new obstacle from a randomly selected ObstacleData
    /// and places it at currentSpawnY.
    /// </summary>
    private void CreateNewObstacle()
    {
        ObstacleData selectedData = WeightedRandomSelection();
        if (selectedData == null)
        {
            Debug.LogError("No obstacle data found. Please assign ScriptableObjects in the inspector.");
            return;
        }

        // Pick a random X within the spawn range, use currentSpawnY for vertical placement
        Vector3 spawnPos = new Vector3(Random.Range(-spawnXRange, spawnXRange), currentSpawnY, 0f);
        GameObject newObstacle = Instantiate(selectedData.prefab, spawnPos, Quaternion.identity);

        // Set up the DamageObstacle component with the selected data
        DamageObstacle damageObstacle = newObstacle.GetComponent<DamageObstacle>();
        if (damageObstacle != null)
        {
            damageObstacle.SetObstacleData(selectedData);
        }

        // Add it to our queue
        obstaclePool.Enqueue(newObstacle);

        // Update the spawn position for the next obstacle
        currentSpawnY += spawnYIncrement;
    }

    /// <summary>
    /// Removes the oldest obstacle from the pool, repositions it at the top,
    /// and reassigns data if desired.
    /// </summary>
    private void RecycleObstacle()
    {
        // Dequeue the bottom-most obstacle
        GameObject oldObstacle = obstaclePool.Dequeue();

        // Choose new data and place it at the top
        ObstacleData selectedData = WeightedRandomSelection();

        Vector3 newPos = new Vector3(Random.Range(-spawnXRange, spawnXRange), currentSpawnY, 0f);
        oldObstacle.transform.position = newPos;

        // Update the DamageObstacle component with new data
        DamageObstacle damageObstacle = oldObstacle.GetComponent<DamageObstacle>();
        if (damageObstacle != null)
        {
            damageObstacle.SetObstacleData(selectedData);
        }

        // Enqueue the recycled obstacle back into the pool
        obstaclePool.Enqueue(oldObstacle);

        // Move the spawner up for the next obstacle
        currentSpawnY += spawnYIncrement;
    }

    /// <summary>
    /// Returns a random obstacle data from obstacleDataList, weighted by spawnWeight.
    /// </summary>
    private ObstacleData WeightedRandomSelection()
    {
        if (obstacleDataList == null || obstacleDataList.Count == 0)
            return null;

        float totalWeight = 0f;
        foreach (ObstacleData data in obstacleDataList)
        {
            totalWeight += data.spawnWeight;
        }

        float randomValue = Random.value * totalWeight;
        float cumulative = 0f;

        foreach (ObstacleData data in obstacleDataList)
        {
            cumulative += data.spawnWeight;
            if (randomValue <= cumulative)
            {
                return data;
            }
        }

        // Fallback: return the last data if something goes wrong with the calculation
        return obstacleDataList[obstacleDataList.Count - 1];
    }
    
    // This method was previously outside the class and is now properly inside it
    public void SetBiomeObstacles(List<ObstacleData> newObstacles, float spawnRateMultiplier)
    {
        if (newObstacles == null || newObstacles.Count == 0)
        {
            Debug.LogWarning("Empty obstacle list provided to SetBiomeObstacles");
            return;
        }
        
        Debug.Log($"Updating obstacle list with {newObstacles.Count} obstacles, spawn rate multiplier: {spawnRateMultiplier}");
        
        // Update the obstacle list
        obstacleDataList = new List<ObstacleData>(newObstacles);
        
        // Adjust the spawn rate if your spawner has this property
        // For example: currentSpawnRate *= spawnRateMultiplier;
    }
}