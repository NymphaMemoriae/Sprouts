using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Displays the Unity console log messages on screen. Useful for debugging on devices.
/// Attach this script to a persistent GameObject (like your GameManager).
/// </summary>
public class InGameConsole : MonoBehaviour
{
    #region Inspector Settings
    [Header("Configuration")]
    [Tooltip("Maximum number of log messages to keep in memory and display.")]
    [SerializeField] private int maxMessages = 100;
    [Tooltip("Height of the console window in pixels.")]
    [SerializeField] private float consoleHeight = 200f;
    [Tooltip("Should the console be visible by default?")]
    [SerializeField] private bool startVisible = true;
    [Tooltip("Key to toggle console visibility (only works in Editor/Standalone).")]
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote; // Usually the `~` key

    [Header("UI Appearance")]
    [SerializeField] private Color logColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color errorColor = Color.red;
    [SerializeField] private Color exceptionColor = Color.magenta;
    [SerializeField] private int fontSize = 14;
    #endregion

    #region Private Fields
    private struct LogMessage
    {
        public string Message;
        public string StackTrace;
        public LogType Type;
        public Color Color; // Store color for easier drawing
    }

    private readonly List<LogMessage> logMessages = new List<LogMessage>();
    private Vector2 scrollPosition;
    private bool isVisible;
    private GUIStyle consoleStyle;
    private GUIStyle logStyle; // Style for individual log entries
    #endregion

    #region Singleton Pattern (Optional but Recommended)
    // Basic singleton pattern to ensure only one instance exists
    // and potentially make it persistent across scenes.
    public static InGameConsole Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: Uncomment the next line if you want the console
            // to persist across scene loads (like your GameManager).
            // DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            // Destroy duplicate instances if one already exists
            Destroy(gameObject);
            return; // Stop further execution for this duplicate instance
        }

        isVisible = startVisible;
    }
    #endregion

    #region Log Handling
    private void OnEnable()
    {
        // Subscribe to the log message stream
        // Use logMessageReceivedThreaded for thread safety
        Application.logMessageReceivedThreaded += HandleLog;
    }

    private void OnDisable()
    {
        // Unsubscribe when the object is disabled or destroyed
        Application.logMessageReceivedThreaded -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        Color color = logColor; // Default color
        switch (type)
        {
            case LogType.Warning:
                color = warningColor;
                break;
            case LogType.Error:
                color = errorColor;
                break;
            case LogType.Exception:
                color = exceptionColor;
                break;
            case LogType.Assert: // Often treated like errors
                color = errorColor;
                break;
        }

        // Create the log message struct
        LogMessage message = new LogMessage
        {
            Message = logString,
            StackTrace = stackTrace,
            Type = type,
            Color = color
        };

        // Add the message to the list (needs thread safety if accessed outside main thread,
        // but OnGUI runs on main thread, so direct add is usually okay here)
        // For extreme safety, you could use a ConcurrentQueue and process it in Update,
        // but let's keep it simple for now.
        lock (logMessages) // Lock for thread safety when modifying the list
        {
            logMessages.Add(message);

            // Limit the number of messages
            while (logMessages.Count > maxMessages)
            {
                logMessages.RemoveAt(0);
            }
        }

        // Optional: Automatically scroll to the bottom when a new message arrives
        // scrollPosition.y = float.MaxValue; // Might be slightly jittery, handle with care
    }
    #endregion

    #region Update Loop (for Input)
    private void Update()
    {
        // Toggle visibility with the specified key (works in Editor/Standalone)
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
        }

        // Example: Add a button toggle for mobile (requires UI setup)
        // You would typically call ToggleVisibility() from a UI Button's OnClick event.
    }

    /// <summary>
    /// Public method to toggle visibility, callable from UI Buttons etc.
    /// </summary>
    public void ToggleVisibility()
    {
        isVisible = !isVisible;
    }
    #endregion

    #region OnGUI Drawing
    private void OnGUI()
    {
        // Only draw if visible and only in development builds or editor
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        if (!isVisible)
        {
            return;
        }

        // Initialize GUI styles if they haven't been created yet
        // This is done here because GUIStyles can only be used reliably inside OnGUI
        if (consoleStyle == null)
        {
            consoleStyle = new GUIStyle(GUI.skin.box);
            consoleStyle.alignment = TextAnchor.UpperLeft;
            // Add a background color - dark semi-transparent is common
            Texture2D backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.1f, 0.85f)); // Dark grey, mostly opaque
            backgroundTexture.Apply();
            consoleStyle.normal.background = backgroundTexture;
        }

        if (logStyle == null)
        {
            logStyle = new GUIStyle(GUI.skin.label);
            logStyle.fontSize = fontSize;
            logStyle.wordWrap = true; // Wrap long lines
        }


        // --- Draw Console Window ---
        // Define the rectangle for the console area at the bottom of the screen
        float windowWidth = Screen.width;
        Rect consoleRect = new Rect(0, Screen.height - consoleHeight, windowWidth, consoleHeight);

        // Draw the background box
        GUI.Box(consoleRect, "", consoleStyle);

        // Define the inner view area for scrolling content
        // Add some padding inside the box
        float padding = 5f;
        Rect viewRect = new Rect(
            consoleRect.x + padding,
            consoleRect.y + padding,
            consoleRect.width - (padding * 2) - 20, // Subtract scrollbar width guess
            consoleRect.height - (padding * 2)
        );

        // Calculate the height needed for all log messages
        // This is an estimation; might need adjustment based on actual text height
        float innerScrollHeight = 0;
        lock (logMessages) // Lock when accessing the list
        {
             foreach (var message in logMessages)
             {
                 // Estimate height based on font size and message length (very rough)
                 innerScrollHeight += logStyle.CalcHeight(new GUIContent(message.Message), viewRect.width) + 2; // Add spacing
             }
        }


        // Define the scrollable content area size
        Rect scrollContentRect = new Rect(0, 0, viewRect.width, Mathf.Max(viewRect.height, innerScrollHeight));

        // Begin the scroll view
        scrollPosition = GUI.BeginScrollView(viewRect, scrollPosition, scrollContentRect, false, true);

        // --- Draw Log Messages ---
        float currentY = 0f;
        lock (logMessages) // Lock again when iterating
        {
            foreach (var message in logMessages)
            {
                // Set color based on log type
                logStyle.normal.textColor = message.Color;

                // Calculate height for this specific message
                float messageHeight = logStyle.CalcHeight(new GUIContent(message.Message), viewRect.width);
                Rect messageRect = new Rect(0, currentY, viewRect.width, messageHeight);

                // Draw the log message
                GUI.Label(messageRect, message.Message, logStyle);

                // Optionally draw stack trace for errors/exceptions (can make console very tall)
                // if ((message.Type == LogType.Error || message.Type == LogType.Exception) && !string.IsNullOrEmpty(message.StackTrace))
                // {
                //     GUIStyle stackTraceStyle = new GUIStyle(logStyle);
                //     stackTraceStyle.fontSize = Mathf.Max(10, fontSize - 2); // Smaller font for stack
                //     stackTraceStyle.normal.textColor = new Color(message.Color.r * 0.8f, message.Color.g * 0.8f, message.Color.b * 0.8f); // Dimmed color
                //     float stackHeight = stackTraceStyle.CalcHeight(new GUIContent(message.StackTrace), viewRect.width);
                //     Rect stackRect = new Rect(10, currentY + messageHeight, viewRect.width - 10, stackHeight);
                //     GUI.Label(stackRect, message.StackTrace, stackTraceStyle);
                //     currentY += stackHeight;
                // }

                currentY += messageHeight + 2; // Move down for the next message, add small spacing
            }
        }

        // End the scroll view
        GUI.EndScrollView();

        // --- Optional: Add Buttons (Clear, Close) ---
        float buttonHeight = 25f;
        float buttonWidth = 60f;

        // Close Button (Top Right)
        if (GUI.Button(new Rect(consoleRect.x + consoleRect.width - buttonWidth - 5, consoleRect.y + 5, buttonWidth, buttonHeight), "Close"))
        {
            isVisible = false;
        }

        // Clear Button (Next to Close)
        if (GUI.Button(new Rect(consoleRect.x + consoleRect.width - (buttonWidth * 2) - 10, consoleRect.y + 5, buttonWidth, buttonHeight), "Clear"))
        {
            lock (logMessages)
            {
                logMessages.Clear();
            }
        }

#endif // DEVELOPMENT_BUILD || UNITY_EDITOR
    }
    #endregion
}