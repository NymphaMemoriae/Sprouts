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

    [Tooltip("How far above the camera top to begin spawning side segment tiles.")]
    [SerializeField] private float segmentSpawnBuffer = 6f;


    private Dictionary<GameObject, List<GameObject>> obstaclePools = new Dictionary<GameObject, List<GameObject>>();
    private List<GameObject> activeObstacles = new List<GameObject>();
    private List<SideObstacleSegment> activeSegments = new List<SideObstacleSegment>();

    private float currentSpawnY;

    // New tiling system state (left/right)
    private SideSegmentState leftTiling = new SideSegmentState();
    private SideSegmentState rightTiling = new SideSegmentState();
    private float leftTilingY;
    private float rightTilingY;

    private void Start()
    {
        currentSpawnY = startYPosition;
        leftTilingY = startYPosition;
        rightTilingY = startYPosition;

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
            if (obstacle == null || obstacle.transform.position.y < (Camera.main.transform.position.y - 30f))
            {
                ReturnToPool(obstacle);
                activeObstacles.RemoveAt(i);
                SpawnObstacle();
            }
        }

       float cameraTop = Camera.main.transform.position.y + Camera.main.orthographicSize + segmentSpawnBuffer;

        const int maxTilesPerFrame = 20;

        for (int i = 0; i < maxTilesPerFrame && leftTilingY < cameraTop; i++)
            SpawnSegmentTile(ref leftTiling, ref leftTilingY, true);

        for (int i = 0; i < maxTilesPerFrame && rightTilingY < cameraTop; i++)
            SpawnSegmentTile(ref rightTiling, ref rightTilingY, false);
    }

    private void SpawnObstacle()
    {
        float roll = Random.Range(0f, 100f);

        if (roll < sideObstacleChance && sideObstaclePrefabs.Count > 0)
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

  private void SpawnSegmentTile(ref SideSegmentState state, ref float yPos, bool isLeft)
    {
        // Ensure a current segment is assigned
        if (state.current == null && activeSegments.Count > 0)
            state.current = activeSegments[Random.Range(0, activeSegments.Count)];
        if (state.current == null) return;

        // Spawn trunk
        GameObject trunkPrefab = state.current.trunkPrefab;
        if (trunkPrefab == null) return;

        GameObject trunk = GetFromPool(trunkPrefab);
        float x = isLeft ? state.current.leftX : state.current.rightX;
        trunk.transform.position = new Vector3(x, yPos, 0f);
        trunk.transform.rotation = Quaternion.identity;
        trunk.SetActive(true);
        activeObstacles.Add(trunk);

        // Cap spawning logic
        state.trunksSinceLastCap++;

        if (state.current.minCapInterval > 0 && state.current.maxCapInterval >= state.current.minCapInterval)
        {
            if (state.trunksSinceLastCap >= state.nextCapInterval)
            {
                GameObject capPrefab = state.current.capPrefab;
                if (capPrefab != null)
                {
                    GameObject cap = GetFromPool(capPrefab);
                    cap.transform.position = new Vector3(x, yPos, 0f);
                    cap.transform.rotation = Quaternion.identity;
                    cap.SetActive(true);
                    activeObstacles.Add(cap);

                    if (cap.TryGetComponent(out SpriteRenderer sr))
                        sr.sortingOrder = 5;
                }

                state.trunksSinceLastCap = 0;
                state.nextCapInterval = Random.Range(state.current.minCapInterval, state.current.maxCapInterval + 1);
            }
        }

        // Advance Y position for next trunk
        yPos += state.current.verticalSpacing;

        // Count down trunks left for this segment
        state.remainingTrunks--;
        if (state.remainingTrunks <= 0)
        {
            if (activeSegments.Count > 0)
            {
                SideObstacleSegment newSegment = activeSegments[Random.Range(0, activeSegments.Count)];
                if (newSegment != null)
                    state.current = newSegment;
            }

            state.remainingTrunks = Random.Range(state.current.minTrunks, state.current.maxTrunks + 1);
            state.trunksSinceLastCap = 0;
            state.nextCapInterval = Random.Range(state.current.minCapInterval, state.current.maxCapInterval + 1);
        }
    }



    private class SideSegmentState
    {
        public SideObstacleSegment current;
        public int remainingTrunks;
        public int trunksSinceLastCap;
        public int nextCapInterval;
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
            ? Random.Range(-maxAngle, -20f)
            : Random.Range(20f, maxAngle);

        obstacle.transform.rotation = Quaternion.Euler(0f, 0f, randomAngle);
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

        leftTilingY = rightTilingY = startYPosition;
        leftTiling = new SideSegmentState();
        rightTiling = new SideSegmentState();
    }
}
