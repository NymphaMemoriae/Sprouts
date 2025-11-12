using UnityEngine;
using UnityEngine.EventSystems; // Required for UI check

public class TouchInputHandler : MonoBehaviour
{
    [SerializeField] private PlantController plantController;
    [SerializeField] private float touchSensitivity = 1f;
    [Header("Vertical Sensitivity")]
    [Tooltip("Vertical drag (normalized) needed to accelerate.")]
    [SerializeField] [Range(0.001f, 0.1f)] private float verticalGrowthThreshold = 0.01f;
    [Tooltip("Vertical drag (normalized) needed to decelerate.")]
    [SerializeField] [Range(-0.1f, -0.001f)] private float verticalDecelThreshold = -0.01f;
    
    private Vector2 lastTouchPosition;
    
    private void Start()
    {
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
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return;
            }
            
            if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                HandleTouchInput(touch);
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            HandleMouseInput();
        }
        else if (Input.GetMouseButton(0))
        {
            HandleMouseInput();
        }
        else // If there is NO active input:
        {
            plantController.SetGrowing(false);
            plantController.SetDecelerating(false); // --- ADD THIS LINE ---
            plantController.SetHorizontalMovement(0); 
        }
    }
    
   private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastTouchPosition = Input.mousePosition;
            plantController.SetGrowing(true); // Start growing on click
            plantController.SetDecelerating(false);
        }
        
        if (Input.GetMouseButton(0))
        {
            Vector2 currentMousePosition = Input.mousePosition;
            
            // --- Horizontal ---
            float horizontalDelta = (currentMousePosition.x - lastTouchPosition.x) / Screen.width;
            plantController.SetHorizontalMovement(horizontalDelta * touchSensitivity);
            
            // --- NEW: Vertical ---
            float verticalDelta = (currentMousePosition.y - lastTouchPosition.y) / Screen.height;

            if (verticalDelta > verticalGrowthThreshold)
            {
                // Dragging up
                plantController.SetGrowing(true);
            }
            else if (verticalDelta < verticalDecelThreshold)
            {
                // Dragging down
                plantController.SetDecelerating(true);
            }
            else
            {
                // Coasting (moving sideways or still)
                plantController.SetGrowing(false);
                plantController.SetDecelerating(false);
            }
            
            lastTouchPosition = currentMousePosition;
        }
    }
    
    // Modified to accept a Touch parameter
   private void HandleTouchInput(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                lastTouchPosition = touch.position;
                plantController.SetGrowing(true);
                plantController.SetDecelerating(false);
                plantController.SetHorizontalMovement(0); 
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                
                // --- Horizontal ---
                float horizontalDelta = (touch.position.x - lastTouchPosition.x) / Screen.width;
                plantController.SetHorizontalMovement(horizontalDelta * touchSensitivity);
                
                // --- NEW: Vertical ---
                float verticalDelta = (touch.position.y - lastTouchPosition.y) / Screen.height;

                if (verticalDelta > verticalGrowthThreshold)
                {
                    // Dragging up
                    plantController.SetGrowing(true);
                }
                else if (verticalDelta < verticalDecelThreshold)
                {
                    // Dragging down
                    plantController.SetDecelerating(true);
                }
                else
                {
                    // Coasting (moving sideways or still)
                    plantController.SetGrowing(false);
                    plantController.SetDecelerating(false);
                }

                lastTouchPosition = touch.position;
                break;

            default:
                plantController.SetGrowing(false);
                plantController.SetDecelerating(false); // Also stop decelerating
                plantController.SetHorizontalMovement(0);
                break;
        }
    }
}