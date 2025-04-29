using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TrailPainter : MonoBehaviour
{
    [Header("Render Textures")]
    public RenderTexture sharedTrailTexture;
    public int textureResolution = 512;

    [Header("References")]
    public Material drawMaterial;
    public Transform plantHead;

    [Header("Brush Settings")]
    public float brushSize = 0.02f;
    public bool debugMode = false;

    [Header("Line Drawing")]
    [Range(0.01f, 1f)]
    public float minSegmentDistance = 0.01f;
    [Range(1, 20)]
    public int lineSegments = 8;

    private Dictionary<RectTransform, RenderTexture> tileTextures = new Dictionary<RectTransform, RenderTexture>();
    private Dictionary<RectTransform, Vector2> lastUVPositions = new Dictionary<RectTransform, Vector2>();
    private RenderTexture tempTexture;
    private bool isInitialized = false;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        InitializeSharedTexture();

        tempTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
        tempTexture.name = "TempTrailTexture";
        tempTexture.Create();

        SetupGroundTiles();
        isInitialized = true;
        Debug.Log("TrailPainter initialized successfully");
    }

    void InitializeSharedTexture()
    {
        if (sharedTrailTexture == null)
        {
            sharedTrailTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
            sharedTrailTexture.name = "SharedTrailTexture";
            sharedTrailTexture.Create();
            Debug.Log("Created new SharedTrailTexture");
        }
    }

    // Note: SetupGroundTiles creates initial textures, but AssignTextureToTile will replace them if called.
    // Consider simplifying SetupGroundTiles if AssignTextureToTile handles all cases now.
    void SetupGroundTiles()
    {
        Image[] groundImages = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
        int setupCount = 0;

        foreach (Image image in groundImages)
        {
            if (image.gameObject.CompareTag("Ground"))
            {
                RectTransform rectTransform = image.rectTransform;
                if (rectTransform == null) continue;

                // Create texture for initial tiles found at start
                RenderTexture tileTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
                tileTexture.name = $"TileTexture_{image.gameObject.name}_Initial";
                tileTexture.Create();

                RenderTexture.active = tileTexture;
                GL.Clear(true, true, Color.black);
                RenderTexture.active = null;

                // Release old if somehow already present (unlikely here but good practice)
                 if (tileTextures.TryGetValue(rectTransform, out RenderTexture oldTexture))
                 {
                     if(oldTexture != null) oldTexture.Release();
                 }

                tileTextures[rectTransform] = tileTexture;

                if (image.material != null)
                {
                    if (!image.material.name.EndsWith(" (Instance)"))
                    {
                        image.material = new Material(image.material);
                    }
                    image.material.SetTexture("_T", tileTexture);
                    float aspectRatio = rectTransform.rect.width / rectTransform.rect.height;
                    image.material.SetFloat("_AspectRatio", aspectRatio);
                    setupCount++;
                }
            }
        }
        Debug.Log($"Set up {setupCount} ground tiles initially");
    }

    void Update()
    {
        if (!isInitialized || plantHead == null || mainCam == null) return;

        Image[] groundImages = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);

        foreach (Image groundImage in groundImages)
        {
            if (groundImage.gameObject.CompareTag("Ground"))
            {
                RectTransform rectTransform = groundImage.rectTransform;
                if (rectTransform == null) continue;

                if (IsPointOverUIElement(plantHead.position, rectTransform))
                {
                    // Get the texture (should always exist if AssignTextureToTile was called)
                    RenderTexture tileTexture;
                    if (!tileTextures.TryGetValue(rectTransform, out tileTexture))
                    {
                        // This case might happen if a tile becomes active before AssignTextureToTile is called
                        // We could force an assignment here, or rely on BackgroundTileManager calling it.
                         Debug.LogWarning($"Texture not found for active tile {groundImage.name} in Update. Assigning now.");
                         AssignTextureToTile(groundImage.gameObject); // Force assign it
                         if (!tileTextures.TryGetValue(rectTransform, out tileTexture)) continue; // Skip if assignment failed
                    }
                    
                    if (tileTexture == null || !tileTexture.IsCreated()) // Add null/created check
                    {
                        Debug.LogError($"Texture for {groundImage.name} is null or not created in Update!");
                        continue; // Skip if texture invalid
                    }


                    Vector2 currentUV = WorldPosToUV(plantHead.position, rectTransform);
                    Vector2 lastUV;

                    if (!lastUVPositions.TryGetValue(rectTransform, out lastUV))
                    {
                        lastUV = currentUV;
                        lastUVPositions[rectTransform] = currentUV;
                        DrawTrailMark(currentUV, tileTexture);
                    }
                    else
                    {
                        float uvDistance = Vector2.Distance(lastUV, currentUV);
                        if (uvDistance >= minSegmentDistance)
                        {
                            DrawTrailLine(lastUV, currentUV, tileTexture);
                            lastUVPositions[rectTransform] = currentUV;
                        }
                    }
                    if (debugMode) { /* Debug Log */ }
                }
                else
                {
                    if (lastUVPositions.ContainsKey(rectTransform))
                    {
                        lastUVPositions.Remove(rectTransform);
                    }
                }
            }
        }
    }

    bool IsPointOverUIElement(Vector3 worldPos, RectTransform rectTransform)
    {
        if (mainCam == null) return false;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCam, worldPos);
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, mainCam);
    }

    Vector2 WorldPosToUV(Vector3 worldPos, RectTransform rectTransform)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            RectTransformUtility.WorldToScreenPoint(mainCam, worldPos),
            mainCam,
            out localPoint
        );
        Rect rect = rectTransform.rect;
        float u = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float v = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
        return new Vector2(u, v);
    }

    void DrawTrailMark(Vector2 uv, RenderTexture targetTexture)
    {
        if (targetTexture == null || !targetTexture.IsCreated() || tempTexture == null || !tempTexture.IsCreated() || drawMaterial == null) // Safety checks
        {
            Debug.LogError("Cannot DrawTrailMark - Missing texture or material");
            return;
        }
        
        RenderTexture.active = tempTexture;
        Graphics.Blit(targetTexture, tempTexture); // Copy existing state

        drawMaterial.SetVector("_BrushPosition", new Vector4(uv.x, uv.y, 0, 0));
        drawMaterial.SetFloat("_BrushSize", brushSize);

        Graphics.Blit(tempTexture, targetTexture, drawMaterial); // Draw brush onto original

        RenderTexture.active = null;
    }

    void DrawTrailLine(Vector2 startUV, Vector2 endUV, RenderTexture targetTexture)
    {
        for (int i = 0; i <= lineSegments; i++)
        {
            float t = (float)i / lineSegments;
            Vector2 pointUV = Vector2.Lerp(startUV, endUV, t);
            DrawTrailMark(pointUV, targetTexture);
        }
    }

    void OnDrawGizmos()
    {
        // Gizmos code remains the same...
        if (!debugMode || !Application.isPlaying || mainCam == null || plantHead == null) return;
        
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(plantHead.position, 0.1f);
        
        Image[] groundImages = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
        foreach (Image groundImage in groundImages)
        {
            if (groundImage.gameObject.CompareTag("Ground"))
            {
                RectTransform rect = groundImage.rectTransform;
                if (rect != null)
                {
                    Gizmos.color = IsPointOverUIElement(plantHead.position, rect) ? Color.yellow : Color.red;
                    Vector3[] corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    for (int i = 0; i < 4; i++) { Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]); }
                }
            }
        }
    }

    // --- MODIFIED AssignTextureToTile ---
    public void AssignTextureToTile(GameObject tile)
    {
        if (!tile.CompareTag("Ground")) return;

        RectTransform rectTransform = tile.GetComponent<RectTransform>();
        Image image = tile.GetComponent<Image>();

        if (rectTransform == null || image == null || image.material == null) return;

        // Release old texture if one exists for this key
        if (tileTextures.TryGetValue(rectTransform, out RenderTexture oldTexture))
        {
            if (oldTexture != null)
            {
                if (RenderTexture.active == oldTexture) RenderTexture.active = null;
                oldTexture.Release();
                // Destroy(oldTexture); // Optional
            }
            tileTextures.Remove(rectTransform); // Remove old entry
        }

        // Always create a new texture
        RenderTexture tileTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
        tileTexture.name = $"TileTexture_{tile.name}_{Time.frameCount}";
        if (!tileTexture.Create())
        {
            Debug.LogError($"Failed to create RenderTexture for {tile.name}");
            return;
        }

        // Clear the new texture immediately
        RenderTexture.active = tileTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = null;

        // Store the new texture
        tileTextures[rectTransform] = tileTexture;

        // Assign the new texture to the material
        image.material.SetTexture("_T", tileTexture);

        // Set aspect ratio
        float aspectRatio = rectTransform.rect.width / rectTransform.rect.height;
        image.material.SetFloat("_AspectRatio", aspectRatio);
    }

    // --- MODIFIED ClearTrailTexture (Now Releases) ---
    public void ClearTrailTexture(RectTransform rectTransform)
    {
        if (rectTransform == null) return; // Added null check

        if (tileTextures.TryGetValue(rectTransform, out RenderTexture tileTexture))
        {
             if (tileTexture != null)
             {
                 if (RenderTexture.active == tileTexture) RenderTexture.active = null;
                 tileTexture.Release();
                 // Destroy(tileTexture); // Optional
             }
             tileTextures.Remove(rectTransform); // Remove from dictionary

             // Also remove from lastUVPositions if present
             if (lastUVPositions.ContainsKey(rectTransform))
             {
                 lastUVPositions.Remove(rectTransform);
             }
        }
    }

    // --- ADDED OnDestroy ---
    void OnDestroy()
    {
        Debug.Log("TrailPainter OnDestroy: Releasing all textures.");
        // Use ToList() to avoid modifying dictionary while iterating
        var keys = new List<RectTransform>(tileTextures.Keys);
        foreach (var key in keys)
        {
            if (tileTextures.TryGetValue(key, out var texture) && texture != null)
            {
                 if (RenderTexture.active == texture) RenderTexture.active = null;
                 texture.Release();
                 // Destroy(texture);
            }
        }
        tileTextures.Clear();

        if (tempTexture != null)
        {
             if (RenderTexture.active == tempTexture) RenderTexture.active = null;
             tempTexture.Release();
             // Destroy(tempTexture);
             tempTexture = null;
        }
    }
}