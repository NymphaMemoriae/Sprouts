using UnityEngine;

[RequireComponent(typeof(Collider2D))] // Ensure there's a collider
public class Checkpoint : MonoBehaviour
{
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
        // Check if the object entering the trigger has the correct tag (adjust "PlantHead" if needed)
        if (!triggered && collision.CompareTag("PlantHead"))
        {
            // Call the static GameManager method to record the checkpoint position
            GameManager.SetCurrentCheckpointPosition(transform.position);
            triggered = true; // Prevent this checkpoint from triggering again

            // --- Optional Feedback & Cleanup ---
            // Example: Log activation
            Debug.Log($"Checkpoint '{name}' activated by {collision.name} at {transform.position}");

            // Example: Change color to show it's activated
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.green; // Use SetColor if using URP/HDRP with specific shader properties
            }

            // Example: Disable the collider so it can't be triggered again
             _collider.enabled = false;

            // Example: Destroy the checkpoint object after a delay
            // Destroy(gameObject, 2f);
            // --- End of Optional ---
        }
    }

    // The ResetTrigger method has been completely removed as per the previous instructions.
}