using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
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

    private float lastHeight = -999f;
    private float lastVelocity = -999f;
    private float lastScore = -999f;

    private void Start()
    {
        restartButton.onClick.AddListener(OnRestartButtonClicked);
        mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        resumeButton.onClick.AddListener(OnResumeButtonClicked);
        quitButton.onClick.AddListener(OnQuitButtonClicked);
        startButton.onClick.AddListener(OnStartButtonClicked);

        UpdateUI();
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (gameOverPanel == null) return;

        GameState state = GameManager.Instance.CurrentGameState;

        gameOverPanel.SetActive(state == GameState.GameOver);
        pausePanel.SetActive(state == GameState.Paused);
        startPanel.SetActive(state == GameState.MainMenu);

        if (state == GameState.GameOver && plantController != null)
        {
            finalScoreText.text = $"Final Height: {plantController.DisplayHeight:F1}m";
            highScoreText.text = "";
        }
    }

    public void UpdateScore(float score)
    {
        if (Mathf.Approximately(score, lastScore)) return;
        scoreText.text = $"Score: {Mathf.FloorToInt(score)}";
        lastScore = score;
    }

    public void UpdateHeight(float height)
    {
        if (Mathf.Approximately(height, lastHeight)) return;
        if (heightText != null)
        {
            heightText.text = $"Height: {height:F1}m";
            lastHeight = height;
        }
    }

    public void UpdateVelocity(float velocity)
    {
        if (Mathf.Approximately(velocity, lastVelocity)) return;
        if (velocityText != null)
        {
            velocityText.text = $"Velocity: {velocity:F2} u/s";
            lastVelocity = velocity;
        }
    }

    public void UpdateBuffIndicators(bool hasGhostBuff, bool hasSpeedBuff, bool hasExtraLife)
    {
        ghostBuffIndicator.SetActive(hasGhostBuff);
        speedBuffIndicator.SetActive(hasSpeedBuff);
        extraLifeIndicator.SetActive(hasExtraLife);
    }

    private void OnRestartButtonClicked() => GameManager.Instance.RestartGame();
    private void OnMainMenuButtonClicked() => GameManager.Instance.ReturnToMainMenu();
    private void OnResumeButtonClicked() => GameManager.Instance.ResumeGame();
    private void OnQuitButtonClicked() => GameManager.Instance.QuitGame();
    private void OnStartButtonClicked() => GameManager.Instance.StartGame();

    private void Update()
    {
        if (plantController != null)
        {
            UpdateHeight(plantController.DisplayHeight);
            UpdateScore(plantController.CurrentHeight);
            UpdateVelocity(plantController.CurrentVelocity);
        }
    }
}
