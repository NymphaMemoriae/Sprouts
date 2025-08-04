using UnityEngine;

public class DamageObstacle : MonoBehaviour
{
    [Header("References")]
    public ObstacleData obstacleData;

    [Tooltip("Optional: Only for obstacles with built-in spike burst particle system")]
    public ParticleSystem collisionParticles; // Only assign if this obstacle has particles

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float lastSoundTime = -1f; // Tracks when the sound was last played for this instance

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
         lastSoundTime = -1f;
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


        if (obstacleData.collisionEffectPrefab != null)
        {
            GameObject effect = Instantiate(obstacleData.collisionEffectPrefab, contactPoint, Quaternion.identity);
            Destroy(effect, obstacleData.effectDuration);
        }

        if (collisionParticles != null)
        {
            collisionParticles.Play();
        }

    
        if (obstacleData.collisionSound != null)
        {
            if (Time.time >= lastSoundTime + obstacleData.soundCooldown)
            {
                
                lastSoundTime = Time.time; 
                
            
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(obstacleData.collisionSound, obstacleData.volume);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(obstacleData.collisionSound, transform.position, obstacleData.volume);
                    Debug.LogWarning("[DamageObstacle] AudioManager not found. Playing sound without mixer control.");
                }
            }
        }

   
        if (obstacleData.changeColorOnCollision && spriteRenderer != null)
        {
            spriteRenderer.color = obstacleData.collisionColor;
            Invoke(nameof(RestoreColor), 0.5f);
        }

        // Gameplay, tells the PlantLife script to handle the damage.
        PlantLife plantLife = other.GetComponentInParent<PlantLife>();
        if (plantLife != null)
        {
            plantLife.HandleCollision(this);
        }

        
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
