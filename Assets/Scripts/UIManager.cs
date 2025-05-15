using UnityEngine;
using UnityEngine.UI; // Keep this if you use standard UI elements, TMPro usually handles its own.
using TMPro;
using UnityEngine.SceneManagement; // Required for loading scenes

public class UIManager : MonoBehaviour
{
    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI heightText;
    [SerializeField] private TextMeshProUGUI velocityText;

    [Header("Game State Panels")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject pausePanel;
    // Removed startPanel reference

    [Header("Game Over UI")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton; // Button to go back to MainMenu scene

    [Header("Pause UI")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    [Header("Gameplay Pause Button")] // Renamed header for clarity
    [SerializeField] private Button pauseButton; // Button to initiate pause

    [Header("Buff Indicators")]
    [SerializeField] private GameObject ghostBuffIndicator;
    [SerializeField] private GameObject speedBuffIndicator;
    [SerializeField] private GameObject extraLifeIndicator;

    private PlantController plantController;
    private PlantLife plantLife; 

    private float lastHeight = -999f;
    private float lastVelocity = -999f;
    private float lastScore = -999f;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            plantController = GameManager.Instance.plantController;
            plantLife = GameManager.Instance.plantLife; 
        }
        else
        {
             Debug.LogError("UIManager Start: GameManager.Instance is null!");
             plantController = FindAnyObjectByType<PlantController>();
             plantLife = FindAnyObjectByType<PlantLife>();
        }

        if(restartButton) restartButton.onClick.AddListener(OnRestartButtonClicked);
        if(mainMenuButton) mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        if(resumeButton) resumeButton.onClick.AddListener(OnResumeButtonClicked);
        if(quitButton) quitButton.onClick.AddListener(OnQuitButtonClicked);

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }
        else
        {
            Debug.LogError("UIManager: Pause Button reference is NOT assigned in the Inspector!");
        }

        if (GameManager.Instance != null)
        {
            HandleGameStateChanged(GameManager.Instance.CurrentGameState);
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            HandleGameStateChanged(GameManager.Instance.CurrentGameState);
             plantController = GameManager.Instance.plantController;
             plantLife = GameManager.Instance.plantLife;
        }
         else
         {
             Debug.LogError("UIManager OnEnable: GameManager.Instance is null!");
         }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    // Updates UI panels based on GameState
    private void HandleGameStateChanged(GameState newState)
    {
        if(gameOverPanel) gameOverPanel.SetActive(newState == GameState.GameOver);
        if(pausePanel) pausePanel.SetActive(newState == GameState.Paused);

        // Update Game Over specific text only when that panel is active
        if (newState == GameState.GameOver)
        {
            // Attempt to refresh plantController reference if it's null
            if (plantController == null && GameManager.Instance != null)
            {
                plantController = GameManager.Instance.plantController;
            }

            int currentFinalScoreInt = 0;

            if (plantController != null)
            {
                // Use CurrentHeight for score consistency with HUD, and convert to int
                currentFinalScoreInt = Mathf.FloorToInt(plantController.CurrentHeight);

                if (finalScoreText != null)
                {
                    finalScoreText.text = $"{currentFinalScoreInt}"; // Display only the integer value
                }
                else
                {
                    Debug.LogError("UIManager: finalScoreText is not assigned!");
                }

                // Handle High Score saving and display
                if (PlayerPrefsManager.Instance != null)
                {
                    float savedHighScoreFloat = PlayerPrefsManager.Instance.LoadHighScore();
                    int savedHighScoreInt = Mathf.FloorToInt(savedHighScoreFloat);

                    if (currentFinalScoreInt > savedHighScoreInt)
                    {
                        PlayerPrefsManager.Instance.SaveHighScore(plantController.CurrentHeight); // Save the raw float value for precision
                        // Update displayed high score to the new score immediately
                        if (highScoreText != null)
                        {
                            highScoreText.text = $"{currentFinalScoreInt}"; // Display only the integer value
                        }
                    }
                    else
                    {
                        // Display existing high score
                        if (highScoreText != null)
                        {
                            highScoreText.text = $"{savedHighScoreInt}"; // Display only the integer value
                        }
                    }
                }
                else
                {
                    Debug.LogError("UIManager: PlayerPrefsManager.Instance is null. Cannot save or load high score.");
                    if (highScoreText != null) highScoreText.text = "ERR"; // Error placeholder
                }
            }
            else
            {
                Debug.LogError("UIManager (GameOver): plantController is null. Cannot display final score or update high score.");
                if (finalScoreText != null) finalScoreText.text = "0"; // Default/Error placeholder for final score
                if (highScoreText != null)
                {
                    // Try to display existing high score even if plantController is null
                    if (PlayerPrefsManager.Instance != null)
                    {
                        highScoreText.text = $"{Mathf.FloorToInt(PlayerPrefsManager.Instance.LoadHighScore())}"; // Display only integer
                    }
                    else
                    {
                        highScoreText.text = "ERR"; // Error placeholder
                    }
                }
            }
        }
    } // This closing brace for HandleGameStateChanged was likely the issue if it was misplaced or duplicated.

    // --- HUD Update Methods ---
    public void UpdateScore(float score)
    {
        if (scoreText != null && !Mathf.Approximately(score, lastScore))
        {
            scoreText.text = $"Score: {Mathf.FloorToInt(score)}";
            lastScore = score;
        }
    }

    public void UpdateHeight(float height)
    {
        if (heightText != null && !Mathf.Approximately(height, lastHeight))
        {
            heightText.text = $"Height: {height:F1}m";
            lastHeight = height;
        }
    }

    public void UpdateVelocity(float velocity)
    {
        if (velocityText != null && !Mathf.Approximately(velocity, lastVelocity))
        {
            velocityText.text = $"Velocity: {velocity:F2} m/s";
            lastVelocity = velocity;
        }
    }

    public void UpdateBuffIndicators(bool hasGhostBuff, bool hasSpeedBuff, bool hasExtraLife)
    {
         if(ghostBuffIndicator) ghostBuffIndicator.SetActive(hasGhostBuff);
         if(speedBuffIndicator) speedBuffIndicator.SetActive(hasSpeedBuff);
         if(extraLifeIndicator) extraLifeIndicator.SetActive(hasExtraLife);
    }

    // --- Button Click Handlers ---
    private void OnRestartButtonClicked() => GameManager.Instance?.RestartGame();
    private void OnMainMenuButtonClicked() => GameManager.Instance?.ReturnToMainMenu(); 
    private void OnResumeButtonClicked() => GameManager.Instance?.ResumeGame();
    private void OnQuitButtonClicked() => GameManager.Instance?.QuitGame();

    private void OnPauseButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
        }
        else
        {
            Debug.LogError("Cannot Pause: GameManager Instance is null!");
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentGameState == GameState.Playing)
        {
             if (plantController == null)
             {
                plantController = GameManager.Instance?.plantController;
             }

             if (plantController != null)
             {
                 UpdateHeight(plantController.DisplayHeight);
                 UpdateScore(plantController.CurrentHeight); 
                 UpdateVelocity(plantController.CurrentVelocity);
             }
        }
    }

    // The local LoadHighScore() method has been removed as PlayerPrefsManager.Instance.LoadHighScore() should be used.

} // This is the final closing brace for the UIManager class.