using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] public PlantController plantController;
    [SerializeField] public PlantLife plantLife;

    public event Action<GameState> OnGameStateChanged;
    public GameState CurrentGameState { get; private set; } = GameState.MainMenu;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetGameState(GameState.MainMenu);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (CurrentGameState == GameState.Playing && Input.GetKeyDown(KeyCode.Escape))
        {
            PauseGame();
        }
        else if (CurrentGameState == GameState.Paused && Input.GetKeyDown(KeyCode.Escape))
        {
            ResumeGame();
        }
#endif
    }

    public void StartGame()
    {
        if (plantLife != null)
        {
            plantLife.ResetLives();
        }

        SetGameState(GameState.Playing);
    }

    public void PauseGame()
    {
        if (CurrentGameState == GameState.Playing)
        {
            Debug.Log($"[GameManager] PauseGame called. Current Time.timeScale = {Time.timeScale}"); // <-- ADD LOG
            Time.timeScale = 0f;
            Debug.Log($"[GameManager] Set Time.timeScale = {Time.timeScale}"); // <-- ADD LOG
            SetGameState(GameState.Paused);
        }
    }

    public void ResumeGame()
    {
        if (CurrentGameState == GameState.Paused)
        {
            Debug.Log($"[GameManager] ResumeGame called. Current Time.timeScale = {Time.timeScale}"); // <-- ADD LOG
            Time.timeScale = 1f;
            Debug.Log($"[GameManager] Set Time.timeScale = {Time.timeScale}"); // <-- ADD LOG
            SetGameState(GameState.Playing);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        StartGame();
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SetGameState(GameState.MainMenu);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetGameState(GameState newState)
    {
        CurrentGameState = newState;
        OnGameStateChanged?.Invoke(newState);
    }
}

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver
}
