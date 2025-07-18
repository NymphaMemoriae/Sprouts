// MenuUIManager.cs (Refactored)
using UnityEngine;
using TMPro;

public class MenuUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI totalCoinsText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    private void OnEnable()
    {
        // Subscribe to the shop state change event to keep the coin display updated.
        ShopManager.OnShopStateChanged += UpdateDisplay;
        // Also call it once when the menu becomes active.
        UpdateDisplay();
    }

    private void OnDisable()
    {
        // Unsubscribe from the event.
        ShopManager.OnShopStateChanged -= UpdateDisplay;
    }

    /// <summary>
    /// Updates the coin and high score text from saved data. Now called by event.
    /// </summary>
    public void UpdateDisplay()
    {
        if (PlayerPrefsManager.Instance == null) return;

        // Load and display total coins
        int currentCoins = PlayerPrefsManager.Instance.LoadMoney();
        if (totalCoinsText != null)
        {
            totalCoinsText.text = currentCoins.ToString();
        }

        // Load and display high score
        float currentHighScore = PlayerPrefsManager.Instance.LoadHighScore();
        if (highScoreText != null)
        {
            highScoreText.text = $"High Score: {currentHighScore:F0}m";
        }
    }
}