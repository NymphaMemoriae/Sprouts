using UnityEngine;

public class DamageObstacle : MonoBehaviour
{
    [Header("References")]
    public ObstacleData obstacleData;

    [Tooltip("Optional: Only for obstacles with built-in spike burst particle system")]
    public ParticleSystem collisionParticles; // Only assign if this obstacle has particles

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private void Awake()
    {
        if (obstacleData == null)
        {
            Debug.LogError("No ObstacleData assigned to DamageObstacle!", this);
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Automatically set trigger based on obstacle type
        if (obstacleData != null)
        {
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = !obstacleData.isPhysical;
            }
        }
    }

    private void OnEnable()
    {
        // Reset color on reuse
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // Clear any leftover particles (for pooled objects)
        if (collisionParticles != null)
        {
            collisionParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePlantContact(collision.gameObject, collision.contacts[0].point);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandlePlantContact(collision.gameObject, collision.transform.position);
    }

    private void HandlePlantContact(GameObject other, Vector3 contactPoint)
    {
        if (!other.CompareTag("PlantHead")) return;

        if (obstacleData == null)
        {
            Debug.LogError("No obstacle data set on DamageObstacle during collision!", this);
            return;
        }

        Debug.Log($"Plant contacted obstacle: {gameObject.name}, type: {(obstacleData.isSideObstacle ? "Side" : "Regular")}", this);

        // 1. Optional prefab-based VFX (still supported)
        if (obstacleData.collisionEffectPrefab != null)
        {
            GameObject effect = Instantiate(obstacleData.collisionEffectPrefab, contactPoint, Quaternion.identity);
            Destroy(effect, obstacleData.effectDuration);
        }

        // 2. Optional built-in particle burst (like spikes)
        if (collisionParticles != null)
        {
            collisionParticles.Play();
        }

        // 3. Optional sound
        if (obstacleData.collisionSound != null)
        {
            AudioSource.PlayClipAtPoint(obstacleData.collisionSound, transform.position, obstacleData.volume);
        }

        // 4. Optional color flash
        if (obstacleData.changeColorOnCollision && spriteRenderer != null)
        {
            spriteRenderer.color = obstacleData.collisionColor;
            Invoke(nameof(RestoreColor), 0.5f);
        }

        // 5. Notify plant for damage
        PlantLife plantLife = other.GetComponentInParent<PlantLife>();
        if (plantLife != null)
        {
            plantLife.HandleCollision(this);
        }

        // 6. Notify plant for physics behavior
        if (obstacleData.isPhysical)
        {
            PlantController plantController = other.GetComponentInParent<PlantController>();
            if (plantController != null)
            {
                plantController.HandlePhysicalCollision();
            }
        }
    }

    private void RestoreColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}
