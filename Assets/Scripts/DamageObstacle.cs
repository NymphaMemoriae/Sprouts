using UnityEngine;

public class DamageObstacle : MonoBehaviour
{
    public ObstacleData obstacleData { get; private set; }
    private GameObject collisionEffectPrefab;
    private float effectDuration;
    private AudioClip collisionSound;
    private float volume;
    
    public void SetObstacleData(ObstacleData data)
    {
        obstacleData = data;
        collisionEffectPrefab = data.collisionEffectPrefab;
        effectDuration = data.effectDuration;
        collisionSound = data.collisionSound;
        volume = data.volume;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the collider belongs to the plant
        if (collision.gameObject.CompareTag("PlantHead"))
        {
            // Play collision effect if available
            if (collisionEffectPrefab != null)
            {
                GameObject effect = Instantiate(collisionEffectPrefab, collision.contacts[0].point, Quaternion.identity);
                Destroy(effect, effectDuration);
            }
            
            // Play collision sound if available
            if (collisionSound != null)
            {
                AudioSource.PlayClipAtPoint(collisionSound, transform.position, volume);
            }
        }
    }
}
