using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TrailPainter : MonoBehaviour
{
    [Header("Render Textures")]
    public RenderTexture sharedTrailTexture;  // A shared texture for reference
    public int textureResolution = 512;       // Resolution of the render texture
    
    [Header("References")]
    public Material drawMaterial;              // Material that blends new marks onto textures
    public Transform plantHead;                // The Plant's head moving through dirt
    
    [Header("Brush Settings")]
    public float brushSize = 0.02f;           // Size of the brush in UV space (0-1)
    public bool debugMode = false;            // Enable to show debug visualizations
    
    // Line drawing settings
    [Header("Line Drawing")]
    [Range(0.01f, 1f)]
    public float minSegmentDistance = 0.01f;  // Minimum UV distance to draw new segment
    [Range(1, 20)]
    public int lineSegments = 8;              // Number of segments to draw between positions
    
    private Dictionary<RectTransform, RenderTexture> tileTextures = new Dictionary<RectTransform, RenderTexture>();
    private Dictionary<RectTransform, Vector2> lastUVPositions = new Dictionary<RectTransform, Vector2>();
    private RenderTexture tempTexture;
    private bool isInitialized = false;
    private Camera mainCam;
    
    void Start()
    {
        mainCam = Camera.main;
        
        // Create and initialize the shared render texture
        InitializeSharedTexture();
        
        // Create temp texture for blending operations
        tempTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
        tempTexture.name = "TempTrailTexture";
        tempTexture.Create();
        
        // Set up individual textures for each ground tile
        SetupGroundTiles();
        
        isInitialized = true;
        Debug.Log("TrailPainter initialized successfully");
    }
    
    void InitializeSharedTexture()
    {
        // Create main trail texture if it doesn't exist
        if (sharedTrailTexture == null)
        {
            sharedTrailTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
            sharedTrailTexture.name = "SharedTrailTexture";
            sharedTrailTexture.Create();
            Debug.Log("Created new SharedTrailTexture");
        }
    }
    
    void SetupGroundTiles()
    {
        // Find all ground tiles
        Image[] groundImages = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
        int setupCount = 0;
        
        foreach (Image image in groundImages)
        {
            if (image.gameObject.CompareTag("Ground"))
            {
                RectTransform rectTransform = image.rectTransform;
                if (rectTransform == null) continue;
                
                // Create unique render texture for this tile
                RenderTexture tileTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
                tileTexture.name = $"TileTexture_{image.gameObject.name}";
                tileTexture.Create();
                
                // Clear the texture (start with black)
                RenderTexture.active = tileTexture;
                GL.Clear(true, true, Color.black);
                RenderTexture.active = null;
                
                // Store in dictionary
                tileTextures[rectTransform] = tileTexture;
                
                // Make sure the material is unique and set its texture
                if (image.material != null)
                {
                    // Create a new material instance if needed
                    if (!image.material.name.EndsWith(" (Instance)"))
                    {
                        image.material = new Material(image.material);
                    }
                    
                    // Assign this tile's unique texture
                    image.material.SetTexture("_T", tileTexture);
                    
                    // Set aspect ratio based on the rectTransform dimensions
                    float aspectRatio = rectTransform.rect.width / rectTransform.rect.height;
                    image.material.SetFloat("_AspectRatio", aspectRatio);
                    
                    setupCount++;
                }
            }
        }
        
        Debug.Log($"Set up {setupCount} ground tiles with unique textures");
    }
    
    void Update()
    {
        if (!isInitialized || plantHead == null || mainCam == null) return;
        
        // Find all ground tiles that might interact with the plant
        Image[] groundImages = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
        
        foreach (Image groundImage in groundImages)
        {
            if (groundImage.gameObject.CompareTag("Ground"))
            {
                RectTransform rectTransform = groundImage.rectTransform;
                if (rectTransform == null) continue;
                
                // Check if plant head is over this ground tile
                if (IsPointOverUIElement(plantHead.position, rectTransform))
                {
                    // Make sure this tile has a texture assigned
                    RenderTexture tileTexture;
                    if (!tileTextures.TryGetValue(rectTransform, out tileTexture))
                    {
                        // Create a new texture for this tile if it doesn't have one
                        tileTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
                        tileTexture.name = $"TileTexture_{groundImage.gameObject.name}";
                        tileTexture.Create();
                        tileTextures[rectTransform] = tileTexture;
                        
                        // Assign to material
                        if (groundImage.material != null)
                        {
                            groundImage.material.SetTexture("_T", tileTexture);
                            
                            // Set aspect ratio
                            float aspectRatio = rectTransform.rect.width / rectTransform.rect.height;
                            groundImage.material.SetFloat("_AspectRatio", aspectRatio);
                        }
                    }
                    
                    // Convert the world position to UV coordinates within this tile
                    Vector2 currentUV = WorldPosToUV(plantHead.position, rectTransform);
                    
                    // Get the last UV position for this tile (if it exists)
                    Vector2 lastUV;
                    if (!lastUVPositions.TryGetValue(rectTransform, out lastUV))
                    {
                        // If no previous position exists, just use the current one
                        lastUV = currentUV;
                        lastUVPositions[rectTransform] = currentUV;
                        
                        // Draw a single point at the current position
                        DrawTrailMark(currentUV, tileTexture);
                    }
                    else
                    {
                        // Calculate the distance between last and current UV positions
                        float uvDistance = Vector2.Distance(lastUV, currentUV);
                        
                        // If the distance is sufficient, draw a line between the points
                        if (uvDistance >= minSegmentDistance)
                        {
                            // Draw a line of trail marks from the last position to the current position
                            DrawTrailLine(lastUV, currentUV, tileTexture);
                            
                            // Update the last position
                            lastUVPositions[rectTransform] = currentUV;
                        }
                    }
                    
                    if (debugMode)
                    {
                        Debug.Log($"Drawing trail at UV: ({currentUV.x:F3}, {currentUV.y:F3}) on {groundImage.gameObject.name}");
                    }
                }
                else
                {
                    // Plant head is not over this tile, remove from lastUVPositions if present
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
        
        // Convert world position to screen position
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCam, worldPos);
        
        // Check if this point is inside the RectTransform
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, mainCam);
    }
    
    Vector2 WorldPosToUV(Vector3 worldPos, RectTransform rectTransform)
    {
        // Convert world position to position on the rect (local position)
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            RectTransformUtility.WorldToScreenPoint(mainCam, worldPos),
            mainCam,
            out localPoint
        );
        
        // Get normalized coordinates (0-1) within the rect
        Rect rect = rectTransform.rect;
        
        // Convert local space to UV space (0,0 is bottom left, 1,1 is top right)
        float u = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float v = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
        
        return new Vector2(u, v);
    }
    
    void DrawTrailMark(Vector2 uv, RenderTexture targetTexture)
    {
        // Set temporary texture as active
        RenderTexture.active = tempTexture;
        
        // Copy current trail state to temp texture
        Graphics.Blit(targetTexture, tempTexture);
        
        // Set the UV position and size for the brush in the shader
        drawMaterial.SetVector("_BrushPosition", new Vector4(uv.x, uv.y, 0, 0));
        drawMaterial.SetFloat("_BrushSize", brushSize);
        
        // Blend new drawing mark into the specific tile's texture
        Graphics.Blit(tempTexture, targetTexture, drawMaterial);
        
        // Clear active render texture
        RenderTexture.active = null;
    }
    
    void DrawTrailLine(Vector2 startUV, Vector2 endUV, RenderTexture targetTexture)
    {
        // Draw a line of points between startUV and endUV
        for (int i = 0; i <= lineSegments; i++)
        {
            float t = (float)i / lineSegments;
            Vector2 pointUV = Vector2.Lerp(startUV, endUV, t);
            DrawTrailMark(pointUV, targetTexture);
        }
    }
    
    void OnDrawGizmos()
    {
        if (!debugMode || !Application.isPlaying || mainCam == null || plantHead == null) return;
        
        // Draw a sphere at plant head
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(plantHead.position, 0.1f);
        
        // Draw lines showing UI rectangles
        Image[] groundImages = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
        foreach (Image groundImage in groundImages)
        {
            if (groundImage.gameObject.CompareTag("Ground"))
            {
                RectTransform rect = groundImage.rectTransform;
                if (rect != null)
                {
                    // Draw rectangle outline
                    Gizmos.color = IsPointOverUIElement(plantHead.position, rect) ? Color.yellow : Color.red;
                    Vector3[] corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    for (int i = 0; i < 4; i++)
                    {
                        Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
                    }
                }
            }
        }
    }
    
    // To handle recycled tiles in BackgroundTileManager
    public void AssignTextureToTile(GameObject tile)
    {
        if (!tile.CompareTag("Ground")) return;
        
        RectTransform rectTransform = tile.GetComponent<RectTransform>();
        Image image = tile.GetComponent<Image>();
        
        if (rectTransform == null || image == null || image.material == null) return;
        
        // Check if this tile already has a texture
        RenderTexture tileTexture;
        if (!tileTextures.TryGetValue(rectTransform, out tileTexture))
        {
            // Create a new texture for this tile
            tileTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);
            tileTexture.name = $"TileTexture_{tile.name}";
            tileTexture.Create();
            tileTextures[rectTransform] = tileTexture;
        }
        
        // Assign the texture to the material
        image.material.SetTexture("_T", tileTexture);
        
        // Set aspect ratio
        float aspectRatio = rectTransform.rect.width / rectTransform.rect.height;
        image.material.SetFloat("_AspectRatio", aspectRatio);
    }
    
    public void ClearTrailTexture(RectTransform rectTransform)
    {
        if (tileTextures.ContainsKey(rectTransform))
        {
            RenderTexture tileTexture = tileTextures[rectTransform];
            
            // Clear the texture to black
            RenderTexture.active = tileTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;
            
            // Also remove from lastUVPositions if present
            if (lastUVPositions.ContainsKey(rectTransform))
            {
                lastUVPositions.Remove(rectTransform);
            }
        }
    }
}