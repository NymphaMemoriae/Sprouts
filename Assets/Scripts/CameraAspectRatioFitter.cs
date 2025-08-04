using UnityEngine;

/// <summary>
/// Adjusts an orthographic camera's size to ensure a specific world-space width is always visible.
/// Attach this to your Main Camera GameObject.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraAspectRatioFitter : MonoBehaviour
{
    [Tooltip("The target width in world units that you want to keep visible.")]
    [SerializeField] private float targetWidth = 10f; // Set this to the width of your World Space Canvas

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam.orthographic)
        {
            AdjustOrthographicSize();
        }
    }

    /// <summary>
    /// Calculates and sets the orthographic size based on the target width and current screen aspect ratio.
    /// </summary>
    private void AdjustOrthographicSize()
    {
        float screenAspectRatio = (float)Screen.width / (float)Screen.height;
        
        // The formula for orthographic width is: orthographicSize * 2 * aspectRatio
        // We rearrange it to solve for orthographicSize:
        float requiredOrthographicSize = targetWidth / (2f * screenAspectRatio);

        cam.orthographicSize = requiredOrthographicSize;
        
        Debug.Log($"[CameraAspectRatioFitter] Adjusted Orthographic Size to {requiredOrthographicSize} to fit target width {targetWidth} on aspect ratio {screenAspectRatio}.");
    }
}