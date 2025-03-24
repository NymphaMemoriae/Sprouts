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
    
    private Queue<GameObject> tilePool = new Queue<GameObject>();
    private List<GameObject> activeTiles = new List<GameObject>();
    private float tileHeight;
    private float lastTileEndY = 0f; // Track where the last tile ends
    private TrailPainter trailPainter;
    
    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        if (playerHead == null && Object.FindAnyObjectByType<PlantController>() != null)
            playerHead = Object.FindAnyObjectByType<PlantController>().PlantHead;
        
        // Find the TrailPainter component
        trailPainter = Object.FindAnyObjectByType<TrailPainter>();
        
        // Initialize the pool with initial tiles
        InitializeTilePool();
        
        // Set up the first tiles to cover the screen
        SetupInitialTiles();
    }
    
    void Update()
    {
        // Check if we need to spawn more tiles as player moves up
        ManageTiles();
    }
    
    private void InitializeTilePool()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("Tile prefab is not assigned!");
            return;
        }
        
        // Get the first tile to measure its height
        GameObject firstTile = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity, transform);
        RectTransform rectTransform = firstTile.GetComponent<RectTransform>();
        
        if (rectTransform != null)
        {
            // Make sure we're getting the actual height in world units
            tileHeight = rectTransform.rect.height;
            Debug.Log($"Tile height is: {tileHeight}");
        }
        else
        {
            Debug.LogError("Tile prefab must have a RectTransform component!");
            Destroy(firstTile);
            return;
        }
        
        // Add the first tile to our pool
        tilePool.Enqueue(firstTile);
        firstTile.SetActive(false);
        
        // Create the rest of the initial pool
        for (int i = 1; i < initialPoolSize; i++)
        {
            GameObject tile = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity, transform);
            tilePool.Enqueue(tile);
            tile.SetActive(false);
        }
        
        // Each tile should have its own material instance
        foreach (GameObject tile in tilePool)
        {
            Image image = tile.GetComponent<Image>();
            if (image != null && image.material != null)
            {
                // Create new material instance
                image.material = new Material(image.material);
                
                // Set the ground tag
                if (!tile.CompareTag("Ground"))
                {
                    tile.tag = "Ground";
                }
                
                // If the aspect ratio property exists, set it
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
        // Calculate how many tiles we need to fill the screen
        float cameraHeight = 2f * mainCamera.orthographicSize;
        int tilesNeeded = Mathf.CeilToInt((cameraHeight + 2 * bufferZone) / tileHeight);
        
        // Start with the bottom tile positioned to cover below the camera view
        float startY = mainCamera.transform.position.y - mainCamera.orthographicSize - bufferZone;
        lastTileEndY = startY;
        
        // Spawn tiles starting from the bottom going up
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
        
        // Remove tiles that are too far below
        for (int i = activeTiles.Count - 1; i >= 0; i--)
        {
            RectTransform tileRect = activeTiles[i].GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            tileRect.GetWorldCorners(corners);
            
            // Top of the tile is at corners[1] or corners[2]
            float tileTopY = corners[1].y;
            
            if (tileTopY < cameraBottomY)
            {
                RecycleTile(activeTiles[i]);
                activeTiles.RemoveAt(i);
                Debug.Log("Recycled a tile that went below camera view");
            }
        }
        
        // Add new tiles at the top if needed
        if (activeTiles.Count > 0)
        {
            // Get the last (topmost) active tile
            RectTransform topTileRect = activeTiles[activeTiles.Count - 1].GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            topTileRect.GetWorldCorners(corners);
            
            // Top of the tile is at corners[1] or corners[2]
            float topTileEndY = corners[1].y;
            
            // If the top tile doesn't reach the camera's top view (plus buffer), spawn another
            if (topTileEndY < cameraTopY)
            {
                // Update the lastTileEndY to match where our current top tile ends
                lastTileEndY = topTileEndY;
                SpawnNextTile();
                Debug.Log("Spawned a new tile at the top");
            }
        }
        else
        {
            // If somehow we have no active tiles, restart with initial setup
            SetupInitialTiles();
            Debug.LogWarning("No active tiles found, resetting initial tiles");
        }
    }
    
    private void SpawnNextTile()
    {
        GameObject tile;
        
        // Get a tile from pool or create a new one
        if (tilePool.Count > 0)
        {
            tile = tilePool.Dequeue();
        }
        else
        {
            tile = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity, transform);
            
            // Ensure the new tile has its own material instance
            Image image = tile.GetComponent<Image>();
            if (image != null && image.material != null)
            {
                image.material = new Material(image.material);
            }
            
            // Set the ground tag
            if (!tile.CompareTag("Ground"))
            {
                tile.tag = "Ground";
            }
        }
        
        tile.SetActive(true);
        
        // Set up the trail texture for this tile
        if (trailPainter != null)
        {
            trailPainter.AssignTextureToTile(tile);
        }
        
        // Position the tile's bottom at where the last tile ended
        RectTransform rectTransform = tile.GetComponent<RectTransform>();
        
        // We need to convert the world position to the local position in the Canvas
        // First, make sure we're using the correct anchor setup
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 0);
        rectTransform.pivot = new Vector2(0.5f, 0);
        
        // Find the local position that corresponds to the world position
        Vector3 worldPos = new Vector3(0, lastTileEndY, 0);
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        
        // Set the position
        rectTransform.localPosition = localPos;
        
        // Update lastTileEndY to be at the top of this new tile
        lastTileEndY += tileHeight;
        
        // Add to active tiles list
        activeTiles.Add(tile);
        
        Debug.Log($"Spawned tile at y={localPos.y}, new lastTileEndY={lastTileEndY}");
    }
    
    private void RecycleTile(GameObject tile)
    {
        // Clear any trail markings on this tile
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
    // Add this new method to BackgroundTileManager
    public void SetBiomeTilePrefab(GameObject newTilePrefab)
    {
        if (newTilePrefab == null)
        {
            Debug.LogError("Attempted to set null tile prefab");
            return;
        }

        // Don't do anything if it's the same prefab
        if (tilePrefab == newTilePrefab)
            return;
            
        Debug.Log($"Changing tile prefab to {newTilePrefab.name}");
        
        // Store the old prefab for comparison
        GameObject oldPrefab = tilePrefab;
        
        // Update the prefab reference
        tilePrefab = newTilePrefab;
        
        // Clear the existing pool of old prefabs
        tilePool.Clear();
        
        // Replace all active tiles with the new prefab
        for (int i = 0; i < activeTiles.Count; i++)
        {
            // Get current tile info
            GameObject oldTile = activeTiles[i];
            RectTransform oldRect = oldTile.GetComponent<RectTransform>();
            
            // Create a new tile at the same position
            GameObject newTile = Instantiate(newTilePrefab, oldRect.position, Quaternion.identity, transform);
            RectTransform newRect = newTile.GetComponent<RectTransform>();
            
            // Copy transformations
            newRect.anchorMin = oldRect.anchorMin;
            newRect.anchorMax = oldRect.anchorMax;
            newRect.pivot = oldRect.pivot;
            newRect.localPosition = oldRect.localPosition;
            newRect.sizeDelta = oldRect.sizeDelta;
            
            // Ensure the new tile has its own material instance
            Image image = newTile.GetComponent<Image>();
            if (image != null && image.material != null)
            {
                image.material = new Material(image.material);
            }
            
            // Replace in active tiles list
            activeTiles[i] = newTile;
            
            // Destroy the old tile
            Destroy(oldTile);
        }
        
        // Get the tile height from the new prefab
        RectTransform rectTransform = newTilePrefab.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            tileHeight = rectTransform.rect.height;
            Debug.Log($"New tile height: {tileHeight}");
        }
        
        // Create some new pooled tiles with the new prefab
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject tile = Instantiate(newTilePrefab, Vector3.zero, Quaternion.identity, transform);
            
            // Ensure the new tile has its own material instance
            Image image = tile.GetComponent<Image>();
            if (image != null && image.material != null)
            {
                image.material = new Material(image.material);
            }
            
            tile.SetActive(false);
            tilePool.Enqueue(tile);
        }
    }
    // This method can be useful for debugging
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Draw a line at lastTileEndY
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-10, lastTileEndY, 0), new Vector3(10, lastTileEndY, 0));
        
        // Draw camera bounds
        if (mainCamera != null)
        {
            float cameraHeight = 2f * mainCamera.orthographicSize;
            float cameraWidth = cameraHeight * mainCamera.aspect;
            
            Vector3 cameraPos = mainCamera.transform.position;
            
            // Bottom line
            float bottomY = cameraPos.y - mainCamera.orthographicSize - bufferZone;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(-cameraWidth/2, bottomY, 0), new Vector3(cameraWidth/2, bottomY, 0));
            
            // Top line
            float topY = cameraPos.y + mainCamera.orthographicSize + bufferZone;
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(new Vector3(-cameraWidth/2, topY, 0), new Vector3(cameraWidth/2, topY, 0));
        }
    }
}