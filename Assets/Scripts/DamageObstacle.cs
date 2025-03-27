using UnityEngine;

public class DamageObstacle : MonoBehaviour
{
    public ObstacleData obstacleData;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    
    private void Awake()
    {
        // Make sure we have a valid obstacle data assigned
        if (obstacleData == null)
        {
            Debug.LogError("No ObstacleData assigned to DamageObstacle!", this);
        }
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Set collider trigger mode based on obstacle type
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
        // Reset color when the obstacle is reactivated
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
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
        // Check if the collider belongs to the plant
        if (other.CompareTag("PlantHead"))
        {
            if (obstacleData == null)
            {
                Debug.LogError("No obstacle data set on DamageObstacle during collision!", this);
                return;
            }
            
            Debug.Log($"Plant contacted obstacle: {gameObject.name}, type: {(obstacleData.isSideObstacle ? "Side" : "Regular")}", this);
            
            // Play collision effect if available
            if (obstacleData.collisionEffectPrefab != null)
            {
                GameObject effect = Instantiate(obstacleData.collisionEffectPrefab, contactPoint, Quaternion.identity);
                Destroy(effect, obstacleData.effectDuration);
            }
            
            // Play collision sound if available
            if (obstacleData.collisionSound != null)
            {
                AudioSource.PlayClipAtPoint(obstacleData.collisionSound, transform.position, obstacleData.volume);
            }
            
            // Change color if enabled
            if (obstacleData.changeColorOnCollision && spriteRenderer != null)
            {
                spriteRenderer.color = obstacleData.collisionColor;
                
                // Restore color after a delay
                Invoke("RestoreColor", 0.5f);
            }
            
            // Notify the plant via PlantLife component
            PlantLife plantLife = other.GetComponentInParent<PlantLife>();
            if (plantLife != null)
            {
                plantLife.HandleCollision(this);
            }
            
            // Notify the plant controller for physics interactions if this is a physical obstacle
            if (obstacleData.isPhysical)
            {
                PlantController plantController = other.GetComponentInParent<PlantController>();
                if (plantController != null)
                {
                  
                    {
                        plantController.HandlePhysicalCollision();
                    }
                }
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