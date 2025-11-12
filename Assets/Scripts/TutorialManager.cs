// TutorialManager.cs

using UnityEngine;
using System.Collections;
using TMPro; // For UI text

public class TutorialManager : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private PlantController plantController;
    [SerializeField] private TouchInputHandler touchInputHandler;
    [SerializeField] private UIManager uiManager; // You may need to add fields to UIManager for tutorial popups

    [Header("Tutorial UI Panels")]
    [SerializeField] private GameObject movementPromptPanel;
    [SerializeField] private GameObject uiPromptPanel;
    [SerializeField] private GameObject obstaclePromptPanel;
    [SerializeField] private GameObject buffPromptPanel;
    [SerializeField] private GameObject cameraPushPromptPanel;
    [SerializeField] private GameObject completionPanel;

    [Header("Tutorial Spawnables")]
    [SerializeField] private GameObject safeObstaclePrefab;
    [SerializeField] private GameObject damagingObstaclePrefab;
    [SerializeField] private GameObject speedBuffPrefab;

    void Start()
    {
        // Immediately start the tutorial sequence
        StartCoroutine(TutorialSequence());
    }

    private IEnumerator TutorialSequence()
    {
        // --- 1. Initial Setup ---
        // Disable player input and freeze the game
        touchInputHandler.enabled = false;
        Time.timeScale = 0f;

        // --- 2. Teach Movement ---
        yield return StartCoroutine(MovementTutorial());

        // --- 3. Explain the UI ---
        yield return StartCoroutine(UITutorial());

        // --- 4. Introduce Obstacles ---
        yield return StartCoroutine(ObstacleTutorial());

        // --- 5. Introduce Buffs ---
        yield return StartCoroutine(BuffTutorial());
        
        // --- 6. Teach Camera Push Mechanic ---
        yield return StartCoroutine(CameraPushTutorial());

        // --- 7. Completion ---
        ShowCompletion();
    }

    private IEnumerator MovementTutorial()
    {
        movementPromptPanel.SetActive(true);

        // Wait until the player touches the screen
        yield return new WaitUntil(() => Input.touchCount > 0 || Input.GetMouseButtonDown(0));

        movementPromptPanel.SetActive(false);
        
        // Unfreeze time and enable input
        Time.timeScale = 1f;
        touchInputHandler.enabled = true;
        
        // Set a slow, constant speed for this part of the tutorial
        // Using your existing methods is perfect!
        plantController.SetMaxVelocity(plantController.GetInitialGrowthSpeed());
        
        yield return new WaitForSeconds(1.5f); // Let player move for a moment
    }

    private IEnumerator UITutorial()
    {
        Time.timeScale = 0f;
        uiPromptPanel.SetActive(true); // This panel would have arrows pointing to the UI

        // Wait for a tap to continue
        yield return new WaitUntil(() => Input.GetMouseButtonDown(0));
        
        uiPromptPanel.SetActive(false);
        Time.timeScale = 1f;

        yield return new WaitForSeconds(1f);
    }

    private IEnumerator ObstacleTutorial()
    {
        // Show prompt: "Dodge the obstacles!"
        obstaclePromptPanel.SetActive(true);
        yield return new WaitForSeconds(2f);
        obstaclePromptPanel.SetActive(false);

        // Spawn a safe obstacle in a predictable spot
        Vector3 spawnPos = plantController.PlantHead.position + Vector3.up * 10f;
        Instantiate(safeObstaclePrefab, spawnPos, Quaternion.identity);

        yield return new WaitForSeconds(3f); // Give time to dodge

        // Show prompt: "Avoid the red ones!" (or similar)
        // You can reuse obstaclePromptPanel with different text
        Instantiate(damagingObstaclePrefab, plantController.PlantHead.position + Vector3.up * 12f, Quaternion.identity);

        yield return new WaitForSeconds(3f);
    }

    private IEnumerator BuffTutorial()
    {
        buffPromptPanel.SetActive(true);
        yield return new WaitForSeconds(2f);
        buffPromptPanel.SetActive(false);
        
        // Spawn a buff directly in their path so they can't miss it
        Vector3 buffSpawnPos = plantController.PlantHead.position + Vector3.up * 8f;
        buffSpawnPos.x = plantController.PlantHead.position.x; // Align horizontally
        Instantiate(speedBuffPrefab, buffSpawnPos, Quaternion.identity);

        yield return new WaitForSeconds(4f); // Let them feel the buff's effect
    }
    
    private IEnumerator CameraPushTutorial()
    {
        // Re-enable normal acceleration
        plantController.SetMaxVelocity(plantController.GetMaxGrowthSpeed());

        cameraPushPromptPanel.SetActive(true); // "Stop moving to see what happens!"
        touchInputHandler.enabled = false; // Force them to stop
        plantController.SetGrowing(false);
        plantController.SetHorizontalMovement(0);

        // Wait a few seconds for the camera's stagnation logic to kick in
        yield return new WaitForSeconds(3f); // Should be > stagnationTimeThreshold
        
        // You could add a check here to ensure the camera is actually pushing
        
        cameraPushPromptPanel.GetComponentInChildren<TextMeshProUGUI>().text = "The world pushes you if you stop! Keep growing!";

        yield return new WaitForSeconds(4f);
        
        cameraPushPromptPanel.SetActive(false);
        touchInputHandler.enabled = true;
    }


    private void ShowCompletion()
    {
        Time.timeScale = 0f;
        touchInputHandler.enabled = false;
        completionPanel.SetActive(true);
        
        // The completion panel should have a button that calls a public method
        // e.g., a button with an onClick that calls TutorialManager.EndTutorial()
    }

    public void EndTutorial()
    {
        // Make sure time is resumed before changing scenes
        Time.timeScale = 1f;

        // Use your existing GameManager logic to go to the main menu
        GameManager.Instance.ReturnToMainMenu();
    }
}