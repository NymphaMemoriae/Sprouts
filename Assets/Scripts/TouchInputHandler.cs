using UnityEngine;

public class TouchInputHandler : MonoBehaviour
{
    [SerializeField] private PlantController plantController;
    [SerializeField] private float touchSensitivity = 1f;
    
    private Vector2 lastTouchPosition;
    // private bool isTouching = false;
    
    private void Start()
    {
        // Debug log to verify references
        Debug.Log("TouchInputHandler initialized. PlantController reference: " + (plantController != null));
    }
    
    private void Update()
    {
        // --- Pre-computation Checks ---
        if (plantController == null || (GameManager.Instance != null && GameManager.Instance.CurrentGameState != GameState.Playing && !Application.isEditor))
        {
            return;
        }

        // --- Check for Active Input ---

        // Is there a touch on the screen?
        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        // Is the mouse button being held down?
        else if (Input.GetMouseButton(0))
        {
            HandleMouseInput();
        }
        // If there is NO active input:
        else
        {
            // Stop growing and force the head to be straight.
            plantController.SetGrowing(false);
            plantController.SetHorizontalMovement(0); 
        }
    }
    
   private void HandleMouseInput()
    {
        
        if (Input.GetMouseButton(0))
        {
            
            if (Input.GetMouseButtonDown(0))
            {
                lastTouchPosition = Input.mousePosition;
                plantController.SetGrowing(true);
            }

            
            Vector2 currentMousePosition = Input.mousePosition;
            float horizontalDelta = (currentMousePosition.x - lastTouchPosition.x) / Screen.width;
            plantController.SetHorizontalMovement(horizontalDelta * touchSensitivity);

         
            lastTouchPosition = currentMousePosition;
        }
       
        else
        {
            
            if (Input.GetMouseButtonUp(0))
            {
                plantController.SetGrowing(false);
            }

           
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
                  
                    lastTouchPosition = touch.position;
                    plantController.SetGrowing(true);
                    plantController.SetHorizontalMovement(0); 
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    
                    float horizontalDelta = (touch.position.x - lastTouchPosition.x) / Screen.width;
                    plantController.SetHorizontalMovement(horizontalDelta * touchSensitivity);
                    lastTouchPosition = touch.position;
                    break;

                
                default:
                    
                    plantController.SetGrowing(false);
                    plantController.SetHorizontalMovement(0);
                    break;
            }
        }
    }
}