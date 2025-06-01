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
    private static Vector3? _lastCheckpointPosition = null; 
    public static BiomeData SelectedStartBiomeForNextRun { get; private set; } = null;
    private static float? _forcedInitialHeightForNextRun = null;
    public const float TILE_VERTICAL_SIZE = 20f; // Define tile height
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
         else if (scene.name == "MainMenu")
        {
            
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

    public void StartGame() // Often called from a generic "Start" button in MainMenu if no level is selected
    {
        // If SetNextStartingLevel(null) hasn't been explicitly called by a "Default Start" button
        // (which would have cleared _lastCheckpointPosition already via SetNextStartingLevel),
        // and no specific level was selected, then ensure the session is reset for a fresh default start.
        if (SelectedStartBiomeForNextRun == null && !_forcedInitialHeightForNextRun.HasValue)
        {
            ResetSessionForNewGame(); // Ensures a clean slate if StartGame() is for a brand new default session
        }
        // If a level *was* selected by StartingBiomeSelection, SetNextStartingLevel already configured
        // _forcedInitialHeightForNextRun and cleared _lastCheckpointPosition.
        // If a "Default Start" button called SetNextStartingLevel(null), that also prepared the state.

        SetGameState(GameState.Playing);
        // The actual loading of GameScene (which should happen after this if triggered by UI)
        // will use GetRespawnPosition() in OnSceneLoaded to place the player.
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
             Debug.Log("[GameManager] Initiating scene restart with fade...");
             Time.timeScale = 1f;
             // SetGameState(GameState.Playing) will be reaffirmed by OnSceneLoaded when GameScene loads.
             
             if (GameSceneLoader.Instance != null)
             {
                 // GameSceneLoader.ReloadCurrentScene() already handles getting active scene name
                 GameSceneLoader.Instance.ReloadCurrentScene(); 
             }
             else
             {
                 Debug.LogError("[GameManager] GameSceneLoader instance not found! Cannot restart scene with fade. Restarting directly.");
                 UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
             }
             // It's generally good to set the intended state *before* the load starts,
             // and OnSceneLoaded can confirm/adjust if needed.
             SetGameState(GameState.Playing);
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        ResetSessionForNewGame();
        // SetGameState(GameState.MainMenu); // Set state *before* calling load. OnSceneLoaded will also ensure it.
        Debug.Log("[GameManager] Initiating return to MainMenu with fade...");

        if (GameSceneLoader.Instance != null)
        {
            GameSceneLoader.Instance.LoadScene("MainMenu");
        }
        else
        {
            Debug.LogError("[GameManager] GameSceneLoader instance not found! Cannot return to MainMenu with fade. Loading directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
        // Setting the state here is good as it defines the intent before the transition begins.
        // OnSceneLoaded for "MainMenu" will also confirm this state.
        SetGameState(GameState.MainMenu);
    }
    
    public static void SetNextStartingLevel(BiomeData biome)
    {
        SelectedStartBiomeForNextRun = biome;
        if (biome != null)
        {
            // Calculate the starting height based on the biome's minTileIndex.
            _forcedInitialHeightForNextRun = biome.minTileIndex * TILE_VERTICAL_SIZE;
            Debug.Log($"[GameManager] Next level set to: {biome.biomeName}. Calculated start height: {_forcedInitialHeightForNextRun.Value}");
        }
        else
        {
            _forcedInitialHeightForNextRun = null; // Standard start from InitialSpawnPosition
            Debug.Log($"[GameManager] Next level set to default start (InitialSpawnPosition will be used).");
        }
        // Crucially, selecting a new level (or default start) overrides any previous in-game checkpoint for the *next* run.
        _lastCheckpointPosition = null;
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
        _lastCheckpointPosition = position; // Use the private variable
        // If an in-game checkpoint is hit, it means we are past any "selected level start" for the current run.
        // These become irrelevant for the current session once an actual checkpoint is saved.
        SelectedStartBiomeForNextRun = null;
        _forcedInitialHeightForNextRun = null;
        Debug.Log($"[GameManager] In-game checkpoint set to: {position}. Selected level start info cleared for this session.");
    }

    public static Vector3 GetRespawnPosition()
    {
        if (_lastCheckpointPosition.HasValue) // Prioritize actual checkpoints hit during gameplay
        {
            Debug.Log($"[GameManager] Using last in-game checkpoint: {_lastCheckpointPosition.Value}");
            return _lastCheckpointPosition.Value;
        }
        if (_forcedInitialHeightForNextRun.HasValue) // Then, prioritize selected level start height
        {
            // Use the X and Z from InitialSpawnPosition, but Y from the forced height
            Vector3 forcedStartPos = new Vector3(InitialSpawnPosition.x, _forcedInitialHeightForNextRun.Value, InitialSpawnPosition.z);
            Debug.Log($"[GameManager] Using forced initial height for selected level: {forcedStartPos}");
            return forcedStartPos;
        }
        Debug.Log($"[GameManager] Using initial spawn position: {InitialSpawnPosition}");
        return InitialSpawnPosition; // Fallback to absolute initial spawn
    }

    // Add this method to reset the checkpoint state
    public static void ResetSessionForNewGame()
    {
        _lastCheckpointPosition = null;
        SelectedStartBiomeForNextRun = null; // Clear selected level too
        _forcedInitialHeightForNextRun = null;
        Debug.Log("[GameManager] Session reset for new game (checkpoints and selected level choice cleared for next run).");
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