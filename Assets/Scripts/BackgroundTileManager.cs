using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

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
    #endregion

    #region Unity lifecycle
    private void Start()
    {
        mainCamera   ??= Camera.main;
        playerHead   ??= Object.FindAnyObjectByType<PlantController>()?.PlantHead;
        biomeManager ??= Object.FindAnyObjectByType<BiomeManager>();
        trailPainter  =  Object.FindAnyObjectByType<TrailPainter>();

        // Measure tile height via one temporary instance
        MeasureTileHeight();
        PreWarmPool(tilePrefab, initialPoolSize);
        SetupInitialTiles();
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
            if (s_corners[1].y < camTop)
            {
                lastTileEndY = s_corners[1].y;
                SpawnNextTile();
            }
        }
        else
        {
            SetupInitialTiles();
        }
    }

    private void SpawnNextTile()
    {
        GameObject prefabToUse = (transitionTileQueued && biomeManager.CurrentBiome.transitionTilePrefab)
                                 ? biomeManager.CurrentBiome.transitionTilePrefab
                                 : queuedTilePrefab ?? tilePrefab;

        var pool = GetPool(prefabToUse);
        GameObject tile = pool.Count > 0 ? pool.Dequeue() : CreateTile(prefabToUse);
        tile.SetActive(true);

        // Trail handle
        if (trailPainter)
        {
            var rt = tile.GetComponent<RectTransform>();
            trailPainter.ClearTrailTexture(rt);
            trailPainter.AssignTextureToTile(tile);
        }

        // Position & bookkeeping
        var rect = tile.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot     = new Vector2(0.5f, 0);

        rect.localPosition = transform.InverseTransformPoint(new Vector3(0, lastTileEndY, 0));
        lastTileEndY      += tileHeight;

        activeTiles.Add(tile);
        ++biomeTileCount;

        // Handle queued biome swap once the first tile spawned
        if (queuedTilePrefab && !transitionTileQueued)
        {
            tilePrefab        = queuedTilePrefab;
            queuedTilePrefab  = null;
            ResetBiomeTileCount();
        }

        int span = biomeManager.CurrentBiome.maxTileIndex - biomeManager.CurrentBiome.minTileIndex + 1;
        if (biomeTileCount >= span)
            transitionTileQueued = true;
    }

    private void RecycleTile(GameObject tile)
    {
        var stamp = tile.GetComponent<TileStamp>();
        if (stamp == null) { Destroy(tile); return; }    // should never happen
        tile.SetActive(false);
        GetPool(stamp.SourcePrefab).Enqueue(tile);
    }
    #endregion

    public void QueueNextBiomeTilePrefab(GameObject newPrefab) => queuedTilePrefab = newPrefab;

    public void ResetBiomeTileCount()
    {
        biomeTileCount       = 0;
        transitionTileQueued = false;
    }

    #region Gizmos / util
    private static readonly Vector3[] s_corners = new Vector3[4];
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-10, lastTileEndY, 0), new Vector3(10, lastTileEndY, 0));
    }
    #endregion
}

/// <summary>
/// Lightweight component added to every spawned tile so we can return it to the correct pool.
/// </summary>
public class TileStamp : MonoBehaviour
{
    public GameObject SourcePrefab;
}
