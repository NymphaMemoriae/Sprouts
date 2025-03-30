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

    [Tooltip("The vertical distance between each newly spawned regular obstacle.")]
    [SerializeField] private float spawnYIncrement = 10f;

    [Tooltip("Starting Y coordinate where the first obstacle is placed.")]
    [SerializeField] private float startYPosition = 0f;

    [Header("Side Obstacle Settings")]
    [Tooltip("X position for obstacles on the left side.")]
    [SerializeField] private float leftSideX = -5.5f;

    [Tooltip("X position for obstacles on the right side.")]
    [SerializeField] private float rightSideX = 5.5f;

    [Tooltip("Percentage chance of spawning a side obstacle (standard ones).")]
    [Range(0f, 100f)]
    [SerializeField] private float sideObstacleChance = 30f;

    [Tooltip("Vertical spacing to apply when spawning a side obstacle.")]
    [SerializeField] private float sideObstacleYSpacing = 5f;

    [Header("Segment Tiling Settings")]
    [Tooltip("Chance (percentage) to spawn a tiling trunk-cap segment instead of normal side obstacle.")]
    [Range(0f, 100f)]
    [SerializeField] private float segmentTilingChance = 30f;

    private Dictionary<GameObject, List<GameObject>> obstaclePools = new Dictionary<GameObject, List<GameObject>>();
    private List<GameObject> activeObstacles = new List<GameObject>();
    private List<SideObstacleSegment> activeSegments = new List<SideObstacleSegment>();

    private float currentSpawnY;
    private SideObstacleSegment currentSegment;
    private int remainingTrunks = 0;
    private bool nextIsCap = false;

    private void Start()
    {
        currentSpawnY = startYPosition;
        InitializeObstaclePools();

        for (int i = 0; i < poolSize; i++)
        {
            SpawnObstacle();
        }
    }

    private void Update()
    {
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            GameObject obstacle = activeObstacles[i];
            if (obstacle == null)
            {
                activeObstacles.RemoveAt(i);
                continue;
            }

            if (obstacle.transform.position.y < (Camera.main.transform.position.y - 30f))
            {
                ReturnToPool(obstacle);
                activeObstacles.RemoveAt(i);
                SpawnObstacle();
            }
        }
    }

    private void InitializeObstaclePools()
    {
        obstaclePools.Clear();
        InitializePoolsForList(regularObstaclePrefabs);
        InitializePoolsForList(sideObstaclePrefabs);

        foreach (var segment in activeSegments)
        {
            InitializePool(segment.trunkPrefab);
            InitializePool(segment.capPrefab);
        }
    }

    private void InitializePoolsForList(List<GameObject> prefabList)
    {
        foreach (GameObject prefab in prefabList)
        {
            InitializePool(prefab);
        }
    }

    private void InitializePool(GameObject prefab)
    {
        if (prefab == null) return;

        if (!obstaclePools.ContainsKey(prefab))
        {
            obstaclePools[prefab] = new List<GameObject>();
        }

        for (int i = 0; i < 3; i++)
        {
            GameObject instance = Instantiate(prefab, transform);
            instance.SetActive(false);
            obstaclePools[prefab].Add(instance);
        }
    }

    private GameObject GetFromPool(GameObject prefab)
    {
        if (!obstaclePools.ContainsKey(prefab))
            InitializePool(prefab);

        foreach (GameObject obj in obstaclePools[prefab])
        {
            if (!obj.activeInHierarchy)
                return obj;
        }

        GameObject newInstance = Instantiate(prefab, transform);
        newInstance.SetActive(false);
        obstaclePools[prefab].Add(newInstance);
        return newInstance;
    }

    private void ReturnToPool(GameObject obstacle)
    {
        obstacle.SetActive(false);
        obstacle.transform.rotation = Quaternion.identity;
        obstacle.transform.position = Vector3.zero;
    }

    private void SpawnObstacle()
    {
        float randomValue = Random.Range(0f, 100f);

        if (randomValue < segmentTilingChance && activeSegments.Count > 0)
        {
            SpawnSegmentedSideObstacle();
            return;
        }

        if (randomValue < sideObstacleChance + segmentTilingChance && sideObstaclePrefabs.Count > 0)
        {
            GameObject sidePrefab = GetRandomPrefab(sideObstaclePrefabs);
            if (sidePrefab == null)
            {
                Debug.LogWarning("No side obstacle prefabs available");
                return;
            }

            GameObject obstacle = GetFromPool(sidePrefab);
            PositionAsSideObstacle(obstacle);
            obstacle.SetActive(true);
            activeObstacles.Add(obstacle);

            Debug.Log($"Spawned side obstacle {obstacle.name} from prefab {sidePrefab.name}");
        }
        else
        {
            GameObject regularPrefab = GetRandomPrefab(regularObstaclePrefabs);
            if (regularPrefab == null)
            {
                Debug.LogWarning("No regular obstacle prefabs available");
                return;
            }

            GameObject obstacle = GetFromPool(regularPrefab);
            PositionAsRegularObstacle(obstacle);
            obstacle.SetActive(true);
            activeObstacles.Add(obstacle);

            Debug.Log($"Spawned regular obstacle {obstacle.name} from prefab {regularPrefab.name}");
        }
    }

    private void SpawnSegmentedSideObstacle()
    {
        if (remainingTrunks <= 0 && !nextIsCap)
        {
            currentSegment = activeSegments[Random.Range(0, activeSegments.Count)];
            remainingTrunks = Random.Range(currentSegment.minTrunks, currentSegment.maxTrunks + 1);
            nextIsCap = false;
        }

        GameObject prefabToUse = null;

        if (remainingTrunks > 0)
        {
            prefabToUse = currentSegment.trunkPrefab;
            remainingTrunks--;
            if (remainingTrunks == 0)
                nextIsCap = true;
        }
        else if (nextIsCap)
        {
            prefabToUse = currentSegment.capPrefab;
            nextIsCap = false;
        }

        if (prefabToUse == null) return;

        GameObject obstacle = GetFromPool(prefabToUse);
        bool spawnLeft = Random.value < 0.5f;
        float xPos = spawnLeft ? currentSegment.leftX : currentSegment.rightX;

        float cameraTopY = Camera.main.transform.position.y + Camera.main.orthographicSize;
        float spawnY = Mathf.Max(currentSpawnY, cameraTopY + 2f);

        obstacle.transform.position = new Vector3(xPos, spawnY, 0);
        obstacle.transform.rotation = Quaternion.identity;
        obstacle.SetActive(true);
        activeObstacles.Add(obstacle);
        currentSpawnY = spawnY + sideObstacleYSpacing;

        Debug.Log($"[Segment] Spawned {(nextIsCap ? "cap" : "trunk")} from segment {currentSegment.name}");
    }

    private void PositionAsRegularObstacle(GameObject obstacle)
    {
        float xPos = Random.Range(-spawnXRange, spawnXRange);
        float cameraTopY = Camera.main.transform.position.y + Camera.main.orthographicSize;
        float spawnY = Mathf.Max(currentSpawnY, cameraTopY + 2f);
        obstacle.transform.position = new Vector3(xPos, spawnY, 0);
        currentSpawnY = spawnY + spawnYIncrement;
    }

    private void PositionAsSideObstacle(GameObject obstacle)
    {
        DamageObstacle damageObstacle = obstacle.GetComponent<DamageObstacle>();

        float minAngle = 20f;
        float maxAngle = 60f;

        if (damageObstacle != null && damageObstacle.obstacleData != null)
        {
            maxAngle = damageObstacle.obstacleData.maxRotationAngle;
        }

        bool spawnOnLeft = Random.Range(0f, 1f) < 0.5f;
        float xPos = spawnOnLeft ? leftSideX : rightSideX;
        float cameraTopY = Camera.main.transform.position.y + Camera.main.orthographicSize;
        float spawnY = Mathf.Max(currentSpawnY, cameraTopY + 2f);

        obstacle.transform.position = new Vector3(xPos, spawnY, 0);
        currentSpawnY = spawnY + sideObstacleYSpacing;

        float randomAngle = spawnOnLeft
            ? Random.Range(-maxAngle, -minAngle)
            : Random.Range(minAngle, maxAngle);

        obstacle.transform.rotation = Quaternion.Euler(0f, 0f, randomAngle);

        Debug.Log($"[Side] Positioned side obstacle {obstacle.name} at ({xPos}, {spawnY}) with rotation {randomAngle}");
    }

    private GameObject GetRandomPrefab(List<GameObject> prefabs)
    {
        if (prefabs == null || prefabs.Count == 0) return null;
        return prefabs[Random.Range(0, prefabs.Count)];
    }

    public void SetBiomeObstacles(List<ObstacleData> biomeObstacles, List<SideObstacleSegment> biomeSegments, float spawnRateMultiplier)
    {
        regularObstaclePrefabs.Clear();
        sideObstaclePrefabs.Clear();
        activeSegments.Clear();

        foreach (ObstacleData data in biomeObstacles)
        {
            if (data == null || data.prefab == null) continue;

            if (data.isSideObstacle)
                sideObstaclePrefabs.Add(data.prefab);
            else
                regularObstaclePrefabs.Add(data.prefab);
        }

        if (biomeSegments != null)
        {
            activeSegments.AddRange(biomeSegments);
        }

        InitializeObstaclePools();
    }
}
