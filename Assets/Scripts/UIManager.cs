using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private TextMeshProUGUI heightText;
    [SerializeField] private TextMeshProUGUI velocityText;

    
    [Header("Game State Panels")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject startPanel;
    
    [Header("Game Over UI")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("Pause UI")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;
    
    [Header("Start UI")]
    [SerializeField] private Button startButton;
    
    [Header("Buff Indicators")]
    [SerializeField] private GameObject ghostBuffIndicator;
    [SerializeField] private GameObject speedBuffIndicator;
    [SerializeField] private GameObject extraLifeIndicator;
    
    [Header("References")]
    [SerializeField] private PlantLife plantLife;
    [SerializeField] private PlantController plantController;

    private void Start()
    {
        // Set up button listeners
        restartButton.onClick.AddListener(OnRestartButtonClicked);
        mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        resumeButton.onClick.AddListener(OnResumeButtonClicked);
        quitButton.onClick.AddListener(OnQuitButtonClicked);
        startButton.onClick.AddListener(OnStartButtonClicked);
        
        UpdateUI();

        // Verify UI setup
        Debug.Log($"UIManager setup check:");
        Debug.Log($"- GameOverPanel reference: {(gameOverPanel != null ? "OK" : "Missing")}");
        Debug.Log($"- GameOverPanel active: {(gameOverPanel != null ? gameOverPanel.activeSelf.ToString() : "N/A")}");
        Debug.Log($"- FinalScoreText reference: {(finalScoreText != null ? "OK" : "Missing")}");
        Debug.Log($"- HighScoreText reference: {(highScoreText != null ? "OK" : "Missing")}");
        Debug.Log($"- RestartButton reference: {(restartButton != null ? "OK" : "Missing")}");
        Debug.Log($"- MainMenuButton reference: {(mainMenuButton != null ? "OK" : "Missing")}");
    }
    
    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            Debug.Log("UIManager: Subscribing to GameManager events"); // Debug log
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }
        else
        {
            Debug.LogError("UIManager: GameManager.Instance is null!");
        }

        // Add this to subscribe to PlantLife events
        if (plantLife != null)
        {
            plantLife.onLivesChanged.AddListener(UpdateLives);
        }
    }
    
    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        // Add this to unsubscribe from PlantLife events
        if (plantLife != null)
        {
            plantLife.onLivesChanged.RemoveListener(UpdateLives);
        }
    }
    
    private void HandleGameStateChanged(GameState newState)
    {
        Debug.Log($"UIManager: Game state changed to {newState}"); // Debug log
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (gameOverPanel == null)
        {
            Debug.LogError("UIManager: gameOverPanel reference is missing!");
            return;
        }

        Debug.Log($"UIManager: Updating UI for state {GameManager.Instance.CurrentGameState}"); // Debug log
        
        // Update panels based on game state
        gameOverPanel.SetActive(GameManager.Instance.CurrentGameState == GameState.GameOver);
        pausePanel.SetActive(GameManager.Instance.CurrentGameState == GameState.Paused);
        startPanel.SetActive(GameManager.Instance.CurrentGameState == GameState.MainMenu);
        
        // Update HUD elements
        if (plantLife != null)
        {
            UpdateLives(plantLife.CurrentLives);
        }
        
        // Update game over panel texts if needed
        if (GameManager.Instance.CurrentGameState == GameState.GameOver && plantController != null)
        {
            // Use DisplayHeight instead of CurrentHeight for the final score
            finalScoreText.text = $"Final Height: {plantController.DisplayHeight:F1}m";
            highScoreText.text = ""; // Remove high score since we're not using it
        }
    }
    
    public void UpdateScore(float score)
    {
        scoreText.text = $"Score: {Mathf.FloorToInt(score)}";
    }
    
    public void UpdateLives(int lives)
    {
        livesText.text = $"Lives: {lives}";
    }
    
    public void UpdateHeight(float height)
    {
        if (heightText == null)
        {
            Debug.LogError("Height Text is not assigned in UIManager!");
            return;
        }
        heightText.text = $"Height: {height:F1}m";
    }
    
    public void UpdateVelocity(float velocity)
    {
        if (velocityText != null)
        {
            velocityText.text = $"Velocity: {velocity:F2} u/s";
        }
    }

    public void UpdateBuffIndicators(bool hasGhostBuff, bool hasSpeedBuff, bool hasExtraLife)
    {
        ghostBuffIndicator.SetActive(hasGhostBuff);
        speedBuffIndicator.SetActive(hasSpeedBuff);
        extraLifeIndicator.SetActive(hasExtraLife);
    }
    
    // Button event handlers
    private void OnRestartButtonClicked()
    {
        GameManager.Instance.RestartGame();
    }
    
    private void OnMainMenuButtonClicked()
    {
        GameManager.Instance.ReturnToMainMenu();
    }
    
    private void OnResumeButtonClicked()
    {
        GameManager.Instance.ResumeGame();
    }
    
    private void OnQuitButtonClicked()
    {
        GameManager.Instance.QuitGame();
    }
    
    private void OnStartButtonClicked()
    {
        GameManager.Instance.StartGame();
    }
    
   private void Update()
    {
        if (plantController != null)
        {
            UpdateHeight(plantController.DisplayHeight);
            UpdateScore(plantController.CurrentHeight);
            UpdateVelocity(plantController.CurrentVelocity); // 👈 Add this line
        }
    }

}