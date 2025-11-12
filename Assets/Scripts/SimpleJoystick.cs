using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SimpleJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Tooltip("The UI element for the joystick's handle (the part that moves).")]
    [SerializeField] private RectTransform handleRect;

    [Tooltip("How far the handle can move from the center (in pixels).")]
    [SerializeField] private float joystickRange = 100f;

    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }
    
    private RectTransform backgroundRect;
    private Vector2 centerPosition;

    private void Start()
    {
        backgroundRect = GetComponent<RectTransform>();
        centerPosition = handleRect.anchoredPosition; // Assumes handle starts at center

        if (handleRect == null)
        {
            Debug.LogError("SimpleJoystick needs a reference to its Handle RectTransform!");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData); // Start processing drag immediately
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (handleRect == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            backgroundRect, 
            eventData.position, 
            eventData.pressEventCamera, 
            out localPoint
        );

        // Clamp the handle's position within the joystickRange
        Vector2 handlePosition = Vector2.ClampMagnitude(localPoint, joystickRange);
        handleRect.anchoredPosition = handlePosition;

        // Calculate normalized output (-1 to 1)
        Horizontal = handlePosition.x / joystickRange;
        Vertical = handlePosition.y / joystickRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Reset handle and output values
        handleRect.anchoredPosition = centerPosition;
        Horizontal = 0f;
        Vertical = 0f;
    }
}