using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneLoader : MonoBehaviour
{
    public static GameSceneLoader Instance { get; private set; }

    [Header("Transition Settings")]
    [Tooltip("Assign your SceneFader prefab here.")]
    [SerializeField] private GameObject sceneFaderPrefab;   
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSceneFaderExists(); 
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void EnsureSceneFaderExists()
    {
        if (SceneFader.Instance == null) // Check if an instance already exists
        {
            if (sceneFaderPrefab != null)
            {
                Instantiate(sceneFaderPrefab);
                // SceneFader.Awake() will handle setting its own Instance and DontDestroyOnLoad.
                Debug.Log("[GameSceneLoader] SceneFader instance created from prefab.");
            }
            else
            {
                Debug.LogError("[GameSceneLoader] SceneFaderPrefab is not assigned in the GameSceneLoader Inspector! Fading transitions will not work.");
            }
        }
    }

    public void ReloadCurrentScene() // Renamed for clarity, implies fade
    {
        EnsureSceneFaderExists(); // Good practice to ensure it's there
        if (SceneFader.Instance != null)
        {
            Debug.Log($"[GameSceneLoader] Reloading current scene '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}' with fade.");
            SceneFader.Instance.TransitionToScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
        else
        {
            Debug.LogWarning("[GameSceneLoader] SceneFader not available. Reloading current scene directly.");
            int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneIndex);
        }
    }

    public void LoadScene(string sceneName) // Renamed for clarity, implies fade
    {
        EnsureSceneFaderExists();
        if (SceneFader.Instance != null)
        {
            Debug.Log($"[GameSceneLoader] Loading scene '{sceneName}' with fade.");
            SceneFader.Instance.TransitionToScene(sceneName);
        }
        else
        {
            Debug.LogWarning($"[GameSceneLoader] SceneFader not available. Loading scene '{sceneName}' directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }

    public void LoadScene(int sceneIndex) // Renamed for clarity, implies fade
    {
        EnsureSceneFaderExists();
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(sceneIndex));
        if (!string.IsNullOrEmpty(sceneName))
        {
            if (SceneFader.Instance != null)
            {
                Debug.Log($"[GameSceneLoader] Loading scene '{sceneName}' (index {sceneIndex}) with fade.");
                SceneFader.Instance.TransitionToScene(sceneName);
            }
            else
            {
                Debug.LogWarning($"[GameSceneLoader] SceneFader not available. Loading scene '{sceneName}' (index {sceneIndex}) directly.");
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneIndex);
            }
        }
        else
        {
            Debug.LogError($"[GameSceneLoader] Could not find scene name for build index {sceneIndex}. Loading by index directly as fallback.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneIndex); // Fallback to direct load by index
        }
    }
}
