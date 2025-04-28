using UnityEngine;
using System;
// Use the 'using' statement for brevity, but qualify within methods for absolute clarity if needed
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References (Assigned Dynamically)")]
    public PlantController plantController;
    public PlantLife plantLife;

    public event Action<GameState> OnGameStateChanged;

    public GameState CurrentGameState { get; private set; } = GameState.MainMenu;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Explicitly use UnityEngine.SceneManagement.SceneManager here
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Explicitly use UnityEngine.SceneManagement.SceneManager here
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Explicitly use UnityEngine.SceneManagement.SceneManager here
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu")
        {
             SetGameState(GameState.MainMenu);
        }
        else
        {
             Debug.LogWarning("[GameManager] Started outside MainMenu scene, forcing MainMenu state initially.");
             SetGameState(GameState.MainMenu);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene Loaded: {scene.name}");

        if (scene.name == "GameScene")
        {
            plantController = FindAnyObjectByType<PlantController>();
            plantLife = FindAnyObjectByType<PlantLife>();


            if (plantController == null) { Debug.LogError("[GameManager] OnSceneLoaded: Could not find PlantController in GameScene!"); }
            if (plantLife == null) { Debug.LogError("[GameManager] OnSceneLoaded: Could not find PlantLife in GameScene!"); }

             if(CurrentGameState != GameState.Playing)
             {
                 // If the game is supposed to start immediately upon loading GameScene after clicking "Start"
                 // Ensure StartGame() was called in the MainMenu before loading the scene.
                 Debug.LogWarning($"[GameManager] GameScene loaded but state is {CurrentGameState}. Ensure StartGame() was called before loading.");
                 // Force playing state and reset lives if needed (adjust based on your flow)
                 SetGameState(GameState.Playing);
             }
             else if (plantLife != null) // If already playing (e.g. restart), reset lives
             {
                 plantLife.ResetLives();
             }
        }
        else
        {
            plantController = null;
            plantLife = null;
        }
    }

    private void Update()
    {
        #if UNITY_EDITOR
        if (CurrentGameState == GameState.Playing && Input.GetKeyDown(KeyCode.Escape)) { PauseGame(); }
        else if (CurrentGameState == GameState.Paused && Input.GetKeyDown(KeyCode.Escape)) { ResumeGame(); }
        #endif
    }

    public void StartGame()
    {
        SetGameState(GameState.Playing);
    }

    public void PauseGame()
    {
        if (CurrentGameState == GameState.Playing)
        {
            Debug.Log($"[GameManager] PauseGame called. Current Time.timeScale = {Time.timeScale}");
            Time.timeScale = 0f;
            Debug.Log($"[GameManager] Set Time.timeScale = {Time.timeScale}");
            SetGameState(GameState.Paused);
        }
    }

    public void ResumeGame()
    {
        if (CurrentGameState == GameState.Paused)
        {
            Debug.Log($"[GameManager] ResumeGame called. Current Time.timeScale = {Time.timeScale}");
            Time.timeScale = 1f;
            Debug.Log($"[GameManager] Set Time.timeScale = {Time.timeScale}");
            SetGameState(GameState.Playing);
        }
    }

    public void RestartGame()
    {
        if (CurrentGameState == GameState.GameOver || CurrentGameState == GameState.Paused || CurrentGameState == GameState.Playing)
        {
             Debug.Log("[GameManager] Restarting Game Scene...");
             Time.timeScale = 1f;
             // Explicitly use UnityEngine.SceneManagement.SceneManager here
             UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
             // OnSceneLoaded will handle finding references and setting state correctly after reload.
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SetGameState(GameState.MainMenu);
        // Explicitly use UnityEngine.SceneManagement.SceneManager here
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
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
        if (CurrentGameState == newState) return;

        CurrentGameState = newState;
        Debug.Log($"[GameManager] GameState changed to: {newState}");
        OnGameStateChanged?.Invoke(newState);

        // Reset lives when entering Playing state if plantLife is available
        // Moved ResetLives logic partially into OnSceneLoaded for restarts,
        // but also keep it here for the initial StartGame call.
        if (newState == GameState.Playing)
        {
             if (plantLife == null) { plantLife = FindAnyObjectByType<PlantLife>(); }

             if (plantLife != null)
             {
                 plantLife.ResetLives();
                 Debug.Log("[GameManager] Player lives reset via SetGameState.");
             }
             // Use explicit check for scene name to avoid error when not in game scene
             else if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameScene")
             {
                 Debug.LogError("[GameManager] SetGameState(Playing): Cannot reset lives - PlantLife reference is missing!");
             }
        }
    }
}

// Ensure this enum is accessible
public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver
}