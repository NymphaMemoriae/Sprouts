using UnityEngine;
using UnityEngine.UI;
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

    // References ideally assigned by GameManager or via events
    // Keeping direct references for now if needed for HUD updates
    private PlantController plantController;
    private PlantLife plantLife; // Only needed if directly accessing lives here

    // Cached HUD values
    private float lastHeight = -999f;
    private float lastVelocity = -999f;
    private float lastScore = -999f;

    private void Start()
    {
        // Attempt to get references from GameManager if not assigned
        if (GameManager.Instance != null)
        {
            plantController = GameManager.Instance.plantController;
            plantLife = GameManager.Instance.plantLife; // Get plantLife if needed
        }
        else
        {
             Debug.LogError("UIManager Start: GameManager.Instance is null!");
             // Fallback find if necessary, though GameManager should provide them
             plantController = FindObjectOfType<PlantController>();
             plantLife = FindObjectOfType<PlantLife>();
        }


        // Add listeners with null checks
        if(restartButton) restartButton.onClick.AddListener(OnRestartButtonClicked);
        if(mainMenuButton) mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        if(resumeButton) resumeButton.onClick.AddListener(OnResumeButtonClicked);
        if(quitButton) quitButton.onClick.AddListener(OnQuitButtonClicked);

        // Crucial: Add listener for the gameplay pause button
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }
        else
        {
            Debug.LogError("UIManager: Pause Button reference is NOT assigned in the Inspector!");
        }

        // Subscribe to game state changes
         // Moved subscription to OnEnable for robustness

        // Set initial UI state based on GameManager's state when scene loads
        if (GameManager.Instance != null)
        {
            HandleGameStateChanged(GameManager.Instance.CurrentGameState);
        }
    }

    private void OnEnable()
    {
        // Subscribe to game state changes when the UI becomes active
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            // Ensure UI reflects current state immediately on enable
            HandleGameStateChanged(GameManager.Instance.CurrentGameState);
             // Refresh references in case they were lost/reloaded
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
        // Unsubscribe when the UI is disabled or destroyed
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
             // Refresh controller reference in case it changed
             if (plantController == null && GameManager.Instance != null) plantController = GameManager.Instance.plantController;

             if(plantController != null)
             {
                 if(finalScoreText) finalScoreText.text = $"Final Height: {plantController.DisplayHeight:F1}m";
             }
             // Add high score display logic here
             if(highScoreText) highScoreText.text = $"High Score: {LoadHighScore():F1}m"; // Example
        }
    }

    // --- HUD Update Methods ---
    // These methods are public if other scripts need to trigger updates,
    // but currently called by internal Update loop. Consider events.
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

    // Called externally or via events ideally
    public void UpdateBuffIndicators(bool hasGhostBuff, bool hasSpeedBuff, bool hasExtraLife)
    {
         if(ghostBuffIndicator) ghostBuffIndicator.SetActive(hasGhostBuff);
         if(speedBuffIndicator) speedBuffIndicator.SetActive(hasSpeedBuff);
         if(extraLifeIndicator) extraLifeIndicator.SetActive(hasExtraLife);
    }

    // --- Button Click Handlers ---
    private void OnRestartButtonClicked() => GameManager.Instance?.RestartGame();
    private void OnMainMenuButtonClicked() => GameManager.Instance?.ReturnToMainMenu(); // GameManager handles scene load
    private void OnResumeButtonClicked() => GameManager.Instance?.ResumeGame();
    private void OnQuitButtonClicked() => GameManager.Instance?.QuitGame();

    // Handler for the gameplay pause button
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

    // --- Update for Polling HUD values ---
    // Consider replacing this with event-driven updates for better performance
    private void Update()
    {
        // Only update HUD if playing and controller is valid
        if (GameManager.Instance != null && GameManager.Instance.CurrentGameState == GameState.Playing)
        {
             // Ensure controller reference is up-to-date
             if (plantController == null)
             {
                plantController = GameManager.Instance?.plantController;
             }

             if (plantController != null)
             {
                 // Use cached values to avoid unnecessary updates
                 UpdateHeight(plantController.DisplayHeight);
                 UpdateScore(plantController.CurrentHeight); // Assuming score is based on height
                 UpdateVelocity(plantController.CurrentVelocity);
             }
        }
    }

     // Example High Score Logic - Better placed in GameManager or separate data manager
    private float LoadHighScore()
    {
        return PlayerPrefs.GetFloat("HighScore", 0f); // Replace with your save system if needed
    }
}