using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

/// <summary>
/// Spawns and recycles vertical background tiles.
/// Handles special first tiles for biomes.
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
        new Dictionary<GameObject, Queue<GameObject>>();

    private readonly List<GameObject> activeTiles = new(); // Only stores pooled tiles

    private float tileHeight; // Determined from default tilePrefab
    private float lastTileEndY;
    private TrailPainter trailPainter;

    private int  biomeTileCount;
    private bool transitionTileQueued;
    private GameObject queuedTilePrefab;
    private float currentZOffset = 0f;
    private const float ZIncrement = 0.1f;
    // private BiomeData biomeForNextCheckpointSpawn = null; // <-- REMOVED, no longer needed
    private bool isFirstBiomeEver = true;
    #endregion

    #region Unity lifecycle
    private void Start()
    {
        mainCamera   ??= Camera.main;
        playerHead   ??= FindObjectOfType<PlantController>()?.PlantHead;
        biomeManager ??= FindObjectOfType<BiomeManager>();
        trailPainter  =  FindObjectOfType<TrailPainter>();

        if (biomeManager != null)
        {
            biomeManager.OnBiomeTransitionComplete += HandleBiomeTransitionComplete;
            Debug.Log("[BackgroundTileManager] Subscribed to BiomeManager.OnBiomeTransitionComplete");
        }
        else
        {
            Debug.LogError("[BackgroundTileManager] BiomeManager reference is missing!");
        }
        currentZOffset = 0f;

        MeasureTileHeight(); // Measures default tile height
        if (tilePrefab != null)
        {
             PreWarmPool(tilePrefab, initialPoolSize);
        }
        SetupInitialTiles();
        isFirstBiomeEver = true;
    }

    private void Update() => ManageTiles();
    #endregion

    #region Pool helpers
    private Queue<GameObject> GetPool(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>();
            pools[prefab] = q;
        }
        return q;
    }

    private void PreWarmPool(GameObject prefab, int count)
    {
        if (prefab == null) {
             Debug.LogWarning("[BackgroundTileManager] Cannot pre-warm pool with a null prefab.");
             return;
        }
        var pool = GetPool(prefab);
        for (int i = 0; i < count; ++i)
        {
            var go = CreateTile(prefab);
            if (go != null) // Check if creation was successful
             {
                go.SetActive(false);
                pool.Enqueue(go);
             }
        }
    }
    #endregion

    #region Tile creation & recycling
    private GameObject CreateTile(GameObject prefab)
    {
         if (prefab == null) {
             Debug.LogError("[BackgroundTileManager] Attempted to create a tile from a null prefab.");
             return null;
         }
        var tile = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);

        var stamp = tile.GetComponent<TileStamp>() ?? tile.AddComponent<TileStamp>();
        stamp.SourcePrefab = prefab;

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
        if (tilePrefab == null)
        {
             Debug.LogError("[BackgroundTileManager] Default tilePrefab is not assigned. Cannot measure tile height.");
             tileHeight = 10f; // Assign a default fallback height
             return;
        }
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
        if (tileHeight <= 0) {
            Debug.LogError("[BackgroundTileManager] Invalid tileHeight. Cannot setup initial tiles.");
            return;
        }
        float cameraHeight = 2f * mainCamera.orthographicSize;
        int   tilesNeeded  = Mathf.CeilToInt((cameraHeight + 2 * bufferZone) / tileHeight);

        float startY = mainCamera.transform.position.y - mainCamera.orthographicSize - bufferZone;
        lastTileEndY = startY;

        // Ensure the first biome's special tile is handled if applicable
        // Note: BiomeManager might not have the first biome set yet in Start,
        // so the first SpawnNextTile call handles the initial state.
        for (int i = 0; i < tilesNeeded; ++i)
            SpawnNextTile();
    }
    #endregion

    #region Runtime tile management
    private void ManageTiles()
    {
        if (!playerHead || !mainCamera) return; // Added null check for camera

        float camBottom = mainCamera.transform.position.y - mainCamera.orthographicSize - bufferZone;
        float camTop    = mainCamera.transform.position.y + mainCamera.orthographicSize + bufferZone;

        // Recycle pooled off-screen tiles
        for (int i = activeTiles.Count - 1; i >= 0; --i)
        {
            // Added null check for safety, although elements in activeTiles should not be null
            if (activeTiles[i] == null)
            {
                activeTiles.RemoveAt(i);
                continue;
            }
            var tileRect = activeTiles[i].GetComponent<RectTransform>();
            if (tileRect == null) // Safety check
            {
                activeTiles.RemoveAt(i);
                 continue;
            }
            tileRect.GetWorldCorners(s_corners);
            float tileTopY = s_corners[1].y; // Use top corner for check
            if (tileTopY < camBottom)
            {
                RecycleTile(activeTiles[i]);
                activeTiles.RemoveAt(i);
            }
        }

        // Determine the top edge for spawning new tiles
        float currentTopEdge = lastTileEndY; // Default if no active tiles
        if (activeTiles.Count > 0 && activeTiles[^1] != null)
        {
             var topRect = activeTiles[^1].GetComponent<RectTransform>();
             if (topRect != null)
             {
                 topRect.GetWorldCorners(s_corners);
                 currentTopEdge = s_corners[1].y; // Top edge of the last active pooled tile
             }
        }
        // Also consider the last spawned tile's end Y, even if it was special
        currentTopEdge = Mathf.Max(currentTopEdge, lastTileEndY);

        // Spawn new ones if the top edge is below the camera's buffer zone
        while (currentTopEdge < camTop)
        {
            // Update lastTileEndY *before* spawning the next tile based on the actual top edge
            lastTileEndY = currentTopEdge;
            SpawnNextTile(); // This will update lastTileEndY internally

            // Recalculate the current top edge after spawning
            currentTopEdge = lastTileEndY; // SpawnNextTile updates this

            // Safety break if spawning fails to progress lastTileEndY
            if(currentTopEdge <= lastTileEndY && activeTiles.Count > 0 && activeTiles[^1]?.GetComponent<RectTransform>() != null) {
                 // If lastTileEndY didn't increase, maybe break to prevent infinite loop.
                 // This might happen if tile height is zero or negative.
                 // Re-check the actual top tile's position just in case.
                 activeTiles[^1].GetComponent<RectTransform>().GetWorldCorners(s_corners);
                 if(s_corners[1].y <= lastTileEndY) {
                     Debug.LogWarning("[BackgroundTileManager] Tile spawning did not increase lastTileEndY. Breaking loop.");
                     break;
                 }
                 currentTopEdge = s_corners[1].y;
            } else if (activeTiles.Count == 0 && currentTopEdge <= lastTileEndY) {
                 // Special case if the only tiles were special ones and got removed somehow or initial state
                 Debug.LogWarning("[BackgroundTileManager] No active pooled tiles and lastTileEndY did not increase. Breaking loop.");
                 break;
            }
        }
    }

    private void SpawnNextTile()
    {
        BiomeData currentBiome = biomeManager?.CurrentBiome; // Cache current biome

        // --- NEW: Special First Tile Handling ---
        if (biomeTileCount == 0 && currentBiome != null && currentBiome.firstTilePrefab != null)
        {
            GameObject firstTilePrefab = currentBiome.firstTilePrefab;
            Debug.Log($"[BackgroundTileManager] Spawning SPECIAL first tile for biome: {currentBiome.biomeName}");

            GameObject firstTileInstance = Instantiate(firstTilePrefab, Vector3.zero, Quaternion.identity, transform);
            if (firstTileInstance == null) {
                 Debug.LogError($"[BackgroundTileManager] Failed to instantiate special first tile prefab '{firstTilePrefab.name}' for biome '{currentBiome.biomeName}'.", firstTilePrefab);
                 return; // Stop if instantiation fails
            }
            firstTileInstance.name = $"FirstTile_{currentBiome.biomeName}";

            RectTransform firstTileRect = firstTileInstance.GetComponent<RectTransform>();
            if (firstTileRect == null)
            {
                Debug.LogError($"Special first tile prefab '{firstTilePrefab.name}' is missing a RectTransform!", firstTilePrefab);
                Destroy(firstTileInstance);
                return;
            }

            firstTileRect.anchorMin = new Vector2(0.5f, 0);
            firstTileRect.anchorMax = new Vector2(0.5f, 0);
            firstTileRect.pivot     = new Vector2(0.5f, 0);

            float spawnY = lastTileEndY;
            Vector3 spawnLocalPosition = transform.InverseTransformPoint(new Vector3(0, spawnY, 0));
            spawnLocalPosition.z = currentZOffset;
            firstTileRect.localPosition = spawnLocalPosition;

            currentZOffset += ZIncrement;

            firstTileRect.GetWorldCorners(s_corners);
            lastTileEndY = s_corners[1].y; // Update based on the world position of the special tile's top edge

            // Spawn Checkpoint as Child
            if (currentBiome.checkpointPrefab != null)
            {
                GameObject checkpointPrefab = currentBiome.checkpointPrefab;
                GameObject checkpointInstance = Instantiate(checkpointPrefab, firstTileInstance.transform); // Parented
                 if (checkpointInstance == null) {
                     Debug.LogError($"[BackgroundTileManager] Failed to instantiate checkpoint prefab '{checkpointPrefab.name}' for biome '{currentBiome.biomeName}'.", checkpointPrefab);
                 } else {
                    checkpointInstance.name = $"Checkpoint_{currentBiome.biomeName}";
                    Checkpoint checkpointComponent = checkpointInstance.GetComponent<Checkpoint>();
                    float yOffset = checkpointComponent != null ? checkpointComponent.GetRelativeYOffset() : 0f;
                    checkpointInstance.transform.localPosition = new Vector3(0f, yOffset, -0.1f); // Local position relative to parent tile
                    Debug.Log($"[BackgroundTileManager] Spawned checkpoint {checkpointInstance.name} as child of SPECIAL tile {firstTileInstance.name}");
                    if (checkpointComponent == null) Debug.LogWarning($"Checkpoint prefab '{checkpointPrefab.name}' is missing Checkpoint script.", checkpointPrefab);
                 }
            }
            else if (!isFirstBiomeEver) // Don't warn for initial biome if no checkpoint is intended
            {
                Debug.LogWarning($"[BackgroundTileManager] Biome '{currentBiome.biomeName}' uses special first tile but has no checkpoint prefab assigned.");
            }

            // IMPORTANT: Do NOT add firstTileInstance to activeTiles.
            biomeTileCount = 1; // Increment count *after* spawning the first tile.
            return; // Handled special tile, exit.
        }
        // --- END: Special First Tile Handling ---


        // --- Regular Tile Spawning Logic (biomeTileCount > 0 or no special tile) ---

        GameObject prefabToUse = tilePrefab; // Start with default
        if (currentBiome != null)
        {
             // Use transition tile if queued AND available, else use queued standard tile, else current biome's standard, else default.
             prefabToUse = (transitionTileQueued && currentBiome.transitionTilePrefab != null)
                           ? currentBiome.transitionTilePrefab
                           : queuedTilePrefab ?? currentBiome.tilePrefab ?? tilePrefab;
        } else if (queuedTilePrefab != null) {
             prefabToUse = queuedTilePrefab; // Use queued if BiomeManager isn't ready
        }

        if (prefabToUse == null) {
             Debug.LogError("[BackgroundTileManager] Could not determine a valid tile prefab to spawn! Check BiomeData and default tilePrefab assignment.");
             // Need to update lastTileEndY somehow to prevent infinite loop? Maybe add default tile height?
             lastTileEndY += tileHeight > 0 ? tileHeight : 10f; // Prevent infinite loop in ManageTiles
             return;
        }

        var pool = GetPool(prefabToUse);
        GameObject tile = pool.Count > 0 ? pool.Dequeue() : CreateTile(prefabToUse);
        if(tile == null) {
             Debug.LogError($"[BackgroundTileManager] Failed to get or create tile from prefab '{prefabToUse.name}'.");
             lastTileEndY += tileHeight > 0 ? tileHeight : 10f; // Prevent infinite loop
             return;
        }

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

        var rect = tile.GetComponent<RectTransform>();
        if (rect == null) { // Should not happen if CreateTile works
             Debug.LogError($"[BackgroundTileManager] Pooled/Created tile '{tile.name}' is missing RectTransform.", tile);
             RecycleTile(tile); // Try to recycle it
             lastTileEndY += tileHeight > 0 ? tileHeight : 10f; // Prevent infinite loop
             return;
        }
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot     = new Vector2(0.5f, 0);

        float spawnYRegular = lastTileEndY;
        Vector3 spawnLocalPositionRegular = transform.InverseTransformPoint(new Vector3(0, spawnYRegular, 0));
        spawnLocalPositionRegular.z = currentZOffset;
        rect.localPosition = spawnLocalPositionRegular;

        currentZOffset += ZIncrement;

        rect.GetWorldCorners(s_corners);
        lastTileEndY = s_corners[1].y; // Update based on the world position of this regular tile's top edge

        activeTiles.Add(tile); // Add regular pooled tiles to the active list
        ++biomeTileCount;

        // Checkpoint spawning logic is now handled by the special first tile section above.
        // The `biomeForNextCheckpointSpawn` field and checks were removed.

        // Handle replacing the main prefab if a queued one was used
        if (queuedTilePrefab != null && prefabToUse == queuedTilePrefab && !transitionTileQueued)
        {
            tilePrefab        = queuedTilePrefab; // Update the default for subsequent pools/biomes
            queuedTilePrefab  = null;
            // ResetBiomeTileCount(); // Reset is handled by HandleBiomeTransitionComplete
        }

        // Check if transition tile should be queued next
        if (currentBiome != null)
        {
            int span = currentBiome.maxTileIndex - currentBiome.minTileIndex + 1;
            if (biomeTileCount >= span && currentBiome.transitionTilePrefab != null && !transitionTileQueued)
            {
                transitionTileQueued = true;
                 Debug.Log($"[BackgroundTileManager] Queuing transition tile for {currentBiome.biomeName}. Biome tile count: {biomeTileCount}, Span: {span}");
            }
        }
    }


    private void RecycleTile(GameObject tile)
    {
         if (tile == null) return; // Safety check

        var stamp = tile.GetComponent<TileStamp>();
        if (stamp == null || stamp.SourcePrefab == null) {
            Debug.LogWarning($"[BackgroundTileManager] Tile '{tile.name}' missing TileStamp or SourcePrefab. Destroying instead of recycling.", tile);
            Destroy(tile);
            return;
        }

        if (trailPainter != null)
        {
            RectTransform rt = tile.GetComponent<RectTransform>();
            if (rt != null)
            {
                trailPainter.ClearTrailTexture(rt);
            }
        }
        tile.SetActive(false);
        tile.transform.SetParent(transform);
        GetPool(stamp.SourcePrefab).Enqueue(tile);
    }
    #endregion

    // Handles the event from BiomeManager
    private void HandleBiomeTransitionComplete(BiomeData newBiome, bool isFirstBiomeOverall)
    {
        Debug.Log($"[BackgroundTileManager] Received Biome Transition Complete for: {newBiome?.biomeName ?? "NULL Biome"}. Is First Biome Overall: {isFirstBiomeOverall}");
        ResetBiomeTileCount(); // Reset count *when* transition is confirmed

        // Checkpoint queuing logic removed - it's handled when the special tile spawns.
        isFirstBiomeEver = isFirstBiomeOverall; // Update the flag based on the event parameter
    }

    // Stores the next biome's tile prefab
    public void QueueNextBiomeTilePrefab(GameObject newPrefab) => queuedTilePrefab = newPrefab;

    // Resets biome-specific counters
    public void ResetBiomeTileCount()
    {
        Debug.Log($"[BackgroundTileManager] Resetting biome tile count from {biomeTileCount} to 0.");
        biomeTileCount       = 0;
        transitionTileQueued = false;
        // Do NOT reset queuedTilePrefab here, it's needed for the *next* spawn
    }

    #region Gizmos / util
    private static readonly Vector3[] s_corners = new Vector3[4];
    // OnDrawGizmos commented out as per previous preference
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
/// Stores the source prefab on a tile instance for correct recycling.
/// </summary>
public class TileStamp : MonoBehaviour
{
    public GameObject SourcePrefab;
}