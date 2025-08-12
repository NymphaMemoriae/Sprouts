using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NotificationPanelUI : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private Button dismissButton; // The specific button to close the panel

    private void Awake()
    {
        // Tell the specific dismiss button to call Hide() when clicked.
        if (dismissButton != null)
        {
            dismissButton.onClick.AddListener(Hide);
        }
        else
        {
            Debug.LogError("Dismiss Button is not assigned in the NotificationPanelUI script!", this);
        }
    }

    /// <summary>
    /// Shows the notification panel with a specific message.
    /// </summary>
    public void Show(string message)
    {
        notificationText.text = message;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Hides the notification panel.
    /// </summary>
    private void Hide()
    {
        gameObject.SetActive(false);
    }
}