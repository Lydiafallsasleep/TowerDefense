using UnityEngine;
using UnityEngine.UI;
using TMPro; // Add TextMeshPro namespace
using System.Collections;

/// <summary>
/// Lives display component, enhances the visual display of player lives
/// </summary>
public class LivesDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    public Text livesText; // Traditional UGUI Text component
    public TextMeshProUGUI livesTMP; // TextMeshPro UGUI component
    public bool showAsPercentage = false; // Whether to display as percentage
    public string prefixText = "Lives: "; // Prefix text
    
    [Header("Animation Settings")]
    public bool animateChanges = true; // Whether to animate changes
    public float animationDuration = 0.5f; // Animation duration
    public Color damageColor = new Color(1f, 0.3f, 0.3f); // Damage color
    public Color healColor = new Color(0.3f, 1f, 0.3f); // Heal color
    
    [Header("Blink Warning")]
    public bool enableLowHealthWarning = true; // Whether to enable low health warning
    public int lowHealthThreshold = 3; // Low health threshold
    public float warningBlinkRate = 1.5f; // Warning blink rate
    
    [Header("Debug Settings")]
    public bool initializeWithDefaultValues = true; // Whether to initialize with default values
    public int defaultLives = 10; // Default lives
    public int defaultMaxLives = 10; // Default maximum lives
    
    private PlayerHealth playerHealth; // Player health component
    private int lastLives; // Last lives value
    private Coroutine blinkCoroutine; // Blink coroutine
    private Color originalTextColor; // Original text color
    private Color originalTMPColor; // Original TMP text color
    private bool usingTMP = false; // Whether using TMP
    
    void Awake()
    {
        Debug.Log("[LivesDisplay] Awake - Initializing component");
        
        // Check which text component to use
        usingTMP = livesTMP != null;
        
        if (usingTMP)
            Debug.Log("[LivesDisplay] Using TMP text component");
        else
            Debug.Log("[LivesDisplay] Using standard Text component");
        
        // Save original text color
        if (livesText != null)
        {
            originalTextColor = livesText.color;
        }
        if (livesTMP != null)
        {
            originalTMPColor = livesTMP.color;
        }
        
        // If default value initialization is enabled, immediately display default values
        if (initializeWithDefaultValues)
        {
            Debug.Log($"[LivesDisplay] Initializing with default values: {defaultLives}/{defaultMaxLives}");
            UpdateDisplay(defaultLives, defaultMaxLives, false);
        }
    }
    
    void Start()
    {
        Debug.Log("[LivesDisplay] Start - Finding PlayerHealth component");
        
        // Get player health component
        playerHealth = FindObjectOfType<PlayerHealth>();
        
        // If PlayerHealth component is found, connect to lives changed event
        if (playerHealth != null)
        {
            Debug.Log($"[LivesDisplay] Found PlayerHealth component, current lives: {playerHealth.GetCurrentLives()}/{playerHealth.GetMaxLives()}");
            playerHealth.OnLivesChanged += OnLivesChanged;
            
            // Initialize last lives
            lastLives = playerHealth.GetCurrentLives();
            
            // Initial display update
            UpdateDisplay(playerHealth.GetCurrentLives(), playerHealth.GetMaxLives(), false);
        }
        else
        {
            Debug.LogWarning("[LivesDisplay] PlayerHealth component not found, using default values");
            
            // If PlayerHealth component not found, try to get from GameManager
            if (GameManager.Instance != null)
            {
                int currentLives = GameManager.Instance.GetCurrentLives();
                int maxLives = GameManager.Instance.maxLives;
                Debug.Log($"[LivesDisplay] Getting lives from GameManager: {currentLives}/{maxLives}");
                UpdateDisplay(currentLives, maxLives, false);
            }
            else if (initializeWithDefaultValues)
            {
                // Use default values
                Debug.Log($"[LivesDisplay] Using default values: {defaultLives}/{defaultMaxLives}");
                UpdateDisplay(defaultLives, defaultMaxLives, false);
            }
        }
        
        // Ensure display is not empty
        if (livesTMP != null && string.IsNullOrEmpty(livesTMP.text))
        {
            Debug.LogWarning("[LivesDisplay] TMP text is empty, using default values");
            livesTMP.text = $"{prefixText}{defaultLives}/{defaultMaxLives}";
        }
        
        if (livesText != null && string.IsNullOrEmpty(livesText.text))
        {
            Debug.LogWarning("[LivesDisplay] Text is empty, using default values");
            livesText.text = $"{prefixText}{defaultLives}/{defaultMaxLives}";
        }
    }
    
    void OnEnable()
    {
        // Ensure correct values are displayed when component is enabled
        if (playerHealth != null)
        {
            UpdateDisplay(playerHealth.GetCurrentLives(), playerHealth.GetMaxLives(), false);
        }
        else if (initializeWithDefaultValues)
        {
            UpdateDisplay(defaultLives, defaultMaxLives, false);
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (playerHealth != null)
        {
            playerHealth.OnLivesChanged -= OnLivesChanged;
        }
        
        // Stop all coroutines
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
    }
    
    /// <summary>
    /// Handle lives changed event
    /// </summary>
    private void OnLivesChanged(int currentLives, int maxLives)
    {
        Debug.Log($"[LivesDisplay] Received lives changed event: {currentLives}/{maxLives} (previous: {lastLives})");
        
        // Check if lives decreased
        bool isDecrease = currentLives < lastLives;
        
        // Update display
        UpdateDisplay(currentLives, maxLives, isDecrease);
        
        // Update last lives
        lastLives = currentLives;
    }
    
    /// <summary>
    /// Update display
    /// </summary>
    private void UpdateDisplay(int currentLives, int maxLives, bool isDecrease)
    {
        Debug.Log($"[LivesDisplay] Updating display: {currentLives}/{maxLives}");
        
        // Build display text
        string displayText;
        if (showAsPercentage)
        {
            float percentage = (float)currentLives / maxLives * 100f;
            displayText = $"{prefixText}{percentage:F0}%";
        }
        else
        {
            displayText = $"{prefixText}{currentLives}/{maxLives}";
        }
        
        // Set text
        if (livesText != null)
        {
            livesText.text = displayText;
            Debug.Log($"[LivesDisplay] Updated Text: {displayText}");
        }
        if (livesTMP != null)
        {
            livesTMP.text = displayText;
            Debug.Log($"[LivesDisplay] Updated TMP: {displayText}");
        }
        
        // If animation is enabled and lives changed
        if (animateChanges && lastLives != currentLives && lastLives != 0)
        {
            // Stop any running animation
            StopAllCoroutines();
            
            // Determine color based on whether lives decreased or increased
            Color targetColor = isDecrease ? damageColor : healColor;
            
            // Start animation
            StartCoroutine(AnimateTextColor(targetColor));
        }
        
        // Check for low health warning
        CheckLowHealthWarning(currentLives, maxLives);
    }
    
    /// <summary>
    /// Animate text color change
    /// </summary>
    private IEnumerator AnimateTextColor(Color targetColor)
    {
        Debug.Log($"[LivesDisplay] Animating text color to {targetColor}");
        
        // Set initial color
        if (livesText != null)
        {
            livesText.color = targetColor;
        }
        if (livesTMP != null)
        {
            livesTMP.color = targetColor;
        }
        
        float elapsedTime = 0f;
        
        // Gradually return to original color
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            
            // Calculate interpolated color
            Color currentColor = Color.Lerp(targetColor, usingTMP ? originalTMPColor : originalTextColor, t);
            
            // Apply color
            if (livesText != null)
            {
                livesText.color = currentColor;
            }
            if (livesTMP != null)
            {
                livesTMP.color = currentColor;
            }
            
            yield return null;
        }
        
        // Ensure final color is set
        if (livesText != null)
        {
            livesText.color = originalTextColor;
        }
        if (livesTMP != null)
        {
            livesTMP.color = originalTMPColor;
        }
        
        Debug.Log("[LivesDisplay] Text color animation complete");
    }
    
    /// <summary>
    /// Check for low health warning
    /// </summary>
    private void CheckLowHealthWarning(int currentLives, int maxLives)
    {
        // If low health warning is enabled
        if (enableLowHealthWarning)
        {
            // Stop existing blink coroutine
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
                
                // Reset colors
                if (livesText != null)
                {
                    livesText.color = originalTextColor;
                }
                if (livesTMP != null)
                {
                    livesTMP.color = originalTMPColor;
                }
            }
            
            // Check if current lives is below threshold
            if (currentLives <= lowHealthThreshold && currentLives > 0)
            {
                Debug.Log($"[LivesDisplay] Low health warning activated: {currentLives} <= {lowHealthThreshold}");
                // Start blink coroutine
                blinkCoroutine = StartCoroutine(BlinkText());
            }
        }
    }
    
    /// <summary>
    /// Blink text for low health warning
    /// </summary>
    private IEnumerator BlinkText()
    {
        Debug.Log("[LivesDisplay] Starting text blink effect");
        
        while (true)
        {
            // Switch between warning color and original color
            if (livesText != null)
            {
                livesText.color = livesText.color == originalTextColor ? damageColor : originalTextColor;
            }
            if (livesTMP != null)
            {
                livesTMP.color = livesTMP.color == originalTMPColor ? damageColor : originalTMPColor;
            }
            
            // Wait for next blink
            yield return new WaitForSeconds(1f / warningBlinkRate);
        }
    }
    
    /// <summary>
    /// Force update display (can be called from external scripts)
    /// </summary>
    public void ForceUpdateDisplay()
    {
        Debug.Log("[LivesDisplay] Force updating display");
        
        if (playerHealth != null)
        {
            UpdateDisplay(playerHealth.GetCurrentLives(), playerHealth.GetMaxLives(), false);
        }
        else if (GameManager.Instance != null)
        {
            int currentLives = GameManager.Instance.GetCurrentLives();
            int maxLives = GameManager.Instance.maxLives;
            UpdateDisplay(currentLives, maxLives, false);
        }
        else
        {
            UpdateDisplay(defaultLives, defaultMaxLives, false);
        }
    }
}