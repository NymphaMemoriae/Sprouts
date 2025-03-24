using UnityEngine;

public class TouchInputHandler : MonoBehaviour
{
    [SerializeField] private PlantController plantController;
    [SerializeField] private float touchSensitivity = 1f;
    
    private Vector2 lastTouchPosition;
    private bool isTouching = false;
    
    private void Start()
    {
        // Debug log to verify references
        Debug.Log("TouchInputHandler initialized. PlantController reference: " + (plantController != null));
    }
    
    private void Update()
    {
        // Debug the game state
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null! Make sure GameManager exists in the scene.");
            return;
        }
        
        // Always process input in editor mode for testing, or when game is playing
        bool shouldProcessInput = Application.isEditor || GameManager.Instance.CurrentGameState == GameState.Playing;
        
        if (!shouldProcessInput)
        {
            Debug.Log("Not processing input because game state is: " + GameManager.Instance.CurrentGameState);
            return;
        }
        
        if (plantController == null)
        {
            Debug.LogError("PlantController reference is missing!");
            return;
        }
            
        // MODIFIED: Always handle both input types for testing
        // This ensures input works on all platforms
        HandleMouseInput();
        
        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
    }
    
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Mouse button down detected");
            isTouching = true;
            lastTouchPosition = Input.mousePosition;
            plantController.SetGrowing(true);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            Debug.Log("Mouse button up detected");
            isTouching = false;
            plantController.SetGrowing(false);
        }
        
        if (isTouching)
        {
            Vector2 currentMousePosition = Input.mousePosition;
            float horizontalDelta = (currentMousePosition.x - lastTouchPosition.x) / Screen.width;
            
            // Apply horizontal movement
            plantController.SetHorizontalMovement(horizontalDelta * touchSensitivity);
            
            // Update last position
            lastTouchPosition = currentMousePosition;
        }
        else
        {
            // When not touching, gradually return to neutral position
            plantController.SetHorizontalMovement(0);
        }
    }
    
    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    Debug.Log("Touch began detected");
                    isTouching = true;
                    lastTouchPosition = touch.position;
                    plantController.SetGrowing(true);
                    break;
                    
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    float horizontalDelta = (touch.position.x - lastTouchPosition.x) / Screen.width;
                    plantController.SetHorizontalMovement(horizontalDelta * touchSensitivity);
                    lastTouchPosition = touch.position;
                    break;
                    
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    Debug.Log("Touch ended detected");
                    isTouching = false;
                    plantController.SetGrowing(false);
                    break;
            }
        }
        else if (!isTouching)
        {
            // When not touching, gradually return to neutral position
            plantController.SetHorizontalMovement(0);
        }
    }
}