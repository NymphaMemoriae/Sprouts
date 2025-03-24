using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [SerializeField] private PlantController plantController;
    [SerializeField] private float verticalOffset = 2f;
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private float recenterDelay = 1f;
    
    private Vector3 velocity = Vector3.zero;
    private float highestYPosition = 0f;
    private bool isRecenteringScheduled = false;
    
    private void Start()
    {
        // Initialize the highest position to the starting position
        if (plantController != null && plantController.PlantHead != null)
        {
            highestYPosition = plantController.PlantHead.position.y;
        }
        
        // Debug log to verify references
        Debug.Log("CameraController initialized. PlantController reference: " + (plantController != null));
        if (plantController != null)
        {
            Debug.Log("PlantHead reference: " + (plantController.PlantHead != null));
        }
    }
    
    private void LateUpdate()
    {
        // Debug the game state
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null! Make sure GameManager exists in the scene.");
            return;
        }
        
        // Always follow in editor mode for testing, or when game is playing
        bool shouldFollow = Application.isEditor || GameManager.Instance.CurrentGameState == GameState.Playing;
        
        if (!shouldFollow)
        {
            Debug.Log("Camera not following because game state is: " + GameManager.Instance.CurrentGameState);
            return;
        }
        
        if (plantController == null || plantController.PlantHead == null)
        {
            Debug.LogError("PlantController or PlantHead reference is missing!");
            return;
        }
            
        // Get the plant head position
        Vector3 targetPosition = plantController.PlantHead.position;
        
        // Add vertical offset
        targetPosition.y += verticalOffset;
        
        // Keep the camera's current x and z positions
        targetPosition.x = transform.position.x;
        targetPosition.z = transform.position.z;
        
        // MODIFIED: Always follow the plant if it's higher than our current position
        // This ensures we don't miss any movement
        if (targetPosition.y > transform.position.y)
        {
            highestYPosition = targetPosition.y;
            
            // If the plant is not stuck, follow it
            if (!plantController.IsStuck)
            {
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
            }
        }
        
        // If the plant is stuck but not already scheduled for recentering
        if (plantController.IsStuck && !isRecenteringScheduled)
        {
            StartCoroutine(RecenterAfterDelay());
        }
    }
    
    private IEnumerator RecenterAfterDelay()
    {
        isRecenteringScheduled = true;
        
        yield return new WaitForSeconds(recenterDelay);
        
        // If the plant is no longer stuck, recenter the camera
        if (plantController != null && !plantController.IsStuck)
        {
            Vector3 targetPosition = plantController.PlantHead.position;
            targetPosition.y += verticalOffset;
            targetPosition.x = transform.position.x;
            targetPosition.z = transform.position.z;
            
            // Update the highest position if needed
            if (targetPosition.y > highestYPosition)
            {
                highestYPosition = targetPosition.y;
            }
            
            // Smoothly move to the target position
            float elapsedTime = 0f;
            Vector3 startPosition = transform.position;
            
            while (elapsedTime < smoothTime)
            {
                transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / smoothTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            transform.position = targetPosition;
        }
        
        isRecenteringScheduled = false;
    }
}