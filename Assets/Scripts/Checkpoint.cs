using UnityEngine;

[RequireComponent(typeof(Collider2D))] // Ensure there's a collider
public class Checkpoint : MonoBehaviour
{
    // --- Keep this section for Inspector offset ---
    [Header("Positioning")]
    [Tooltip("Vertical offset relative to the parent tile's pivot.")]
    [SerializeField] private float relativeYOffset = 0f; // Inspector field for Y offset relative to parent tile
    // --- End Section ---

    private bool triggered = false;
    private Collider2D _collider;

    void Awake()
    {
        _collider = GetComponent<Collider2D>();

        // Ensure the collider is set to be a trigger
        if (!_collider.isTrigger)
        {
             Debug.LogWarning($"Checkpoint '{name}' collider was not set to Trigger. Forcing it.", gameObject);
            _collider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the object entering the trigger has the correct tag
        if (!triggered && collision.CompareTag("PlantHead"))
        {
            // --- Save the PARENT TILE's position ---
            if (transform.parent != null)
            {
                GameManager.SetCurrentCheckpointPosition(transform.parent.position); // Save parent's world position
                Debug.Log($"Checkpoint '{name}' activated by {collision.name}. Saving parent tile position: {transform.parent.position}");
            }
            else
            {
                // Fallback if somehow orphaned, though this shouldn't happen with the new logic
                // --- Save the PARENT TILE's position ---
    if (transform.parent != null)
    {
        GameManager.SetCurrentCheckpointPosition(transform.parent.position); // Save parent's world position
        Debug.Log($"Checkpoint '{name}' activated by {collision.name}. Saving parent tile position: {transform.parent.position}");
    }
    else
    {
        // Fallback if somehow orphaned (shouldn't happen now)
        GameManager.SetCurrentCheckpointPosition(transform.position);
        Debug.LogWarning($"Checkpoint '{name}' activated but has no parent! Saving own position: {transform.position}");
    }
    // --- End Position Saving Modification ---
                    Debug.LogWarning($"Checkpoint '{name}' activated but has no parent! Saving own position: {transform.position}");
                }
                // --- End Position Saving Modification ---

                triggered = true; // Prevent this checkpoint from triggering again

                // Optional Feedback
                Renderer rend = GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = Color.green;
                }
                _collider.enabled = false;
            }
        }

    // Getter for the offset needed by BackgroundTileManager when positioning this checkpoint
    public float GetRelativeYOffset()
    {
        return relativeYOffset;
    }
}