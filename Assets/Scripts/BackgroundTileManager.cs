using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

/// <summary>
/// Spawns and recycles vertical background tiles.
/// Each prefab gets its own object‑pool queue, keyed by the prefab itself, so tiles from
/// one biome can never be reused in another biome.
/// </summary>
public class BackgroundTileManager : MonoBehaviour
{
    #region Inspector
    [Header("Tile Settings")]
    [SerializeField] private GameObject tilePrefab;        // initial / default prefab
    [SerializeField] private int       initialPoolSize = 3;
    [SerializeField] private float     bufferZone      = 10f;

    [Header("References")]
    [SerializeField] private Camera         mainCamera;
    [SerializeField] private Transform      playerHead;
    [SerializeField] private BiomeManager   biomeManager;
    #endregion

    #region Runtime fields
    private readonly Dictionary<GameObject, Queue<GameObject>> pools =
        new Dictionary<GameObject, Queue<GameObject>>();      // 👈 one queue per prefab

    private readonly List<GameObject> activeTiles = new();

    private float tileHeight;
    private float lastTileEndY;
    private TrailPainter trailPainter;

    private int  biomeTileCount;
    private bool transitionTileQueued;
    private GameObject queuedTilePrefab;
    private float currentZOffset = 0f;
    private const float ZIncrement = 0.1f;
    private BiomeData biomeForNextCheckpointSpawn = null;
    private bool isFirstBiomeEver = true; // Flag to track if we are still in the initial biome
    #endregion

    #region Unity lifecycle
    private void Start()
    {
        mainCamera   ??= Camera.main;
       playerHead   ??= FindObjectOfType<PlantController>()?.PlantHead; // Uses FindObjectOfType as corrected previously
        biomeManager ??= FindObjectOfType<BiomeManager>(); // Uses FindObjectOfType as corrected previously
        trailPainter  =  FindObjectOfType<TrailPainter>(); // Uses FindObjectOfType as corrected previously

        if (biomeManager != null)
        {
            biomeManager.OnBiomeTransitionComplete += HandleBiomeTransitionComplete;
            Debug.Log("[BackgroundTileManager] Subscribed to BiomeManager.OnBiomeTransitionComplete");
        }
        else
        {
            Debug.LogError("[BackgroundTileManager] BiomeManager reference is missing!");
        }
        currentZOffset = 0f; // Reset Z offset on start

        // Measure tile height via one temporary instance
        MeasureTileHeight();
        PreWarmPool(tilePrefab, initialPoolSize);
        SetupInitialTiles();
         isFirstBiomeEver = true;
    }

    private void Update() => ManageTiles();
    #endregion

    #region Pool helpers
    /// <summary> Returns (or creates) the queue for a given prefab. </summary>
    private Queue<GameObject> GetPool(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>();
            pools[prefab] = q;
        }
        return q;
    }

    /// <summary>Instantiate <paramref name="count"/> disabled objects and enqueue them.</summary>
    private void PreWarmPool(GameObject prefab, int count)
    {
        var pool = GetPool(prefab);
        for (int i = 0; i < count; ++i)
        {
            var go = CreateTile(prefab);
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }
    #endregion

    #region Tile creation & recycling
    /// <summary>Wraps Object.Instantiate and stamps the tile with its prefab of origin.</summary>
    private GameObject CreateTile(GameObject prefab)
    {
        var tile = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);

        // Stamp once, so we know where to return it later.
        var stamp = tile.GetComponent<TileStamp>() ?? tile.AddComponent<TileStamp>();
        stamp.SourcePrefab = prefab;

        // One‑time material clone & aspect‑ratio setup
        if (tile.TryGetComponent(out Image img) && img.material != null)
        {
            img.material = new Material(img.material);
            if (img.material.HasProperty("_AspectRatio") && tile.TryGetComponent(out RectTransform rt))
            {
                img.material.SetFloat("_AspectRatio", rt.rect.width / rt.rect.height);
            }
        }
        if (!tile.CompareTag("Ground")) tile.tag = "Ground";

        return tile;
    }
    #endregion

    #region Initialisation helpers
    private void MeasureTileHeight()
    {
        var temp = Instantiate(tilePrefab);
        if (!temp.TryGetComponent(out RectTransform rt))
        {
            Debug.LogError("Tile prefab must contain a RectTransform");
            Destroy(temp);
            return;
        }
        tileHeight = rt.rect.height;
        Destroy(temp);
    }

    private void SetupInitialTiles()
    {
        float cameraHeight = 2f * mainCamera.orthographicSize;
        int   tilesNeeded  = Mathf.CeilToInt((cameraHeight + 2 * bufferZone) / tileHeight);

        float startY = mainCamera.transform.position.y - mainCamera.orthographicSize - bufferZone;
        lastTileEndY = startY;

        for (int i = 0; i < tilesNeeded; ++i)
            SpawnNextTile();
    }
    #endregion

    #region Runtime tile management
    private void ManageTiles()
    {
        if (!playerHead) return;

        float camBottom = mainCamera.transform.position.y - mainCamera.orthographicSize - bufferZone;
        float camTop    = mainCamera.transform.position.y + mainCamera.orthographicSize + bufferZone;

        // Recycle off‑screen tiles
        for (int i = activeTiles.Count - 1; i >= 0; --i)
        {
            var tileRect = activeTiles[i].GetComponent<RectTransform>();
            tileRect.GetWorldCorners(s_corners);
            float tileTopY = s_corners[1].y;
            if (tileTopY < camBottom)
            {
                RecycleTile(activeTiles[i]);
                activeTiles.RemoveAt(i);
            }
        }

        // Spawn new ones on top
        if (activeTiles.Count > 0)
        {
            var topRect = activeTiles[^1].GetComponent<RectTransform>();
            topRect.GetWorldCorners(s_corners);
            // Use while loop to potentially spawn multiple tiles per frame if needed
            while (s_corners[1].y < camTop)
            {
                 lastTileEndY = s_corners[1].y;
                 SpawnNextTile();
                 // Update topRect for the loop condition after spawning
                 if (activeTiles.Count > 0) {
                     topRect = activeTiles[^1].GetComponent<RectTransform>();
                     topRect.GetWorldCorners(s_corners);
                 } else {
                     break; // Exit loop if activeTiles becomes empty
                 }
            }
        }
        else // If no tiles are active, restart setup
        {
            SetupInitialTiles();
        }
    }

    private void SpawnNextTile()
    {
        // Determine which prefab to use
        GameObject prefabToUse = tilePrefab; // Default fallback
        if (biomeManager?.CurrentBiome != null) // Check if BiomeManager and CurrentBiome are valid
        {
             prefabToUse = (transitionTileQueued && biomeManager.CurrentBiome.transitionTilePrefab != null)
                           ? biomeManager.CurrentBiome.transitionTilePrefab
                           : queuedTilePrefab ?? biomeManager.CurrentBiome.tilePrefab ?? tilePrefab; // Use queued, then current biome's, then default
        } else if (queuedTilePrefab != null) {
             prefabToUse = queuedTilePrefab; // Use queued if BiomeManager isn't ready but a queue exists
        }


        if (prefabToUse == null) {
             Debug.LogError("Could not determine a valid tile prefab to spawn!");
             return;
        }

        var pool = GetPool(prefabToUse);
        GameObject tile = pool.Count > 0 ? pool.Dequeue() : CreateTile(prefabToUse);
        if(tile == null) return; // Exit if tile creation failed

        // TrailPainter logic...
        if (trailPainter)
        {
            var rt = tile.GetComponent<RectTransform>();
            if (rt != null)
            {
                trailPainter.ClearTrailTexture(rt);
                trailPainter.AssignTextureToTile(tile);
            }
        }

        tile.SetActive(true);

        // Position the tile
        var rect = tile.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot     = new Vector2(0.5f, 0);

        float spawnY = lastTileEndY; // Place new tile directly above the previous one

        Vector3 spawnLocalPosition = transform.InverseTransformPoint(new Vector3(0, spawnY, 0));
        spawnLocalPosition.z = currentZOffset;

        rect.localPosition = spawnLocalPosition;
        // Debug.Log($"Spawned Tile: {tile.name} | LocalPos: {spawnLocalPosition.ToString("F3")} | WorldPos: {tile.transform.position.ToString("F3")}");

        currentZOffset += ZIncrement;

        activeTiles.Add(tile);
        ++biomeTileCount;

        // --- MODIFIED CHECKPOINT SPAWNING ---
        // Check if a checkpoint should spawn for this new tile
        if (biomeForNextCheckpointSpawn != null && biomeTileCount == 1)
        {
            Debug.Log($"[BackgroundTileManager] Attempting to spawn checkpoint for {biomeForNextCheckpointSpawn.biomeName} as child of {tile.name}");
            GameObject checkpointPrefab = biomeForNextCheckpointSpawn.checkpointPrefab;

            if (checkpointPrefab != null)
            {
                // Instantiate the checkpoint PREFAB as a child of the TILE
                GameObject checkpointInstance = Instantiate(checkpointPrefab, tile.transform); // Parent set here
                checkpointInstance.name = $"Checkpoint_{biomeForNextCheckpointSpawn.biomeName}";

                // Get the Checkpoint component to access its offset
                Checkpoint checkpointComponent = checkpointInstance.GetComponent<Checkpoint>();
                float yOffset = 0f; // Default offset
                if (checkpointComponent != null)
                {
                    yOffset = checkpointComponent.GetRelativeYOffset(); // Get offset from script
                }
                else
                {
                     Debug.LogWarning($"Checkpoint prefab '{checkpointPrefab.name}' is missing the Checkpoint script component!", checkpointPrefab);
                }

                // Set the LOCAL position relative to the tile (pivot assumed bottom-center)
                checkpointInstance.transform.localPosition = new Vector3(0f, yOffset, -0.1f); // Use offset

                Debug.Log($"[BackgroundTileManager] Spawned checkpoint {checkpointInstance.name} at local pos {checkpointInstance.transform.localPosition.ToString("F3")}");
            }
            else
            {
                Debug.LogError($"[BackgroundTileManager] Checkpoint prefab for {biomeForNextCheckpointSpawn.biomeName} is null!");
            }
            // Clear the flag regardless of success to prevent repeated attempts
            biomeForNextCheckpointSpawn = null;
        }
        // --- END MODIFIED CHECKPOINT SPAWNING ---


        // Handle queued biome swap (Premature ResetBiomeTileCount() is still here as per file)
        if (queuedTilePrefab && !transitionTileQueued)
        {
            tilePrefab        = queuedTilePrefab;
            queuedTilePrefab  = null;
            ResetBiomeTileCount(); // This was present in the provided file
        }

        // Check if transition tile should be queued (No NullRef check here as per file)
        int span = biomeManager.CurrentBiome.maxTileIndex - biomeManager.CurrentBiome.minTileIndex + 1;
        Debug.Log($"Current Biome: {biomeManager.CurrentBiome} + SPAN : {span}"); // Potential NullRef if biomeManager.CurrentBiome is null here
        Debug.Log($"ITS PRINTING");
        if (biomeTileCount >= span)
            transitionTileQueued = true;
    }

    private void RecycleTile(GameObject tile)
    {
        var stamp = tile.GetComponent<TileStamp>();
        if (stamp == null) { Destroy(tile); return; }    // should never happen

        // Clear the trail texture before putting it back in the pool
        if (trailPainter != null)
        {
            RectTransform rt = tile.GetComponent<RectTransform>();
            if (rt != null)
            {
                trailPainter.ClearTrailTexture(rt);
            }
        }
        tile.SetActive(false);
        tile.transform.SetParent(transform); // Re-parent to manager before pooling
        GetPool(stamp.SourcePrefab).Enqueue(tile);
    }
    #endregion

    // Handles the event from BiomeManager
    private void HandleBiomeTransitionComplete(BiomeData newBiome, bool isFirstBiomeOverall)
    {
        Debug.Log($"[BackgroundTileManager] Received Biome Transition Complete for: {newBiome.biomeName}. Is First Biome Overall: {isFirstBiomeOverall}");
        ResetBiomeTileCount(); // Reset count *when* transition is confirmed

        if (!isFirstBiomeOverall && newBiome.checkpointPrefab != null)
        {
            biomeForNextCheckpointSpawn = newBiome;
            Debug.Log($"[BackgroundTileManager] Queued checkpoint spawn for biome: {newBiome.biomeName}");
        }
        else
        {
            biomeForNextCheckpointSpawn = null; // Ensure no checkpoint for the first biome or if prefab is missing
            if(isFirstBiomeOverall) Debug.Log("[BackgroundTileManager] Not queuing checkpoint spawn - it's the first overall biome.");
            else if(newBiome.checkpointPrefab == null) Debug.Log($"[BackgroundTileManager] Not queuing checkpoint spawn - {newBiome.biomeName} has no checkpoint prefab assigned.");
        }
        isFirstBiomeEver = false; // Any transition means we are past the first biome
    }

    // Stores the next biome's tile prefab
    public void QueueNextBiomeTilePrefab(GameObject newPrefab) => queuedTilePrefab = newPrefab;

    // Resets biome-specific counters
    public void ResetBiomeTileCount()
    {
        Debug.Log($"[BackgroundTileManager] Resetting biome tile count from {biomeTileCount} to 0."); // Kept this Debug.Log as requested previously
        biomeTileCount       = 0;
        transitionTileQueued = false;
    }

    #region Gizmos / util
    private static readonly Vector3[] s_corners = new Vector3[4];
    // Commented out OnDrawGizmos as per user preference for fewer comments/clutter
    // private void OnDrawGizmos()
    // {
    //     if (!Application.isPlaying) return;
    //     Gizmos.color = Color.green;
    //     Gizmos.DrawLine(new Vector3(-10, lastTileEndY, 0), new Vector3(10, lastTileEndY, 0));
    // }
    #endregion

    // Unsubscribe from event on destruction
    private void OnDestroy()
    {
        if (biomeManager != null)
        {
            biomeManager.OnBiomeTransitionComplete -= HandleBiomeTransitionComplete;
        }
    }
}

/// <summary>
/// Lightweight component added to every spawned tile so we can return it to the correct pool.
/// </summary>
public class TileStamp : MonoBehaviour
{
    public GameObject SourcePrefab;
}