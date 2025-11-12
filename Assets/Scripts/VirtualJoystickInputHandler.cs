using UnityEngine;

public class VirtualJoystickInputHandler : MonoBehaviour
{
    [SerializeField] private PlantController plantController;
    [SerializeField] private SimpleJoystick joystick;

    [Tooltip("Sensitivity for horizontal movement from the joystick.")]
    [SerializeField] private float horizontalSensitivity = 1.5f;

    [Tooltip("How far 'up' the stick must be pushed to trigger growth.")]
    [SerializeField][Range(0f, 1f)] private float growthThreshold = 0.2f;
    
    [Tooltip("How far 'down' the stick must be pushed to trigger deceleration.")]
    [SerializeField] [Range(-1f, 0f)] private float decelerationThreshold = -0.2f;

    private void Start()
    {
        if (plantController == null)
            Debug.LogError("PlantController is not set on VirtualJoystickInputHandler!");
        if (joystick == null)
            Debug.LogError("Joystick is not set on VirtualJoystickInputHandler!");
    }

    private void Update()
    {
        if (plantController == null || joystick == null) return;
        
        // --- Pre-computation Checks ---
        if (GameManager.Instance != null && GameManager.Instance.CurrentGameState != GameState.Playing && !Application.isEditor)
        {
            plantController.SetGrowing(false);
            plantController.SetDecelerating(false); // Also reset deceleration
            plantController.SetHorizontalMovement(0);
            return;
        }

        // 1. Handle Horizontal Movement
        float horizontalInput = joystick.Horizontal * horizontalSensitivity;
        plantController.SetHorizontalMovement(horizontalInput);

        // 2. Handle Vertical Movement (NEW LOGIC)
        if (joystick.Vertical > growthThreshold)
        {
            // Accelerate
            plantController.SetGrowing(true);
        }
        else if (joystick.Vertical < decelerationThreshold)
        {
            // Decelerate
            plantController.SetDecelerating(true);
        }
        else
        {
            // Neither accelerating nor decelerating (coasting)
            plantController.SetGrowing(false);
            plantController.SetDecelerating(false);
        }
    }
}