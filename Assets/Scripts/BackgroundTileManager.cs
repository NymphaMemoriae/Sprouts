using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class BackgroundTileManager : MonoBehaviour
{
    [Header("Tile Settings")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private int initialPoolSize = 3;
    [SerializeField] private float bufferZone = 10f;

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform playerHead;
    [SerializeField] private BiomeManager biomeManager;

    private Queue<GameObject> tilePool = new Queue<GameObject>();
    private List<GameObject> activeTiles = new List<GameObject>();
    private float tileHeight;
    private float lastTileEndY = 0f;
    private TrailPainter trailPainter;

    private int biomeTileCount = 0;
    private bool transitionTileQueued = false;
    private GameObject queuedTilePrefab = null;

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
            tile.SetActive(false);
            tilePool.Enqueue(tile);
        }

        foreach (GameObject tile in tilePool)
        {
            Image image = tile.GetComponent<Image>();
            if (image != null && image.material != null)
            {
                image.material = new Material(image.material);

                if (!tile.CompareTag("Ground"))
                    tile.tag = "Ground";

                if (image.material.HasProperty("_AspectRatio"))
                {
                    RectTransform rt = tile.GetComponent<RectTransform>();
                    float aspectRatio = rt.rect.width / rt.rect.height;
                    image.material.SetFloat("_AspectRatio", aspectRatio);
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
            }
        }
        else
        {
            SetupInitialTiles();
        }
    }

    private void SpawnNextTile()
    {
        GameObject tile;

        GameObject prefabToUse = (transitionTileQueued && biomeManager.CurrentBiome.transitionTilePrefab != null)
            ? biomeManager.CurrentBiome.transitionTilePrefab
            : queuedTilePrefab ?? tilePrefab;

        // ✅ Pool reuse logic only for regular tiles
        if (prefabToUse == tilePrefab && tilePool.Count > 0)
        {
            tile = tilePool.Dequeue();
            tile.SetActive(true);
        }
        else
        {
            tile = Instantiate(prefabToUse, Vector3.zero, Quaternion.identity, transform);
            tile.SetActive(true);
        }

       if (trailPainter != null)
        {
            RectTransform rt = tile.GetComponent<RectTransform>();
            if (rt != null)
            {
                trailPainter.ClearTrailTexture(rt); // ✅ clear any old trail first
            }

            trailPainter.AssignTextureToTile(tile); // ✅ then assign fresh texture
        }


        RectTransform rectTransform = tile.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 0);
        rectTransform.pivot = new Vector2(0.5f, 0);

        Vector3 worldPos = new Vector3(0, lastTileEndY, 0);
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        rectTransform.localPosition = localPos;

        lastTileEndY += tileHeight;
        activeTiles.Add(tile);
        biomeTileCount++;

        if (queuedTilePrefab != null && !transitionTileQueued)
        {
            tilePrefab = queuedTilePrefab;
            queuedTilePrefab = null;
            ResetBiomeTileCount();
        }

        int max = biomeManager.CurrentBiome.maxTileIndex;
        int min = biomeManager.CurrentBiome.minTileIndex;
        if (biomeTileCount == (max - min))
        {
            transitionTileQueued = true;
        }
    }

    private void RecycleTile(GameObject tile)
    {
        // ✅ DO NOT clear trail — just disable and return to pool
        tile.SetActive(false);
        tilePool.Enqueue(tile);
    }

    public void QueueNextBiomeTilePrefab(GameObject newTilePrefab)
    {
        if (newTilePrefab == null) return;
        queuedTilePrefab = newTilePrefab;
    }

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
            float topY = cameraPos.y + mainCamera.orthographicSize + bufferZone;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(-cameraWidth / 2, bottomY, 0), new Vector3(cameraWidth / 2, bottomY, 0));

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(new Vector3(-cameraWidth / 2, topY, 0), new Vector3(cameraWidth / 2, topY, 0));
        }
    }
}
