using UnityEngine;
using UnityEngine.UI;
using TMPro; // Add TextMeshPro namespace
using System.Collections;
using UnityEngine.SceneManagement;
using JetBrains.Annotations;

/// <summary>
/// Player health system, manages player lives and related UI display
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxLives = 10;
    private int currentLives;
    
    [Header("UI References")]
    public Text livesText; // Traditional UGUI Text
    public TextMeshProUGUI livesTMP; // TextMeshPro UGUI
    
    [Header("Damage Effects")]
    public GameObject damageEffect; // Damage visual effect
    public AudioClip damageSound; // Damage sound effect
    public float screenShakeDuration = 0.5f; // Screen shake duration
    public float screenShakeIntensity = 0.2f; // Screen shake intensity
    
    [Header("Game Over Settings")]
    public GameObject gameOverPanel; // Game over panel
    
    // Events
    public delegate void LivesChangedHandler(int currentLives, int maxLives);
    public event LivesChangedHandler OnLivesChanged;
    
    public delegate void GameOverHandler();
    public event GameOverHandler OnGameOver;
    
    private AudioSource audioSource;
    private GameManager gameManager;
    private bool isInvulnerable = false;
    private bool isGameOver = false;
    
    void Awake()
    {
        Debug.Log("[PlayerHealth] Awake - Initializing components");
        
        // Set initial lives, ensure it's not zero
        currentLives = maxLives;
        
        
        gameManager = GameManager.Instance;
        
        // If game manager exists, sync max lives
        if (gameManager != null)
        {
            Debug.Log($"[PlayerHealth] Awake - Getting lives from GameManager: {gameManager.maxLives}");
            maxLives = gameManager.maxLives;
            currentLives = maxLives;
        }
        
        
        // Ensure game over panel is initially hidden
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] Cannot find GameOverPanel! Please make sure this panel exists in the scene");
        }
        
        // Immediately update UI to ensure correct lives display at Awake stage
        UpdateLivesUI();
    }
    
    void Start()
    {
        Debug.Log($"[PlayerHealth] Start - Current lives: {currentLives}/{maxLives}");
        
        // Again ensure lives won't be zero
        if (currentLives <= 0)
        {
            currentLives = maxLives;
            Debug.LogWarning("[PlayerHealth] Detected lives as 0, reset to max value");
        }
        
        // Update UI display
        UpdateLivesUI();
        
        // Manually trigger lives change event to ensure all subscribers receive the latest lives
        if (OnLivesChanged != null)
        {
            Debug.Log("[PlayerHealth] Triggering OnLivesChanged event");
            OnLivesChanged.Invoke(currentLives, maxLives);
        }
    }
    
    void OnEnable()
    {
        // Ensure UI updates when component is enabled
        UpdateLivesUI();
    }
    
    /// <summary>
    /// Called when player takes damage
    /// </summary>
    /// <param name="damage">Damage amount</param>
    public void TakeDamage(int damage)
    {
        // If game is over or player is invulnerable, don't take damage
        if (isGameOver || isInvulnerable) return;
        
        Debug.Log($"[PlayerHealth] Taking damage: {damage}, current lives: {currentLives} -> {currentLives - damage}");
        
        // Reduce lives
        currentLives = Mathf.Max(0, currentLives - damage);
        
        // Play damage effects
        PlayDamageEffects();
        
        // Update UI
        UpdateLivesUI();
        
        // Trigger event
        if (OnLivesChanged != null)
        {
            OnLivesChanged.Invoke(currentLives, maxLives);
        }
        
        // Check if game over
        if (currentLives <= 0)
        {
            TriggerGameOver();
        }
        else
        {
            // Only apply brief invulnerability if not game over
            StartCoroutine(InvulnerabilityPeriod(1.0f));
        }
    }
    
    /// <summary>
    /// Restore lives
    /// </summary>
    /// <param name="amount">Amount to restore</param>
    
    /// <summary>
    /// Update lives UI display
    /// </summary>
    private void UpdateLivesUI()
    {
        // Ensure lives are within valid range
        currentLives = Mathf.Clamp(currentLives, 0, maxLives);
        
        // Update text display
        if (livesText != null)
        {
            livesText.text = $"Lives: {currentLives}/{maxLives}";
            Debug.Log($"[PlayerHealth] Updated UI Text: {livesText.text}");
        }
        
        // Update TextMeshPro text display
        if (livesTMP != null)
        {
            livesTMP.text = $"Lives: {currentLives}/{maxLives}";
            Debug.Log($"[PlayerHealth] Updated UI TMP: {livesTMP.text}");
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] livesTMP reference is null");
        }
    }
    
    /// <summary>
    /// Play damage effects
    /// </summary>
    private void PlayDamageEffects()
    {
        // Play damage sound
        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
        
        // Show damage effect
        if (damageEffect != null)
        {
            GameObject effect = Instantiate(damageEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f); // Destroy after 2 seconds
        }
        
        // Shake screen
        StartCoroutine(ShakeScreen());
        
        // Find and activate damage flash effect if present
        ScreenDamageEffect damageFlash = FindObjectOfType<ScreenDamageEffect>();
        if (damageFlash != null)
        {
            damageFlash.ShowDamageEffect();
        }
    }
    
    /// <summary>
    /// Shake the camera when taking damage
    /// </summary>
    private IEnumerator ShakeScreen()
    {
        // Find the main camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            yield break;
        }
        
        Vector3 originalPosition = mainCamera.transform.position;
        float elapsed = 0f;
        
        while (elapsed < screenShakeDuration)
        {
            float x = Random.Range(-1f, 1f) * screenShakeIntensity;
            float y = Random.Range(-1f, 1f) * screenShakeIntensity;
            
            mainCamera.transform.position = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        mainCamera.transform.position = originalPosition;
    }
    
    /// <summary>
    /// Create a brief invulnerability period after taking damage
    /// </summary>
    private IEnumerator InvulnerabilityPeriod(float duration)
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(duration);
        isInvulnerable = false;
    }
    
    /// <summary>
    /// Get current lives
    /// </summary>
    public int GetCurrentLives()
    {
        return currentLives;
    }
    
    /// <summary>
    /// Get maximum lives
    /// </summary>
    public int GetMaxLives()
    {
        return maxLives;
    }
    
    /// <summary>
    /// Set lives to a specific value
    /// </summary>
    public void SetLives(int lives)
    {
        if (lives < 0)
        {
            Debug.LogWarning("[PlayerHealth] Attempted to set negative lives value, clamping to 0");
            lives = 0;
        }
        
        if (lives > maxLives)
        {
            Debug.LogWarning("[PlayerHealth] Attempted to set lives above max, clamping to max");
            lives = maxLives;
        }
        
        int oldLives = currentLives;
        currentLives = lives;
        
        Debug.Log($"[PlayerHealth] Lives set from {oldLives} to {currentLives}");
        
        // Update UI
        UpdateLivesUI();
        
        // Trigger event
        if (OnLivesChanged != null)
        {
            OnLivesChanged.Invoke(currentLives, maxLives);
        }
    }
    
    /// <summary>
    /// Trigger game over state
    /// </summary>
    private void TriggerGameOver()
    {
        // Avoid triggering multiple times
       // if (isGameOver) return;
        
        isGameOver = true;
        Debug.Log("[PlayerHealth] Game Over triggered!");
        
        // Trigger event
        if (OnGameOver != null)
        {
            OnGameOver.Invoke();
        }
        
        // Notify GameManager
        if (gameManager != null)
        {
            gameManager.GameOver("Player died");
        }
        
        // Remove all enemies
        ClearAllEnemies();
        
        // Show game over panel after delay
        StartCoroutine(ShowGameOverPanel());
    }
    
    /// <summary>
    /// Get the full path of a GameObject in the hierarchy
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    /// <summary>
    /// Clear all enemies from the scene
    /// </summary>
    private void ClearAllEnemies()
    {
        Debug.Log("[PlayerHealth] Clearing all enemies");
        
        // Find all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        int enemyCount = enemies.Length;
        
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null)
            {
                // Try to get enemy component
                Enemy enemyComponent = enemy.GetComponent<Enemy>();
                if (enemyComponent != null)
                {
                    // Call enemy's own death method if available
                    enemyComponent.Die(false); // Don't award score
                }
                else
                {
                    // Otherwise just destroy the GameObject
                    Destroy(enemy);
                }
            }
        }
        
        Debug.Log($"[PlayerHealth] Cleared {enemyCount} enemies");
    }
    
    /// <summary>
    /// Show game over panel after a delay
    /// </summary>
    private IEnumerator ShowGameOverPanel()
    {

        if (gameOverPanel == null)
        {
            Debug.LogError("GameOverPanel reference is missing!");
            yield break;
        }

        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }
    
    /// <summary>
    /// Reset health to maximum value
    /// </summary>
    public void ResetHealth()
    {
        Debug.Log("[PlayerHealth] Resetting health");
        
        // Reset state
        isGameOver = false;
        isInvulnerable = false;
        
        // Reset lives
        currentLives = maxLives;
        
        // Hide game over panel if visible
        if (gameOverPanel != null && gameOverPanel.activeSelf)
        {
            gameOverPanel.SetActive(false);
        }
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Update UI
        UpdateLivesUI();
        
        // Trigger event
        if (OnLivesChanged != null)
        {
            OnLivesChanged.Invoke(currentLives, maxLives);
        }
        
        Debug.Log($"[PlayerHealth] Health reset to {currentLives}/{maxLives}");
    }
    
    /// <summary>
    /// Event handler for tower damage applied
    /// </summary>
    public void OnEnemyReachedEnd(Enemy enemy)
    {
        if (enemy == null) return;
        
        int damage = enemy.damageOnReachingEnd;
        Debug.Log($"[PlayerHealth] Enemy {enemy.name} reached end, applying {damage} damage");
        
        // Apply damage
        TakeDamage(damage);
        
        // Remove the enemy
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.OnDespawn(enemy.gameObject);
        }
        else
        {
            Destroy(enemy.gameObject);
        }
    }

    /// <summary>
    /// Restart the game
    /// </summary>
    public void RestartGame()
    {


        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
       
    }
    public void ExitGame()
    {
        SceneManager.LoadScene(0);
    }
}