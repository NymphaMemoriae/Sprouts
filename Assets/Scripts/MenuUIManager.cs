using UnityEngine;
using TMPro; // Use TextMeshPro for modern, scalable text

public class MenuUIManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Assign the TextMeshPro UI element that displays the total coins.")]
    [SerializeField] private TextMeshProUGUI totalCoinsText;

    [Tooltip("Assign the TextMeshPro UI element that displays the high score.")]
    [SerializeField] private TextMeshProUGUI highScoreText;

    // OnEnable is called every time the menu becomes active.
    // This is more efficient than using Update().
    private void OnEnable()
    {
        UpdateDisplay();
    }

    /// <summary>
    /// Updates the coin and high score text from saved data.
    /// Can be called from other scripts (like a shop button) to force a refresh.
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
            // "F0" formats the float as a whole number without decimals
            highScoreText.text = $"High Score: {currentHighScore:F0}m";
        }
    }
}