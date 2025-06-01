using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [Tooltip("Duration of the fade in seconds.")]
    [SerializeField] private float fadeDuration = 0.75f; // You can adjust this
    [Tooltip("CanvasGroup component for fading. Should be on the same GameObject or assigned.")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    private string _targetSceneName;
    private Coroutine _currentFadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (fadeCanvasGroup == null)
            {
                fadeCanvasGroup = GetComponent<CanvasGroup>();
                if (fadeCanvasGroup == null)
                {
                    // Adding one if not present, ensure your prefab has it for best results.
                    Debug.LogWarning("SceneFader: CanvasGroup not found, adding one. Please ensure your SceneFader prefab has a CanvasGroup component.");
                    fadeCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
            // Ensure it starts transparent and non-blocking, unless it's mid-transition from a previous scene load.
            // If SceneManager.sceneLoaded triggers FadeOut, alpha might be 1 initially.
            // For the very first launch, it should be 0.
            // This will be handled by FadeOut in OnSceneLoaded if coming from a black screen.
            // If starting fresh, it should be transparent. Let's ensure it's 0 on first Awake.
            if (SceneManager.GetActiveScene().buildIndex == 0 && fadeCanvasGroup.alpha != 0) // Example: if it's the initial scene
            {
                //fadeCanvasGroup.alpha = 0f; // Start fully transparent only on very first game load
            }


        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoadedAction;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedAction;
    }

    public void TransitionToScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneFader: Target scene name is null or empty!");
            return;
        }
        _targetSceneName = sceneName;

        if (_currentFadeCoroutine != null)
        {
            StopCoroutine(_currentFadeCoroutine);
        }
        _currentFadeCoroutine = StartCoroutine(FadeInAndLoad());
    }

    private IEnumerator FadeInAndLoad()
    {
        yield return StartCoroutine(FadeCanvas(1f, fadeDuration)); // Fade In

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(_targetSceneName);
        // We don't need to wait for asyncLoad.isDone here because
        // OnSceneLoadedAction will trigger the fade out once the scene is actually loaded and active.
        yield return null; // Allow the scene loading to begin
    }

    private void OnSceneLoadedAction(Scene scene, LoadSceneMode mode)
    {
        // If the screen is opaque (we just faded in), then fade out
        if (fadeCanvasGroup.alpha >= 0.99f)
        {
             if (_currentFadeCoroutine != null)
             {
                StopCoroutine(_currentFadeCoroutine);
             }
            _currentFadeCoroutine = StartCoroutine(FadeCanvas(0f, fadeDuration)); // Fade Out
        }
        // If for some reason OnSceneLoaded is called and the screen isn't black (e.g. first app load),
        // ensure it's transparent.
        else if (fadeCanvasGroup.alpha > 0f && fadeCanvasGroup.alpha < 0.99f)
        {
            // This might happen if a scene is loaded by other means while a fade was partial.
            // Or if the game starts not in an initial scene and SceneFader exists.
            // Generally, we want to ensure it becomes transparent if the scene loads and it wasn't fully black.
             if (_currentFadeCoroutine != null)
             {
                StopCoroutine(_currentFadeCoroutine);
             }
            _currentFadeCoroutine = StartCoroutine(FadeCanvas(0f, fadeDuration * fadeCanvasGroup.alpha)); // Faster fade out if partially visible
        }
    }

    private IEnumerator FadeCanvas(float targetAlpha, float duration)
    {
        fadeCanvasGroup.blocksRaycasts = true; // Block interaction during any fade activity

        float currentTime = 0f;
        float startAlpha = fadeCanvasGroup.alpha;

        // Ensure duration is not zero to avoid division by zero
        if (duration <= 0) duration = 0.01f;


        while (currentTime < duration)
        {
            currentTime += Time.unscaledDeltaTime; // Use unscaledDeltaTime for smooth fading even if Time.timeScale is 0
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, currentTime / duration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha; // Ensure target alpha is set
        fadeCanvasGroup.blocksRaycasts = (targetAlpha >= 0.5f); // Only block raycasts if significantly opaque
    }
}