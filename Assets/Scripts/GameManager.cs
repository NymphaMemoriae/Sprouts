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
    public CameraController cameraController;

    public event Action<GameState> OnGameStateChanged;

    public GameState CurrentGameState { get; private set; } = GameState.MainMenu;
    public static Vector3? LastCheckpointPosition { get; private set; } = null;
    public static Vector3 InitialSpawnPosition { get; private set; } = Vector3.zero; // Default, will be set
    private static bool _initialSpawnPositionSet = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Explicitly use UnityEngine.SceneManagement.SceneManager here
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
 // Find initial spawn point ONLY once when the GameManager is first created
            if (!_initialSpawnPositionSet)
            {
                GameObject spawnPointObject = GameObject.FindGameObjectWithTag("InitialSpawn");
                if (spawnPointObject != null)
                {
                    InitialSpawnPosition = spawnPointObject.transform.position;
                    _initialSpawnPositionSet = true;
                    Debug.Log($"[GameManager] Initial spawn position set to: {InitialSpawnPosition} from object {spawnPointObject.name}");
                }
                else
                {
                    Debug.LogWarning("[GameManager] Could not find GameObject with tag 'InitialSpawn'. Using default Vector3.zero.");
                    InitialSpawnPosition = Vector3.zero; // Or a sensible default
                        _initialSpawnPositionSet = true; // Mark as set even if not found to avoid repeated searches
                }
            }           
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
            plantController = FindObjectOfType<PlantController>(); // Around Line 64
            plantLife = FindObjectOfType<PlantLife>(); 

            if (cameraController == null) // Try to find it if not assigned
            {
                cameraController = FindObjectOfType<CameraController>();
            }


            if (plantController == null) { Debug.LogError("[GameManager] OnSceneLoaded: Could not find PlantController in GameScene!"); }
            else
            {
                // *** THIS IS THE KEY RESPAWN LOGIC ***
                Vector3 respawnPos = GetRespawnPosition();
                plantController.ResetState(respawnPos); // Reset position and state
                Debug.Log($"[GameManager] Plant positioned at {respawnPos}");
            }
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
             if (cameraController != null)
            {
                Debug.Log("[GameManager] GameScene loaded, resetting CameraController state.");
                cameraController.ResetCameraPushState();
            }
            else
            {
                Debug.LogWarning("[GameManager] CameraController not found in GameScene to reset its state.");
            }
        }
         else if (scene.name == "MainMenu") // Reset checkpoint when going back to MainMenu
        {
            ResetCheckpoint();
            SetGameState(GameState.MainMenu); // Ensure state is MainMenu
            plantController = null;
            plantLife = null;
        }
        else
        {
            plantController = null;
            plantLife = null;
            cameraController = null; 
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
        ResetCheckpoint(); // Ensure a fresh start from the beginning
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
              SetGameState(GameState.Playing); // Ensure state is Playing after restart
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        ResetCheckpoint(); // Reset checkpoint when quitting to menu
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
    public static void SetCurrentCheckpointPosition(Vector3 position)
    {
        LastCheckpointPosition = position;
        Debug.Log($"[GameManager] Checkpoint set to: {position}");
    }

    // Add this method to get the correct respawn position
    public static Vector3 GetRespawnPosition()
    {
        return LastCheckpointPosition ?? InitialSpawnPosition;
    }

    // Add this method to reset the checkpoint state
    public static void ResetCheckpoint()
    {
        LastCheckpointPosition = null;
        Debug.Log("[GameManager] Checkpoint reset.");
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