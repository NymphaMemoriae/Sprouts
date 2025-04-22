using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Cluster Spawner Settings")]
    [SerializeField] private List<GameObject> startingClusterPrefabs;
    [SerializeField] private List<GameObject> middleClusterPrefabs;
    [SerializeField] private List<GameObject> endingClusterPrefabs;

    // [Tooltip("Horizontal range for cluster spawn positions.")]
    // [SerializeField] private float spawnXRange = 5f;

    [Tooltip("Vertical spacing between each spawned cluster.")]
    [SerializeField] private float spawnYIncrement = 10f;

    [Tooltip("Starting Y position for spawning clusters.")]
    [SerializeField] private float startYPosition = 0f;

    [Header("Side Obstacle Tiling Settings")]
    [Tooltip("How far above the camera top to begin spawning side segment tiles.")]
    [SerializeField] private float segmentSpawnBuffer = 6f;

    private List<SideObstacleSegment> activeSegments = new List<SideObstacleSegment>();
    private float currentSpawnY;

    private SideSegmentState leftTiling = new SideSegmentState();
    private SideSegmentState rightTiling = new SideSegmentState();
    private float leftTilingY;
    private float rightTilingY;

    private bool hasSpawnedStart = false;
    private bool hasSpawnedEnd = false;

    private List<GameObject> activeClusters = new List<GameObject>();
    private Dictionary<GameObject, List<GameObject>> clusterPool = new Dictionary<GameObject, List<GameObject>>();

    private Dictionary<List<GameObject>, ClusterRotation> clusterRotationIndices = new Dictionary<List<GameObject>, ClusterRotation>();

    private class ClusterRotation
    {
        public List<GameObject> shuffledList = new List<GameObject>();
        public int currentIndex = 0;
    }

    private void Start()
    {
        currentSpawnY = startYPosition;
        leftTilingY = startYPosition;
        rightTilingY = startYPosition;

        InitializeSideObstaclePools();
    }

    private void Update()
    {
        float cameraTop = Camera.main.transform.position.y + Camera.main.orthographicSize + segmentSpawnBuffer;

        const int maxTilesPerFrame = 20;

        for (int i = 0; i < maxTilesPerFrame && leftTilingY < cameraTop; i++)
            SpawnSegmentTile(ref leftTiling, ref leftTilingY, true);

        for (int i = 0; i < maxTilesPerFrame && rightTilingY < cameraTop; i++)
            SpawnSegmentTile(ref rightTiling, ref rightTilingY, false);

        for (int i = activeClusters.Count - 1; i >= 0; i--)
        {
            GameObject cluster = activeClusters[i];
            if (cluster == null || cluster.transform.position.y < (Camera.main.transform.position.y - 30f))
            {
                ReturnClusterToPool(cluster);
                activeClusters.RemoveAt(i);
            }
        }

        if (!hasSpawnedStart && startingClusterPrefabs.Count > 0)
        {
            SpawnClusterFromList(startingClusterPrefabs);
            hasSpawnedStart = true;
        }

        if (!hasSpawnedEnd && endingClusterPrefabs.Count > 0 && currentSpawnY > 300f)
        {
            SpawnClusterFromList(endingClusterPrefabs);
            hasSpawnedEnd = true;
        }

        if (middleClusterPrefabs.Count > 0 && currentSpawnY < cameraTop)
        {
            SpawnClusterFromList(middleClusterPrefabs);
        }
    }

    public void SetBiomeClusters(
        List<GameObject> start,
        List<GameObject> middle,
        List<GameObject> end,
        List<SideObstacleSegment> biomeSegments,
        float spawnRateMultiplier)
    {
        startingClusterPrefabs = start ?? new List<GameObject>();
        middleClusterPrefabs = middle ?? new List<GameObject>();
        endingClusterPrefabs = end ?? new List<GameObject>();

        activeSegments.Clear();
        if (biomeSegments != null)
        {
            activeSegments.AddRange(biomeSegments);
        }

        leftTilingY = rightTilingY = startYPosition;
        leftTiling = new SideSegmentState();
        rightTiling = new SideSegmentState();

        hasSpawnedStart = false;
        hasSpawnedEnd = false;

        clusterRotationIndices.Clear(); // Reset rotations when switching biomes
    }

    private void SpawnClusterFromList(List<GameObject> list)
    {
        if (list == null || list.Count == 0) return;

        GameObject prefab = GetNextClusterInRotation(list);
        if (prefab == null) return;

        GameObject cluster = GetClusterFromPool(prefab);

        float x = 0f;
        float cameraTopY = Camera.main.transform.position.y + Camera.main.orthographicSize;
        float spawnY = Mathf.Max(currentSpawnY, cameraTopY + 2f);

        cluster.transform.position = new Vector3(x, spawnY, 0f);
        cluster.transform.rotation = Quaternion.identity;
        cluster.transform.SetParent(transform);
        cluster.SetActive(true);

        currentSpawnY = spawnY + spawnYIncrement;
        activeClusters.Add(cluster);
    }

    private GameObject GetNextClusterInRotation(List<GameObject> originalList)
    {
        if (!clusterRotationIndices.ContainsKey(originalList))
        {
            clusterRotationIndices[originalList] = new ClusterRotation();
        }

        ClusterRotation rotation = clusterRotationIndices[originalList];

        if (rotation.shuffledList.Count != originalList.Count)
        {
            rotation.shuffledList = new List<GameObject>(originalList);
            Shuffle(rotation.shuffledList);
            rotation.currentIndex = 0;
        }

        if (rotation.currentIndex >= rotation.shuffledList.Count)
        {
            Shuffle(rotation.shuffledList);
            rotation.currentIndex = 0;
        }

        return rotation.shuffledList[rotation.currentIndex++];
    }

    private void Shuffle(List<GameObject> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            GameObject temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private GameObject GetClusterFromPool(GameObject prefab)
    {
        if (!clusterPool.ContainsKey(prefab))
            clusterPool[prefab] = new List<GameObject>();

        foreach (var obj in clusterPool[prefab])
        {
            if (!obj.activeInHierarchy)
                return obj;
        }

        GameObject newInstance = Instantiate(prefab);
        newInstance.SetActive(false);
        clusterPool[prefab].Add(newInstance);
        return newInstance;
    }

    private void ReturnClusterToPool(GameObject cluster)
    {
        cluster.SetActive(false);
        cluster.transform.SetParent(transform);
    }

    private void InitializeSideObstaclePools()
    {
        foreach (var segment in activeSegments)
        {
            InitializePool(segment.trunkPrefab);
            InitializePool(segment.capPrefab);
        }
    }

    private void InitializePool(GameObject prefab)
    {
        if (prefab == null) return;
        for (int i = 0; i < 3; i++)
        {
            GameObject instance = Instantiate(prefab, transform);
            instance.SetActive(false);
        }
    }

    private void SpawnSegmentTile(ref SideSegmentState state, ref float yPos, bool isLeft)
    {
        if (state.current == null && activeSegments.Count > 0)
            state.current = activeSegments[Random.Range(0, activeSegments.Count)];
        if (state.current == null) return;

        GameObject trunkPrefab = state.current.trunkPrefab;
        if (trunkPrefab == null) return;

        GameObject trunk = Instantiate(trunkPrefab, transform);
        float x = isLeft ? state.current.leftX : state.current.rightX;
        trunk.transform.position = new Vector3(x, yPos, 0f);
        trunk.transform.rotation = Quaternion.identity;
        trunk.SetActive(true);

        state.trunksSinceLastCap++;

        if (state.current.minCapInterval > 0 && state.current.maxCapInterval >= state.current.minCapInterval)
        {
            if (state.trunksSinceLastCap >= state.nextCapInterval)
            {
                GameObject capPrefab = state.current.capPrefab;
                if (capPrefab != null)
                {
                    GameObject cap = Instantiate(capPrefab, transform);
                    cap.transform.position = new Vector3(x, yPos, 0f);
                    cap.transform.rotation = Quaternion.identity;
                    cap.SetActive(true);

                    if (cap.TryGetComponent(out SpriteRenderer sr))
                        sr.sortingOrder = 5;
                }

                state.trunksSinceLastCap = 0;
                state.nextCapInterval = Random.Range(state.current.minCapInterval, state.current.maxCapInterval + 1);
            }
        }

        yPos += state.current.verticalSpacing;

        state.remainingTrunks--;
        if (state.remainingTrunks <= 0)
        {
            if (activeSegments.Count > 0)
                state.current = activeSegments[Random.Range(0, activeSegments.Count)];

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
}
