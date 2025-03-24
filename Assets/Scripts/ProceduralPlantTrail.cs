using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralPlantTrail : MonoBehaviour
{
    [Header("References")]
    public Transform plantHead;
    public Transform groundAnchor;

    [Header("Trail Settings")]
    public float topOffset = 0.2f;
    public float trailWidth = 0.5f;
    public int framesPerSegment = 30;
    public int maxSegments = 100;

    [Header("Smoothing Settings")]
    [Tooltip("Enables curve-based smoothing between segments")]
    public bool enableSmoothing = true;
    [Range(0, 10)]
    [Tooltip("Number of extra points to add between segments for smoothing")]
    public int extraSmoothingPoints = 3;
    [Range(0.01f, 1.0f)]
    [Tooltip("Minimum distance between points to prevent overlapping")]
    public float minPointDistance = 0.1f;
    [Range(0f, 1f)]
    [Tooltip("Tension parameter for Catmull-Rom spline (0 = more rounded, 1 = more angular)")]
    public float curveTension = 0.5f;

    [Header("Curve Handling")]
    [Tooltip("Prevents overlapping in sharp curves by adjusting width")]
    public bool preventOverlapping = true;
    [Range(0.1f, 1.0f)]
    [Tooltip("Minimum width multiplier for narrow curves (prevents pinching)")]
    public float minWidthMultiplier = 0.5f;
    [Range(0f, 90f)]
    [Tooltip("Angle threshold for detecting sharp curves (degrees)")]
    public float sharpCurveAngle = 45f;
    [Tooltip("Adds a small Z-offset to prevent Z-fighting in overlapping areas")]
    public bool useZOffset = true;
    [Range(0.0001f, 0.01f)]
    [Tooltip("Z-offset amount for preventing Z-fighting")]
    public float zOffsetAmount = 0.001f;

    [Header("Texture Settings")]
    public Material trailMaterial;
    [Tooltip("Size of the texture in world units (how tall each tile should be)")]
    public float textureWorldSize = 1.0f;
    [Tooltip("Flip the texture horizontally")]
    public bool flipTextureHorizontally = false;

    [Header("Visual Stability")]
    [Tooltip("Minimum movement threshold before adding a new segment")]
    public float minMovementThreshold = 0.05f;
    [Tooltip("Adds a small delay before freezing segments to smooth out jitter")]
    public bool useSegmentDelay = true;
    [Range(1, 10)]
    [Tooltip("Frames to wait before freezing a segment position")]
    public int segmentDelayFrames = 2;

    // Trail data
    private List<Vector3> trailPoints = new List<Vector3>();
    private List<Vector3> delayedPoints = new List<Vector3>(); // For segment delay
    private List<int> delayCounters = new List<int>(); // For segment delay
    private List<float> pointYPositions = new List<float>(); // Store world Y position for UV mapping
    private Mesh trailMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private int frameCount = 0;
    private Vector3 lastHeadPosition;
    private float currentZOffset = 0f; // For Z-fighting prevention

    private void Start()
    {
        // Get components
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        // Create mesh
        trailMesh = new Mesh();
        trailMesh.name = "PlantTrailMesh";
        meshFilter.mesh = trailMesh;

        // Assign material
        if (trailMaterial != null)
            meshRenderer.material = trailMaterial;
        
        // Initial setup
        if (plantHead == null || groundAnchor == null)
        {
            Debug.LogError("Plant Head or Ground Anchor is missing!");
            return;
        }
        
        // Initialize with just two points - plant head and ground
        trailPoints.Clear();
        delayedPoints.Clear();
        delayCounters.Clear();
        pointYPositions.Clear();
        
        Vector3 headPos = plantHead.position - Vector3.up * topOffset;
        Vector3 groundPos = groundAnchor.position;
        trailPoints.Add(headPos);
        trailPoints.Add(groundPos);
        
        // Store world Y positions for UV mapping
        pointYPositions.Add(headPos.y);
        pointYPositions.Add(groundPos.y);
        
        // Initialize delayed points if using segment delay
        if (useSegmentDelay)
        {
            delayedPoints.Add(headPos);
            delayCounters.Add(0);
        }
        
        lastHeadPosition = plantHead.position;
        
        // Generate initial mesh
        GenerateMesh();
    }

    private void Update()
    {
        if (plantHead == null || groundAnchor == null)
            return;
            
        // Get current positions
        Vector3 headPos = plantHead.position - Vector3.up * topOffset;
        Vector3 groundPos = groundAnchor.position;
        
        // Calculate movement magnitude
        float movementMagnitude = Vector3.Distance(headPos, lastHeadPosition);
        lastHeadPosition = plantHead.position;
        
        // Always update the top point to follow plant head
        if (trailPoints.Count > 0)
        {
            trailPoints[0] = headPos;
            pointYPositions[0] = headPos.y;
        }
            
        // Always update the bottom point to follow ground
        if (trailPoints.Count > 1)
        {
            trailPoints[trailPoints.Count - 1] = groundPos;
            pointYPositions[pointYPositions.Count - 1] = groundPos.y;
        }
            
        // Update delayed points if using segment delay
        if (useSegmentDelay && delayedPoints.Count > 0)
        {
            delayedPoints[0] = headPos;
            
            // Process delayed points
            for (int i = 0; i < delayCounters.Count; i++)
            {
                delayCounters[i]++;
                
                // If delay threshold reached, freeze this point
                if (delayCounters[i] >= segmentDelayFrames)
                {
                    // If this is not the first point, it's ready to be frozen
                    if (i > 0)
                    {
                        // Update the corresponding point in trailPoints
                        if (i + 1 < trailPoints.Count)
                            trailPoints[i + 1] = delayedPoints[i];
                    }
                }
            }
        }
            
        // Increment frame counter
        frameCount++;
        
        // Every N frames, freeze the current top point and add a new one
        // Or add a point if movement exceeds threshold
        bool shouldAddPoint = frameCount >= framesPerSegment || 
                            (movementMagnitude > minMovementThreshold);
                            
        if (shouldAddPoint)
        {
            frameCount = 0;
            
            // Calculate the required minimum distance based on curve detection
            float requiredDistance = minPointDistance;
            
            // If we have enough points, check if we're in a curve
            if (trailPoints.Count >= 3)
            {
                // Calculate angle to determine if we're in a curve
                float angle = CalculateAngle(
                    trailPoints[0], 
                    trailPoints[1], 
                    trailPoints.Count > 2 ? trailPoints[2] : trailPoints[1] + (trailPoints[1] - trailPoints[0])
                );
                
                // If we're in a curve, increase the required distance to prevent overlapping
                if (angle < sharpCurveAngle)
                {
                    // The sharper the curve, the more spacing we need
                    float curveFactor = Mathf.Clamp01(1.0f - (angle / sharpCurveAngle));
                    requiredDistance = minPointDistance * (1.0f + curveFactor);
                }
            }
            
            // Check if the new point would be far enough from the previous one
            if (trailPoints.Count <= 1 || Vector3.Distance(headPos, trailPoints[1]) > requiredDistance)
            {
                // Add a new point at the top that will follow plant head
                trailPoints.Insert(1, headPos);
                pointYPositions.Insert(1, headPos.y);
                
                // Add to delayed points if using segment delay
                if (useSegmentDelay)
                {
                    delayedPoints.Insert(1, headPos);
                    delayCounters.Insert(1, 0);
                }
                
                // Limit total points
                if (trailPoints.Count > maxSegments)
                {
                    trailPoints.RemoveAt(trailPoints.Count - 2); // Remove second-to-last (preserve ground anchor)
                    pointYPositions.RemoveAt(pointYPositions.Count - 2); // Remove corresponding Y position
                    
                    // Also remove from delayed points if using segment delay
                    if (useSegmentDelay && delayedPoints.Count > maxSegments - 1)
                    {
                        delayedPoints.RemoveAt(delayedPoints.Count - 2);
                        delayCounters.RemoveAt(delayCounters.Count - 2);
                    }
                }
            }
        }
            
        // Update the mesh
        GenerateMesh();
    }
    
    // Creates a smooth path using Catmull-Rom spline interpolation
    private List<Vector3> GenerateSmoothPath(List<Vector3> points, out List<float> smoothedYPositions)
    {
        smoothedYPositions = new List<float>();
        
        if (points.Count < 3)
        {
            smoothedYPositions.AddRange(pointYPositions);
            return new List<Vector3>(points);
        }
            
        List<Vector3> smoothedPoints = new List<Vector3>();
        
        // Add the first point
        smoothedPoints.Add(points[0]);
        smoothedYPositions.Add(pointYPositions[0]);
        
        // Generate smooth points between each pair of original points
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 p0 = (i > 0) ? points[i - 1] : points[i];
            Vector3 p1 = points[i];
            Vector3 p2 = points[i + 1];
            Vector3 p3 = (i < points.Count - 2) ? points[i + 2] : p2 + (p2 - p1);
            
            float y1 = pointYPositions[i];
            float y2 = pointYPositions[i + 1];
            
            // Calculate angle to determine if we're in a curve
            float angle = CalculateAngle(p0, p1, p2);
            
            // Adjust number of interpolation points based on curve sharpness
            int pointsToAdd = extraSmoothingPoints;
            if (angle < sharpCurveAngle)
            {
                // Reduce number of points in sharp curves to prevent overlapping
                float curveFactor = Mathf.Clamp01(1.0f - (angle / sharpCurveAngle));
                pointsToAdd = Mathf.Max(1, Mathf.FloorToInt(extraSmoothingPoints * (1.0f - curveFactor * 0.5f)));
            }
            
            // Add extra points between p1 and p2 using Catmull-Rom interpolation
            for (int j = 1; j <= pointsToAdd; j++)
            {
                float t = j / (float)(pointsToAdd + 1);
                Vector3 smoothedPoint = CatmullRomPoint(p0, p1, p2, p3, t, curveTension);
                
                // Interpolate Y position linearly
                float smoothedY = Mathf.Lerp(y1, y2, t);
                
                // Ensure minimum distance between points
                if (smoothedPoints.Count == 0 || Vector3.Distance(smoothedPoint, smoothedPoints[smoothedPoints.Count - 1]) >= minPointDistance)
                {
                    smoothedPoints.Add(smoothedPoint);
                    smoothedYPositions.Add(smoothedY);
                }
            }
            
            // Add the original point (except for the first one which is already added)
            if (i < points.Count - 2)
            {
                if (smoothedPoints.Count == 0 || Vector3.Distance(p2, smoothedPoints[smoothedPoints.Count - 1]) >= minPointDistance)
                {
                    smoothedPoints.Add(p2);
                    smoothedYPositions.Add(pointYPositions[i + 1]);
                }
            }
        }
        
        // Add the last point
        smoothedPoints.Add(points[points.Count - 1]);
        smoothedYPositions.Add(pointYPositions[pointYPositions.Count - 1]);
        
        return smoothedPoints;
    }
    
    // Catmull-Rom spline interpolation
    private Vector3 CatmullRomPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float tension)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        
        float a = (1 - tension) / 2f;
        
        float a0 = -a * t3 + 2 * a * t2 - a * t;
        float a1 = (2 - a) * t3 + (a - 3) * t2 + 1;
        float a2 = (a - 2) * t3 + (3 - 2 * a) * t2 + a * t;
        float a3 = a * t3 - a * t2;
        
        return a0 * p0 + a1 * p1 + a2 * p2 + a3 * p3;
    }
    
    // Calculate the angle between three points
    private float CalculateAngle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 v1 = p1 - p2;
        Vector3 v2 = p3 - p2;
        return Vector3.Angle(v1, v2);
    }
    
    // Calculate width multiplier based on curve sharpness
    private float CalculateWidthMultiplier(Vector3[] points, int index)
    {
        if (!preventOverlapping || points.Length < 3 || index <= 0 || index >= points.Length - 1)
            return 1.0f;
            
        // Calculate angle at this point
        float angle = CalculateAngle(points[index - 1], points[index], points[index + 1]);
        
        // If angle is sharp, reduce width to prevent overlapping
        if (angle < sharpCurveAngle)
        {
            // Use a smoother transition curve (easeOutQuad) instead of linear
            float t = angle / sharpCurveAngle;
            float easeOutValue = t * (2 - t); // Quadratic ease-out
            
            // Apply the eased value to get a smoother width transition
            return Mathf.Lerp(minWidthMultiplier, 1.0f, easeOutValue);
        }
        
        return 1.0f;
    }
    
    private void GenerateMesh()
    {
        if (trailPoints.Count < 2)
            return;

        // Create a smooth path if enabled, otherwise use the original points
        List<float> yPositions;
        List<Vector3> pointsToUse;
        
        if (enableSmoothing)
        {
            pointsToUse = GenerateSmoothPath(trailPoints, out yPositions);
            }
            else
        {
            pointsToUse = new List<Vector3>(trailPoints);
            yPositions = new List<float>(pointYPositions);
        }
        
        // Convert to array for easier indexing
        Vector3[] pointsArray = pointsToUse.ToArray();
        float[] yPositionsArray = yPositions.ToArray();
            
        // Create vertices and UVs arrays
        Vector3[] vertices = new Vector3[pointsArray.Length * 2];
        Vector2[] uvs = new Vector2[pointsArray.Length * 2];
        
        // Reset Z offset
        currentZOffset = 0f;
        
        // Find the highest and lowest Y positions for UV mapping
        float highestY = yPositionsArray[0]; // Top point
        float lowestY = yPositionsArray[yPositionsArray.Length - 1]; // Bottom point
        
        for (int i = 0; i < pointsArray.Length; i++)
        {
            // Calculate direction for this point
            Vector3 direction;
            if (i == 0) // Top point
                direction = (pointsArray[1] - pointsArray[0]).normalized;
            else if (i == pointsArray.Length - 1) // Bottom point
                direction = (pointsArray[i] - pointsArray[i - 1]).normalized;
            else // Middle points
                direction = ((pointsArray[i + 1] - pointsArray[i - 1]) * 0.5f).normalized;
                
            // Calculate perpendicular vector for width
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;

            // Calculate width multiplier based on curve sharpness
            float widthMultiplier = CalculateWidthMultiplier(pointsArray, i);
            
            // Calculate width for this point
            float pointWidth = trailWidth * widthMultiplier;
            
            // Apply Z-offset to prevent Z-fighting if enabled
            Vector3 zOffset = Vector3.zero;
            if (useZOffset)
            {
                // Gradually increase Z-offset for each point to ensure proper layering
                currentZOffset += zOffsetAmount;
                zOffset = new Vector3(0, 0, currentZOffset);
            }
            
            // Convert world positions to local space for the mesh
            Vector3 centerPoint = transform.InverseTransformPoint(pointsArray[i]);
            Vector3 leftPoint = transform.InverseTransformPoint(pointsArray[i] + perpendicular * (pointWidth * 0.5f)) + zOffset;
            Vector3 rightPoint = transform.InverseTransformPoint(pointsArray[i] - perpendicular * (pointWidth * 0.5f)) + zOffset;
            
            // Store vertices in local space
            vertices[i * 2] = leftPoint;
            vertices[i * 2 + 1] = rightPoint;
            
            // Calculate UV based on world Y position
            // This ensures consistent tiling regardless of segment length or curve
            float worldY = yPositionsArray[i];
            
            // Map Y position to UV space (0-1 range per texture tile)
            float uvY = (worldY - lowestY) / textureWorldSize;
            // Ensure we're using the fractional part for tiling
            uvY = uvY % 1.0f;
            
            // Apply horizontal tiling
            float uvLeft = flipTextureHorizontally ? 1 : 0;
            float uvRight = flipTextureHorizontally ? 0 : 1;
            
            uvs[i * 2] = new Vector2(uvLeft, uvY);
            uvs[i * 2 + 1] = new Vector2(uvRight, uvY);
        }

        // Create triangles
        int[] triangles = new int[(pointsArray.Length - 1) * 6];
        int triIndex = 0;
        
        for (int i = 0; i < pointsArray.Length - 1; i++)
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
        
        // Update mesh
        trailMesh.Clear();
        trailMesh.vertices = vertices;
        trailMesh.uv = uvs;
        trailMesh.triangles = triangles;
        trailMesh.RecalculateNormals();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Draw the trail points
        if (trailPoints.Count > 0)
        {
            // Draw each point
            for (int i = 0; i < trailPoints.Count; i++)
            {
                Gizmos.color = (i == 0) ? Color.red : (i == trailPoints.Count - 1) ? Color.green : Color.white;
                Gizmos.DrawSphere(trailPoints[i], 0.1f);
                
                // Draw connecting lines
                if (i < trailPoints.Count - 1)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(trailPoints[i], trailPoints[i + 1]);
                }
            }
            
            // Draw delayed points if using segment delay
            if (useSegmentDelay && delayedPoints.Count > 0)
            {
                for (int i = 0; i < delayedPoints.Count; i++)
                {
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange, semi-transparent
                    Gizmos.DrawWireSphere(delayedPoints[i], 0.08f);
                }
            }
        }
    }
#endif
}
