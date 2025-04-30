using UnityEngine;

[RequireComponent(typeof(Collider2D))] // Ensure there's a collider
public class Checkpoint : MonoBehaviour
{
    public BiomeData Biome { get; set; } // To potentially know which biome this belongs to
    public Vector3 SpawnPosition { get; private set; } // Store the precise position

    private bool triggered = false;
    private Collider2D _collider;

    void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true; // Make sure it's a trigger
        SpawnPosition = transform.position; // Store position on awake
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the player's head (or main body part) entered the trigger
        // Adjust the tag "PlayerHead" if your player has a different tag.
        if (!triggered && collision.CompareTag("PlayerHead")) // Assuming PlayerHead tag exists
        {
            Debug.Log($"Checkpoint reached for Biome: {(Biome != null ? Biome.biomeName : "Unknown")} at {SpawnPosition}");
            GameManager.Instance.SetCurrentCheckpoint(this);
            triggered = true; // Prevent multiple triggers

            // Optional: Add visual/audio feedback here
        }
    }

    // Optional: Call this if you want to reuse checkpoints or reset their state
    public void ResetTrigger()
    {
        triggered = false;
    }
}