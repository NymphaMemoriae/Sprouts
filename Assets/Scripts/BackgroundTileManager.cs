using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class BackgroundTileManager : MonoBehaviour
{
    [Header("Tile Settings")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private int initialPoolSize = 3;
    [SerializeField] private float bufferZone = 10f; // Extra space beyond camera view

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform playerHead;
    [SerializeField] private BiomeManager biomeManager; // ✅ NEW

    private Queue<GameObject> tilePool = new Queue<GameObject>();
    private List<GameObject> activeTiles = new List<GameObject>();
    private float tileHeight;
    private float lastTileEndY = 0f;
    private TrailPainter trailPainter;

    // ✅ NEW
    private int biomeTileCount = 0;
    private bool transitionTileQueued = false;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (playerHead == null && Object.FindAnyObjectByType<PlantController>() != null)
            playerHead = Object.FindAnyObjectByType<PlantController>().PlantHead;

        if (biomeManager == null)
            biomeManager = Object.FindAnyObjectByType<BiomeManager>();

        trailPainter = Object.FindAnyObjectByType<TrailPainter>();

        InitializeTilePool();
        SetupInitialTiles();
    }

    void Update()
    {
        ManageTiles();
    }

    private void InitializeTilePool()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("Tile prefab is not assigned!");
            return;
        }

        GameObject firstTile = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity, transform);
        RectTransform rectTransform = firstTile.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            tileHeight = rectTransform.rect.height;
            Debug.Log($"Tile height is: {tileHeight}");
        }
        else
        {
            Debug.LogError("Tile prefab must have a RectTransform component!");
            Destroy(firstTile);
            return;
        }

        tilePool.Enqueue(firstTile);
        firstTile.SetActive(false);

        for (int i = 1; i < initialPoolSize; i++)
        {
            GameObject tile = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity, transform);
            tilePool.Enqueue(tile);
            tile.SetActive(false);
        }

        foreach (GameObject tile in tilePool)
        {
            Image image = tile.GetComponent<Image>();
            if (image != null && image.material != null)
            {
                image.material = new Material(image.material);

                if (!tile.CompareTag("Ground"))
                {
                    tile.tag = "Ground";
                }

                if (image.material.HasProperty("_AspectRatio"))
                {
                    RectTransform rt = tile.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        float aspectRatio = rt.rect.width / rt.rect.height;
                        image.material.SetFloat("_AspectRatio", aspectRatio);
                    }
                }
            }
        }
    }

    private void SetupInitialTiles()
    {
        float cameraHeight = 2f * mainCamera.orthographicSize;
        int tilesNeeded = Mathf.CeilToInt((cameraHeight + 2 * bufferZone) / tileHeight);

        float startY = mainCamera.transform.position.y - mainCamera.orthographicSize - bufferZone;
        lastTileEndY = startY;

        for (int i = 0; i < tilesNeeded; i++)
        {
            SpawnNextTile();
        }
    }

    private void ManageTiles()
    {
        if (playerHead == null)
            return;

        float cameraBottomY = mainCamera.transform.position.y - mainCamera.orthographicSize - bufferZone;
        float cameraTopY = mainCamera.transform.position.y + mainCamera.orthographicSize + bufferZone;

        for (int i = activeTiles.Count - 1; i >= 0; i--)
        {
            RectTransform tileRect = activeTiles[i].GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            tileRect.GetWorldCorners(corners);

            float tileTopY = corners[1].y;

            if (tileTopY < cameraBottomY)
            {
                RecycleTile(activeTiles[i]);
                activeTiles.RemoveAt(i);
                Debug.Log("Recycled a tile that went below camera view");
            }
        }

        if (activeTiles.Count > 0)
        {
            RectTransform topTileRect = activeTiles[activeTiles.Count - 1].GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            topTileRect.GetWorldCorners(corners);

            float topTileEndY = corners[1].y;

            if (topTileEndY < cameraTopY)
            {
                lastTileEndY = topTileEndY;
                SpawnNextTile();
                Debug.Log("Spawned a new tile at the top");
            }
        }
        else
        {
            SetupInitialTiles();
            Debug.LogWarning("No active tiles found, resetting initial tiles");
        }
    }

    private void SpawnNextTile()
    {
        GameObject tile;

        // ✅ Check if transition tile should be spawned
        if (transitionTileQueued && biomeManager.CurrentBiome.transitionTilePrefab != null)
        {
            tile = Instantiate(biomeManager.CurrentBiome.transitionTilePrefab, Vector3.zero, Quaternion.identity, transform);
            transitionTileQueued = false;
            Debug.Log("[Biome] Inserted transition tile.");
        }
        else
        {
            if (tilePool.Count > 0)
            {
                tile = tilePool.Dequeue();
            }
            else
            {
                tile = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity, transform);

                Image image = tile.GetComponent<Image>();
                if (image != null && image.material != null)
                {
                    image.material = new Material(image.material);
                }

                if (!tile.CompareTag("Ground"))
                {
                    tile.tag = "Ground";
                }
            }
        }

        tile.SetActive(true);

        if (trailPainter != null)
        {
            trailPainter.AssignTextureToTile(tile);
        }

        RectTransform rectTransform = tile.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 0);
        rectTransform.pivot = new Vector2(0.5f, 0);

        Vector3 worldPos = new Vector3(0, lastTileEndY, 0);
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        rectTransform.localPosition = localPos;

        float tileWorldHeight = rectTransform.TransformVector(Vector3.up * rectTransform.rect.height).y;
        Debug.Log($"[Tile Debug] Spawned tile world height: {tileWorldHeight} meters");

        lastTileEndY += tileHeight;
        activeTiles.Add(tile);

        biomeTileCount++;

        // ✅ Queue transition tile if next tile is the biome's last one
        int max = biomeManager.CurrentBiome.maxTileIndex;
        int min = biomeManager.CurrentBiome.minTileIndex;
        if (biomeTileCount == (max - min))
        {
            transitionTileQueued = true;
            Debug.Log("[Biome] Queued transition tile for next spawn.");
        }

        Debug.Log($"Spawned tile at y={localPos.y}, new lastTileEndY={lastTileEndY}");
    }

    private void RecycleTile(GameObject tile)
    {
        if (trailPainter != null)
        {
            RectTransform rectTransform = tile.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                trailPainter.ClearTrailTexture(rectTransform);
            }
        }

        tile.SetActive(false);
        tilePool.Enqueue(tile);
    }

    public void SetBiomeTilePrefab(GameObject newTilePrefab)
    {
        if (newTilePrefab == null)
        {
            Debug.LogError("Attempted to set null tile prefab");
            return;
        }

        if (tilePrefab == newTilePrefab)
            return;

        Debug.Log($"Changing tile prefab to {newTilePrefab.name}");

        tilePrefab = newTilePrefab;
        tilePool.Clear();

        for (int i = 0; i < activeTiles.Count; i++)
        {
            GameObject oldTile = activeTiles[i];
            RectTransform oldRect = oldTile.GetComponent<RectTransform>();

            GameObject newTile = Instantiate(newTilePrefab, oldRect.position, Quaternion.identity, transform);
            RectTransform newRect = newTile.GetComponent<RectTransform>();

            newRect.anchorMin = oldRect.anchorMin;
            newRect.anchorMax = oldRect.anchorMax;
            newRect.pivot = oldRect.pivot;
            newRect.localPosition = oldRect.localPosition;
            newRect.sizeDelta = oldRect.sizeDelta;

            Image image = newTile.GetComponent<Image>();
            if (image != null && image.material != null)
            {
                image.material = new Material(image.material);
            }

            activeTiles[i] = newTile;
            Destroy(oldTile);
        }

        RectTransform rectTransform = newTilePrefab.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            tileHeight = rectTransform.rect.height;
            Debug.Log($"New tile height: {tileHeight}");
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject tile = Instantiate(newTilePrefab, Vector3.zero, Quaternion.identity, transform);
            Image image = tile.GetComponent<Image>();
            if (image != null && image.material != null)
            {
                image.material = new Material(image.material);
            }

            tile.SetActive(false);
            tilePool.Enqueue(tile);
        }
    }

    // ✅ Used by BiomeManager to reset on biome change
    public void ResetBiomeTileCount()
    {
        biomeTileCount = 0;
        transitionTileQueued = false;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-10, lastTileEndY, 0), new Vector3(10, lastTileEndY, 0));

        if (mainCamera != null)
        {
            float cameraHeight = 2f * mainCamera.orthographicSize;
            float cameraWidth = cameraHeight * mainCamera.aspect;

            Vector3 cameraPos = mainCamera.transform.position;

            float bottomY = cameraPos.y - mainCamera.orthographicSize - bufferZone;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(-cameraWidth / 2, bottomY, 0), new Vector3(cameraWidth / 2, bottomY, 0));

            float topY = cameraPos.y + mainCamera.orthographicSize + bufferZone;
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(new Vector3(-cameraWidth / 2, topY, 0), new Vector3(cameraWidth / 2, topY, 0));
        }
    }
}
