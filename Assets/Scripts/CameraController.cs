using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [SerializeField] private PlantController plantController;
    [SerializeField] private float verticalOffset = 2f;
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private float recenterDelay = 1f;
    // --- New variables for Stagnation and Camera Push ---
    [Header("Stagnation & Push Settings")]
    [Tooltip("Time in seconds the plant must be stagnant for the camera to start pushing.")]
    [SerializeField] private float stagnationTimeThreshold = 2f;
    [Tooltip("Height difference tolerance to consider the plant stagnant.")]
    [SerializeField] private float heightStagnationTolerance = 0.1f;
    [Tooltip("Time in seconds the plant must be off-screen (below camera) during push mode before game over.")]
    [SerializeField] private float plantOffScreenGameOverDelay = 0.5f;

    private enum CameraPushState { Normal, PushingPlayer, PlantIsOffScreenGameOverSequence }
    private CameraPushState currentPushState = CameraPushState.Normal;

    private float lastPlantHeightForStagnation;
    private float stagnationTimer;
    private float plantOffScreenTimer;
    // --- End of new variables ---
    private Vector3 velocity = Vector3.zero;
    private float highestYPosition = 0f;
    private bool isRecenteringScheduled = false;
    private Camera mainCamera; 
    [Header("Camera Lerp Settings")]
    [SerializeField] private float cameraPushLerpFactor = 5f; // Adjust for smoother/sharper lerp
    private float currentActualPushSpeed = 0f;
    private float targetCameraPushSpeed = 0f;

    
    private void Start()
    {
        mainCamera = Camera.main;
        // Initialize the highest position to the starting position
        if (plantController != null && plantController.PlantHead != null)
        {
            highestYPosition = plantController.PlantHead.position.y;
            lastPlantHeightForStagnation = plantController.DisplayHeight; // Initialize for stagnation check
        }
        else
        {
            Debug.LogError("[CameraController] PlantController or PlantHead reference is missing at Start!");
            enabled = false; // Disable component if critical references are missing
            return;
        }
        // Debug log to verify references
        Debug.Log("CameraController initialized. PlantController reference: " + (plantController != null));
        if (plantController != null)
        {
            Debug.Log("PlantHead reference: " + (plantController.PlantHead != null));
        }
        ResetCameraPushState();
    }
    


// AND REPLACE IT WITH THIS:
private void LateUpdate()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[CameraController] GameManager.Instance is null!");
            return;
        }

        if (plantController == null || plantController.PlantHead == null)
        {
            Debug.LogError("[CameraController] PlantController or PlantHead reference is missing in LateUpdate!");
            return;
        }

        // Only run active camera logic if the game is playing or in editor for testing
        // Your original 'shouldFollow' logic for game state:
        bool isActiveState = Application.isEditor || GameManager.Instance.CurrentGameState == GameState.Playing;

        if (!isActiveState)
        {
            // If game is not playing, but was previously in a push state, consider resetting it
            // or let ResetCameraPushState handle it on game restart/scene load.
            // For now, just prevent updates if not in an active playing state.
            Debug.Log("[CameraController] Not updating camera logic because game state is: " + GameManager.Instance.CurrentGameState);
            return;
        }

        // --- Stagnation Detection (only in Normal camera state) ---
        if (currentPushState == CameraPushState.Normal)
        {
            if (Mathf.Abs(plantController.DisplayHeight - lastPlantHeightForStagnation) < heightStagnationTolerance)
            {
                stagnationTimer += Time.deltaTime;
            }
            else
            {
                stagnationTimer = 0f; // Reset if plant moved enough
                lastPlantHeightForStagnation = plantController.DisplayHeight;
            }

            if (stagnationTimer >= stagnationTimeThreshold)
            {
                Debug.Log("[CameraController] Stagnation detected! Camera will start pushing.");
                if (currentPushState == CameraPushState.Normal) // Ensure we are coming from Normal
                {
                    currentPushState = CameraPushState.PushingPlayer; // State changes
                    targetCameraPushSpeed = plantController.GetInitialGrowthSpeed(); // Set target for lerp
                    // currentActualPushSpeed will lerp towards this in HandlePushingPlayerCameraMovement
                }
                stagnationTimer = 0f; 
                if(isRecenteringScheduled) { // Cancel any pending recenter
                    StopCoroutine(RecenterAfterDelay());
                    isRecenteringScheduled = false;
                }
            }
        }
        // --- Check for Recovery from Pushing States if Plant Moves Up ---
        // This check allows reverting to Normal if the plant unsticks and moves up significantly.
        // Precedes the switch statement to allow state change before executing PushingPlayer/GameOverSequence logic for the frame.
        if (currentPushState == CameraPushState.PushingPlayer || currentPushState == CameraPushState.PlantIsOffScreenGameOverSequence)
        {
            // Check if the plant has moved significantly upwards from the last height recorded before/during stagnation
            if (plantController.DisplayHeight > lastPlantHeightForStagnation + heightStagnationTolerance)
            {
                Debug.Log("[CameraController] Plant has resumed significant upward movement. Reverting to Normal state.");
                if (currentPushState != CameraPushState.Normal) // Only if we were actually pushing or in game over sequence
                {
                    currentPushState = CameraPushState.Normal; // State changes
                    targetCameraPushSpeed = 0f; // Set target for lerp to 0 (camera will slow down)
                                                // Prime SmoothDamp's velocity for a smoother transition to normal follow
                    velocity.y = currentActualPushSpeed;
                }
                stagnationTimer = 0f;     // Reset stagnation timer to prevent immediate re-stagnation
                plantOffScreenTimer = 0f; // Reset off-screen timer as it's no longer relevant
                lastPlantHeightForStagnation = plantController.DisplayHeight; // Update height baseline to current height
                
                // No need to call HandleNormalCameraMovement() here directly, 
                // the switch statement below will handle it in this same frame if currentPushState is now Normal.
            }
        }

        // --- Camera Logic based on State ---
        switch (currentPushState)
        {
            case CameraPushState.Normal:
                HandleNormalCameraMovement();
                break;

            case CameraPushState.PushingPlayer:
                HandlePushingPlayerCameraMovement();
                CheckPlantOffScreenDuringPush(); // This will transition state if needed
                break;

            case CameraPushState.PlantIsOffScreenGameOverSequence:
                HandlePushingPlayerCameraMovement(); // Continue moving camera
                plantOffScreenTimer += Time.deltaTime;
                if (plantOffScreenTimer >= plantOffScreenGameOverDelay)
                {
                    Debug.Log("[CameraController] Plant off-screen for too long during push. Triggering Game Over.");
                    if (GameManager.Instance != null) { // Safety check
                        GameManager.Instance.SetGameState(GameState.GameOver);
                    }
                    // Optionally, set currentPushState to Normal or a specific "GameOverFollow" state
                    // For now, it will just stop processing due to GameState.GameOver in the next frame.
                }
                else if (IsPlantVisibleDuringPush()) // Check if plant came back into view
                {
                    Debug.Log("[CameraController] Plant became visible again during game over sequence. Reverting to PushingPlayer.");
                    currentPushState = CameraPushState.PushingPlayer;
                    plantOffScreenTimer = 0f;
                }
                break;
        }
    }
    public void ResetCameraPushState() // Make this public to be called by GameManager
    {
        currentPushState = CameraPushState.Normal;
        stagnationTimer = 0f;
        plantOffScreenTimer = 0f;
         currentActualPushSpeed = 0f;
        targetCameraPushSpeed = 0f; // Ensure target is reset for Normal state
        velocity = Vector3.zero;    // Reset SmoothDamp's velocity for a clean start
        if (plantController != null && plantController.PlantHead != null) // Added null check for PlantHead
        {
            lastPlantHeightForStagnation = plantController.DisplayHeight;
            // Re-initialize highestYPosition to be consistent with normal camera logic start
            highestYPosition = plantController.PlantHead.position.y + verticalOffset; 
            
            // Ensure camera starts at a reasonable position relative to the plant
            // This helps if the game starts with the camera far away
            Vector3 initialCameraPos = transform.position;
            initialCameraPos.y = plantController.PlantHead.position.y + verticalOffset;
            // Preserve camera's X and Z if they are meant to be fixed or controlled elsewhere initially
            // initialCameraPos.x = transform.position.x; 
            // initialCameraPos.z = transform.position.z;
            transform.position = initialCameraPos;
        }
        else if (plantController != null) // If only PlantHead is null, still log lastPlantHeight
        {
            lastPlantHeightForStagnation = plantController.DisplayHeight;
        }


        if (isRecenteringScheduled) // Stop any ongoing recentering
        {
            StopCoroutine(RecenterAfterDelay());
            isRecenteringScheduled = false;
        }
        Debug.Log("[CameraController] Camera Push State Reset to Normal.");
    }

    private void HandleNormalCameraMovement()
    {
        Vector3 targetPlantHeadPos = plantController.PlantHead.position;
        // Keep the camera's current x and z from its actual transform position
        Vector3 targetCameraPos = new Vector3(transform.position.x, targetPlantHeadPos.y + verticalOffset, transform.position.z);

        if (targetCameraPos.y > transform.position.y) 
        {
            highestYPosition = targetCameraPos.y; 
            // Your existing smooth follow logic:
            if (!plantController.IsStuck) 
            {
                transform.position = Vector3.SmoothDamp(transform.position, targetCameraPos, ref velocity, smoothTime);
            }
        }
        // If the plant is below the highest point it reached AND it's stuck, the camera holds its position
        // until recentering kicks in or it unsticks.

        if (plantController.IsStuck && !isRecenteringScheduled && currentPushState == CameraPushState.Normal) // Ensure it only runs in normal mode
        {
            StartCoroutine(RecenterAfterDelay());
        }
    }

    private void HandlePushingPlayerCameraMovement()
    {
        currentActualPushSpeed = Mathf.Lerp(currentActualPushSpeed, targetCameraPushSpeed, Time.deltaTime * cameraPushLerpFactor);
        transform.Translate(Vector3.up * currentActualPushSpeed * Time.deltaTime, Space.World);
    }

    private bool IsPlantVisibleDuringPush()
    {
        if (plantController == null || plantController.PlantHead == null || mainCamera == null) return false;
        
        float cameraBottomEdge = mainCamera.transform.position.y - mainCamera.orthographicSize;
        return plantController.PlantHead.position.y >= cameraBottomEdge;
    }

    private void CheckPlantOffScreenDuringPush()
    {
        if (!IsPlantVisibleDuringPush())
        {
            if (currentPushState == CameraPushState.PushingPlayer) 
            {
                Debug.Log("[CameraController] Plant is off-screen during push. Starting game over timer.");
                currentPushState = CameraPushState.PlantIsOffScreenGameOverSequence;
                plantOffScreenTimer = 0f; 
            }
        }
    }
    private IEnumerator RecenterAfterDelay()
    {
         if (currentPushState != CameraPushState.Normal)
        {
            isRecenteringScheduled = false;
            yield break; // Exit if not in normal mode
        }
        isRecenteringScheduled = true;
        
        yield return new WaitForSeconds(recenterDelay);

        if (currentPushState != CameraPushState.Normal)
        {
            isRecenteringScheduled = false;
            yield break; 
        }
        
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
                if (currentPushState != CameraPushState.Normal || (plantController != null && plantController.IsStuck))
                {
                    isRecenteringScheduled = false;
                    yield break;
                } 
                transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / smoothTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
          if (currentPushState == CameraPushState.Normal && plantController != null && !plantController.IsStuck)
            {
                transform.position = targetPosition;
            }
        }
        
        isRecenteringScheduled = false;
    }
}