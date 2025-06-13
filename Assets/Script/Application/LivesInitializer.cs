using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Lives initializer helper component, ensures that lives display is correctly initialized
/// </summary>
public class LivesInitializer : MonoBehaviour
{
    [Header("Reference Settings")]
    public PlayerHealth playerHealth;
    public LivesDisplay livesDisplay;
    public TextMeshProUGUI livesTMP;
    public Text livesText;
    
    [Header("Initial Value Settings")]
    public int initialLives = 10;
    public int maxLives = 10;
    public string prefixText = "Lives: ";
    
    [Header("Execution Settings")]
    public bool initializeOnAwake = true;
    public bool initializeOnStart = true;
    public bool initializeOnEnable = true;
    public float initializationDelay = 0.1f;
    
    void Awake()
    {
        if (initializeOnAwake)
        {
            Initialize();
        }
    }
    
    void Start()
    {
        if (initializeOnStart)
        {
            Initialize();
        }
        
        // Delayed initialization to ensure all components are ready
        if (initializationDelay > 0)
        {
            Invoke("DelayedInitialize", initializationDelay);
        }
    }
    
    void OnEnable()
    {
        if (initializeOnEnable)
        {
            Initialize();
        }
    }
    
    /// <summary>
    /// Initialize lives display
    /// </summary>
    public void Initialize()
    {
        Debug.Log("[LivesInitializer] Starting to initialize lives display");
        
        // Find references (if not set yet)
        FindReferences();
        
        // Initialize PlayerHealth
        if (playerHealth != null)
        {
            Debug.Log($"[LivesInitializer] Setting PlayerHealth lives: {initialLives}/{maxLives}");
            playerHealth.SetLives(initialLives);
        }
        
        // Initialize LivesDisplay
        if (livesDisplay != null)
        {
            Debug.Log("[LivesInitializer] Force updating LivesDisplay");
            livesDisplay.ForceUpdateDisplay();
        }
        
        // Directly set text (as a fallback)
        string displayText = $"{prefixText}{initialLives}/{maxLives}";
        
        if (livesTMP != null)
        {
            Debug.Log($"[LivesInitializer] Directly setting TMP text: {displayText}");
            livesTMP.text = displayText;
        }
        
        if (livesText != null)
        {
            Debug.Log($"[LivesInitializer] Directly setting Text text: {displayText}");
            livesText.text = displayText;
        }
    }
    
    /// <summary>
    /// Delayed initialization
    /// </summary>
    private void DelayedInitialize()
    {
        Debug.Log("[LivesInitializer] Executing delayed initialization");
        Initialize();
    }
    
    /// <summary>
    /// Find references
    /// </summary>
    private void FindReferences()
    {
        // Find PlayerHealth (if not set yet)
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
            Debug.Log(playerHealth != null 
                ? "[LivesInitializer] Found PlayerHealth component" 
                : "[LivesInitializer] PlayerHealth component not found");
        }
        
        // Find LivesDisplay (if not set yet)
        if (livesDisplay == null)
        {
            livesDisplay = FindObjectOfType<LivesDisplay>();
            Debug.Log(livesDisplay != null 
                ? "[LivesInitializer] Found LivesDisplay component" 
                : "[LivesInitializer] LivesDisplay component not found");
        }
        
        // Find TextMeshPro (if not set yet)
        if (livesTMP == null)
        {
            if (livesDisplay != null && livesDisplay.livesTMP != null)
            {
                livesTMP = livesDisplay.livesTMP;
                Debug.Log("[LivesInitializer] Got TMP reference from LivesDisplay");
            }
            else
            {
                livesTMP = GetComponentInChildren<TextMeshProUGUI>();
                if (livesTMP == null)
                {
                    // Try to find in current GameObject or child objects
                    livesTMP = FindObjectOfType<TextMeshProUGUI>();
                }
                Debug.Log(livesTMP != null 
                    ? "[LivesInitializer] Found TMP component" 
                    : "[LivesInitializer] TMP component not found");
            }
        }
        
        // Find Text (if not set yet)
        if (livesText == null)
        {
            if (livesDisplay != null && livesDisplay.livesText != null)
            {
                livesText = livesDisplay.livesText;
                Debug.Log("[LivesInitializer] Got Text reference from LivesDisplay");
            }
            else
            {
                livesText = GetComponentInChildren<Text>();
                if (livesText == null)
                {
                    // Try to find in current GameObject or child objects
                    livesText = FindObjectOfType<Text>();
                }
                Debug.Log(livesText != null 
                    ? "[LivesInitializer] Found Text component" 
                    : "[LivesInitializer] Text component not found");
            }
        }
    }
} 