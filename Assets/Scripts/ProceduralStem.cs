using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralStem : MonoBehaviour
{
    [Header("Anchors")]
    public Transform groundAnchor;    // Bottom anchor (doesn't move)
    public Transform plantHead;       // Top anchor (moves)

    [Header("Basic Settings")]
    public float topOffset = 0.2f;    // Offset below the head
    public float stemWidth = 0.5f;    // Overall width of the stem
    public int framesPerSegment = 30; // Wait this many frames before adding a new segment
    public float minMovementThreshold = 0.05f; // If the head moves this much, add a new segment

    [Header("Freezing")]
    public bool useSegmentDelay = true;    // If true, newly created points follow the head for 'segmentDelayFrames'
    public int segmentDelayFrames = 2;     // After these frames, the segment is frozen

    [Header("Smoothing")]
    public bool enableSmoothing = false; // Toggle to enable simple smoothing
    [Range(1, 8)]
    public int smoothingIterations = 2;  // How many times to smooth the final chain
    [Range(0f, 1f)]
    public float smoothingStrength = 0.5f; // How strong the smoothing effect is

    [Header("Texture")]
    public Material stemMaterial;
    public float horizontalTiling = 1f; // Repeat horizontally
    public float verticalTiling = 1f;   // Repeat vertically
    public bool useTextureClamp = true; // Whether to use clamp mode for textures

    [Header("Seam Prevention")]
    [Range(0f, 0.1f)]
    public float segmentOverlap = 0.01f; // Slight overlap between segments to prevent gaps
    [Range(0f, 0.01f)]
    public float uvPadding = 0.001f;     // Small padding in UV space to avoid edge sampling

    [Header("Visual")]
    public bool useZOffset = true;
    [Range(0.0001f, 0.01f)]
    public float zOffsetAmount = 0.001f;
    public bool newerSegmentsOnTop = true;  // New option to control layering behavior

    [Header("Leaf Generation")]
    public LeafSpawner leafSpawner;  // Reference to the LeafSpawner component

    // Internal data
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh stemMesh;

    // The chain of points representing the stem (newest at front, oldest at back)
    private List<Vector3> stemPoints = new List<Vector3>();
    private List<int> freezeCounters = new List<int>(); // Keep track of frames for which each segment was active
    private List<Vector2> fixedUVs = new List<Vector2>(); // Store fixed UVs for each vertex

    private int frameCounter = 0;
    private Vector3 lastHeadPos;
    private float currentZOffset = 0f;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (stemMaterial != null)
            meshRenderer.material = stemMaterial;

        stemMesh = new Mesh { name = "ProceduralStem" };
        meshFilter.mesh = stemMesh;

        // Verify references
        if (!groundAnchor || !plantHead)
        {
            Debug.LogError("Assign groundAnchor and plantHead in the inspector.");
            enabled = false;
            return;
        }

        // Initialize with 2 points
        Vector3 topPos = plantHead.position - Vector3.up * topOffset;
        Vector3 bottomPos = groundAnchor.position;

        stemPoints.Clear();
        freezeCounters.Clear();
        fixedUVs.Clear();

        stemPoints.Add(topPos);
        freezeCounters.Add(0); // This top can move if useSegmentDelay = true
        // Initialize UVs for first segment
        fixedUVs.Add(new Vector2(0, 0));
        fixedUVs.Add(new Vector2(horizontalTiling, 0));

        stemPoints.Add(bottomPos);
        freezeCounters.Add(int.MaxValue); // The bottom never moves
        // Initialize UVs for bottom segment
        fixedUVs.Add(new Vector2(0, 0));
        fixedUVs.Add(new Vector2(horizontalTiling, 0));

        lastHeadPos = plantHead.position;

        // Apply texture wrap mode if needed
        if (useTextureClamp && stemMaterial != null && stemMaterial.mainTexture != null)
        {
            stemMaterial.mainTexture.wrapMode = TextureWrapMode.Clamp;
        }

        // Build initial mesh
        UpdateMesh();
    }

    void Update()
    {
        if (!groundAnchor || !plantHead) return;

        // Store the old top position for leaf spawning
        Vector3 oldTopPos = stemPoints[0];

        // Update bottom anchor
        stemPoints[stemPoints.Count - 1] = groundAnchor.position;

        // If useSegmentDelay, let the top point follow the head until it "freezes"
        if (useSegmentDelay)
        {
            freezeCounters[0]++;
            // If it's not frozen yet, follow the head
            if (freezeCounters[0] < segmentDelayFrames)
            {
                Vector3 headPos = plantHead.position - Vector3.up * topOffset;
                stemPoints[0] = headPos;
            }
        }
        else
        {
            // If no delay, the top point is effectively always "frozen" once placed,
            // so each update we move the existing top to the head
            Vector3 headPos = plantHead.position - Vector3.up * topOffset;
            stemPoints[0] = headPos;
        }

        // Check movement & time to see if we need a new segment
        frameCounter++;
        float headMove = Vector3.Distance(plantHead.position, lastHeadPos);
        if (frameCounter >= framesPerSegment || headMove > minMovementThreshold)
        {
            frameCounter = 0;
            lastHeadPos = plantHead.position;

            // Freeze the old top
            freezeCounters[0] = int.MaxValue;

            // Insert a new top
            Vector3 newTopPos = plantHead.position - Vector3.up * topOffset;
            
            // Calculate stem direction for leaf orientation
            Vector3 stemDirection = (newTopPos - oldTopPos).normalized;
            
            // Notify the leaf spawner about the stem growth
            if (leafSpawner != null)
            {
                leafSpawner.UpdateLeafSpawner(oldTopPos, newTopPos, stemDirection);
            }
            
            stemPoints.Insert(0, newTopPos);
            freezeCounters.Insert(0, 0);

            // Calculate UV for new segment based on the previous top segment
            float prevV = fixedUVs[0].y;
            float segLen = Vector3.Distance(newTopPos, oldTopPos);
            float newV = prevV + segLen * verticalTiling;
            
            // Insert UVs for the new segment
            fixedUVs.Insert(0, new Vector2(0, newV));
            fixedUVs.Insert(1, new Vector2(horizontalTiling, newV));
        }

        // Rebuild mesh every frame (you could optimize by only rebuilding when a segment is added/updated)
        UpdateMesh();
    }

    private void UpdateMesh()
    {
        if (stemPoints.Count < 2) return;

        // Make a working copy if we want to apply smoothing
        List<Vector3> workingPoints = new List<Vector3>(stemPoints);

        if (enableSmoothing && stemPoints.Count > 2)
            SmoothPoints(workingPoints, smoothingIterations, smoothingStrength);

        // Create arrays
        int count = workingPoints.Count;
        Vector3[] vertices = new Vector3[count * 2];
        Vector2[] uvs = new Vector2[count * 2];
        int[] triangles = new int[(count - 1) * 6];

        currentZOffset = 0f;

        for (int i = 0; i < count; i++)
        {
            // Direction for width orientation
            Vector3 dir;
            if (i == 0)
                dir = (workingPoints[1] - workingPoints[0]).normalized;
            else if (i == count - 1)
                dir = (workingPoints[i] - workingPoints[i - 1]).normalized;
            else
                dir = ((workingPoints[i + 1] - workingPoints[i - 1]) * 0.5f).normalized;

            Vector3 perp = Vector3.Cross(dir, Vector3.forward).normalized;

            // Z offset - modified to ensure newer segments are on top
            Vector3 offsetZ = Vector3.zero;
            if (useZOffset)
            {
                // If newerSegmentsOnTop is true, we apply larger z offsets to newer segments
                // This ensures they render on top when overlapping with older segments
                if (newerSegmentsOnTop)
                {
                    // Index 0 is newest, so we want it to have the largest offset
                    offsetZ.z = zOffsetAmount * (count - i);
                }
                else
                {
                    // Original behavior - incremental offset
                    currentZOffset += zOffsetAmount;
                    offsetZ.z = currentZOffset;
                }
            }

            // Apply slight overlap to prevent seams
            float overlapFactor = 1.0f;
            if (i > 0 && i < count - 1) 
            {
                // Extend vertices slightly to create overlap with adjacent segments
                overlapFactor = 1.0f + segmentOverlap;
            }

            Vector3 centerLocal = transform.InverseTransformPoint(workingPoints[i]);
            Vector3 leftLocal = transform.InverseTransformPoint(workingPoints[i] + perp * (stemWidth * 0.5f * overlapFactor)) + offsetZ;
            Vector3 rightLocal = transform.InverseTransformPoint(workingPoints[i] - perp * (stemWidth * 0.5f * overlapFactor)) + offsetZ;

            vertices[i * 2] = leftLocal;
            vertices[i * 2 + 1] = rightLocal;

            // Apply UV padding to avoid edge sampling artifacts
            float uMin = 0f + uvPadding;
            float uMax = horizontalTiling - uvPadding;
            
            // Use stored UVs with padding applied
            uvs[i * 2] = new Vector2(uMin, fixedUVs[i * 2].y);
            uvs[i * 2 + 1] = new Vector2(uMax, fixedUVs[i * 2 + 1].y);
        }

        int triIndex = 0;
        for (int i = 0; i < count - 1; i++)
        {
            int baseIndex = i * 2;
            // First triangle
            triangles[triIndex++] = baseIndex;
            triangles[triIndex++] = baseIndex + 2;
            triangles[triIndex++] = baseIndex + 1;
            // Second triangle
            triangles[triIndex++] = baseIndex + 1;
            triangles[triIndex++] = baseIndex + 2;
            triangles[triIndex++] = baseIndex + 3;
        }

        stemMesh.Clear();
        stemMesh.vertices = vertices;
        stemMesh.uv = uvs;
        stemMesh.triangles = triangles;
        stemMesh.RecalculateNormals();
    }

    /// <summary>
    /// Simple smoothing of the points: each iteration, we shift middle points towards
    /// the average of neighbors. This is a simplistic technique for a bit of "rounded" shape.
    /// </summary>
    private void SmoothPoints(List<Vector3> pts, int iterations, float strength)
    {
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            for (int i = 1; i < pts.Count - 1; i++)
            {
                // Weighted average between current point and midpoint of neighbors
                Vector3 neighborMid = (pts[i - 1] + pts[i + 1]) * 0.5f;
                Vector3 moveDir = neighborMid - pts[i];
                pts[i] += moveDir * strength;
            }
        }
    }

    // Add this method to recalculate UVs for the entire stem
    public void RecalculateUVs()
    {
        if (stemPoints.Count < 2) return;
        
        // Clear existing UVs
        fixedUVs.Clear();
        
        // Calculate total stem length for proper UV mapping
        float totalLength = 0f;
        for (int i = 0; i < stemPoints.Count - 1; i++)
        {
            totalLength += Vector3.Distance(stemPoints[i], stemPoints[i + 1]);
        }
        
        // Assign UVs based on normalized distance along the stem
        float currentLength = 0f;
        for (int i = 0; i < stemPoints.Count; i++)
        {
            float v = 0f;
            if (i > 0)
            {
                currentLength += Vector3.Distance(stemPoints[i-1], stemPoints[i]);
                v = (currentLength / totalLength) * verticalTiling;
            }
            
            fixedUVs.Add(new Vector2(0, v));
            fixedUVs.Add(new Vector2(horizontalTiling, v));
        }
        
        // Update the mesh with new UVs
        UpdateMesh();
    }
}
