// ConfirmationPanelUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; // Required for Action

public class ConfirmationPanelUI : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action<bool> onConfirmationComplete;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
    }

    /// <summary>
    /// Shows the confirmation panel with a specific message and registers a callback for the result.
    /// </summary>
    public void Show(string message, Action<bool> callback)
    {
        confirmationText.text = message;
        onConfirmationComplete = callback;
        gameObject.SetActive(true);
    }

    private void OnConfirm()
    {
        gameObject.SetActive(false);
        onConfirmationComplete?.Invoke(true);
    }

    private void OnCancel()
    {
        gameObject.SetActive(false);
        onConfirmationComplete?.Invoke(false);
    }
}