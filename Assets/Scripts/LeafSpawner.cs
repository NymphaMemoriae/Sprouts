using UnityEngine;

public class LeafSpawner : MonoBehaviour
{
    [Header("Leaf Settings")]
    public GameObject leafPrefab;                 // The leaf prefab to spawn
    public float minDistanceBetweenLeaves = 1.0f; // Minimum distance between leaf spawns
    public float maxDistanceBetweenLeaves = 3.0f; // Maximum distance between leaf spawns
    
    [Header("Leaf Appearance")]
    public float minLeafScale = 0.8f;             // Minimum scale for spawned leaves
    public float maxLeafScale = 1.2f;             // Maximum scale for spawned leaves
    
    [Header("Debug")]
    public bool showDebugGizmos = false;          // Toggle to show debug visualization

    private float distanceSinceLastLeaf = 0f;     // Current accumulated distance
    private float nextLeafDistance;               // Target distance for next leaf spawn
    private Vector3 lastTipPosition;              // Last known position of the stem tip

    private void Start()
    {
        // Initialize with a random distance for the first leaf
        nextLeafDistance = Random.Range(minDistanceBetweenLeaves, maxDistanceBetweenLeaves);
        
        // If we have a reference to the stem, initialize the last position
        ProceduralStem stem = GetComponent<ProceduralStem>();
        if (stem != null && stem.plantHead != null)
        {
            lastTipPosition = stem.plantHead.position - Vector3.up * stem.topOffset;
        }
    }

    /// <summary>
    /// Call this method from the ProceduralStem script whenever the stem grows
    /// </summary>
    /// <param name="oldTipPosition">Previous position of the stem tip</param>
    /// <param name="newTipPosition">Current position of the stem tip</param>
    /// <param name="stemDirection">Current direction of the stem growth</param>
    public void UpdateLeafSpawner(Vector3 oldTipPosition, Vector3 newTipPosition, Vector3 stemDirection)
    {
        // Calculate how much the stem has grown
        float distanceGrown = Vector3.Distance(oldTipPosition, newTipPosition);
        
        // Add to our accumulated distance
        distanceSinceLastLeaf += distanceGrown;
        
        // Check if we've grown enough to spawn a new leaf
        if (distanceSinceLastLeaf >= nextLeafDistance)
        {
            // Spawn a leaf
            SpawnLeaf(newTipPosition, stemDirection);
            
            // Reset the counter and pick a new random distance for the next leaf
            distanceSinceLastLeaf = 0f;
            nextLeafDistance = Random.Range(minDistanceBetweenLeaves, maxDistanceBetweenLeaves);
        }
        
        // Update the last tip position for next frame
        lastTipPosition = newTipPosition;
    }

    /// <summary>
    /// Spawns a leaf at the given position with proper orientation
    /// </summary>
    private void SpawnLeaf(Vector3 position, Vector3 stemDirection)
    {
        if (leafPrefab == null)
        {
            Debug.LogError("Leaf prefab is not assigned!");
            return;
        }

        // Normalize the stem direction
        stemDirection = stemDirection.normalized;
        
        // Calculate perpendicular vector (in 2D)
        Vector3 perpendicular = Vector3.Cross(stemDirection, Vector3.forward).normalized;
        
        // Randomly flip the direction (left or right of the stem)
        if (Random.value > 0.5f)
        {
            perpendicular = -perpendicular;
        }
        
        // Create the leaf
        GameObject leaf = Instantiate(leafPrefab, position, Quaternion.identity);
        
        // Calculate rotation so the leaf stem points toward the main stem
        // and the leaf body points outward
        float angle = Mathf.Atan2(perpendicular.y, perpendicular.x) * Mathf.Rad2Deg;
        leaf.transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // Randomize the scale
        float randomScale = Random.Range(minLeafScale, maxLeafScale);
        leaf.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
        
        // Optional: Parent the leaf to this object for organization
        leaf.transform.parent = transform;
    }

    // Optional: Visualization for debugging
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        Gizmos.color = Color.green;
        if (lastTipPosition != Vector3.zero)
        {
            Gizmos.DrawWireSphere(lastTipPosition, 0.1f);
            Gizmos.DrawLine(lastTipPosition, lastTipPosition + Vector3.up * nextLeafDistance);
        }
    }
}
