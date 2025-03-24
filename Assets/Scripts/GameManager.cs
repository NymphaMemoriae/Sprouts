using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] public PlantController plantController;
    [SerializeField] public PlantLife plantLife;  // Reference to the PlantLife component

    // Events
    public event Action<GameState> OnGameStateChanged;
    
    // Properties
    public GameState CurrentGameState { get; private set; } = GameState.MainMenu;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        SetGameState(GameState.MainMenu);
    }

    private void OnEnable()
    {

    }

    private void OnDisable()
    {

    }

    
    private void Update()
    {
        if (CurrentGameState == GameState.Playing)
        {
            // Check for pause input
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PauseGame();
            }
        }
        else if (CurrentGameState == GameState.Paused)
        {
            // Check for resume input
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ResumeGame();
            }
        }
    }
    
    public void StartGame()
    {
        // Reset the plant's lives at the start of each game
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
            Time.timeScale = 0f;
            SetGameState(GameState.Paused);
        }
    }
    
    public void ResumeGame()
    {
        if (CurrentGameState == GameState.Paused)
        {
            Time.timeScale = 1f;
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
 
    private void SetGameState(GameState newState)
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