using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Game victory panel controller
/// </summary>
public class WinPanel : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI scoreText; // Score display
    public TextMeshProUGUI waveText; // Wave display
    public Button continueButton; // Continue button
    public Button mainMenuButton; // Main menu button
    
    [Header("Save Settings")]
    public string highScoreKey = "HighScore"; // High score save key name
    
    private GameManager gameManager;
    private PlayerHealth playerHealth;
    private WaveManager waveManager;
    private bool inputEnabled = false;
    
    void Awake()
    {
        // Find GameManager
        gameManager = GameManager.Instance;
        
        // Find PlayerHealth
        playerHealth = FindObjectOfType<PlayerHealth>();
        
        // Find WaveManager
        waveManager = FindObjectOfType<WaveManager>();
        
        // Initially hide panel
        gameObject.SetActive(false);
    }
    
    void OnEnable()
    {
        // Update UI display
        UpdateUI();
        
        // Set button click events
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueGame);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
        
        // Delay enabling input
        inputEnabled = false;
        Invoke("EnableInput", 1.0f);
        
        // Play victory sound
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            AudioClip victorySound = Resources.Load<AudioClip>("Sounds/Victory");
            if (victorySound != null)
            {
                audioSource.PlayOneShot(victorySound);
            }
        }
        
        // Slow motion effect
        Time.timeScale = 0.5f;
    }
    
    void OnDisable()
    {
        // Remove button click events
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ContinueGame);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        }
        
        // Restore normal time scale
        Time.timeScale = 1.0f;
    }
    
    /// <summary>
    /// Update UI display
    /// </summary>
    private void UpdateUI()
    {
        // Update score display
        if (scoreText != null && gameManager != null)
        {
            int currentScore = gameManager.GetScore();
            scoreText.text = $"Final Score: {currentScore}";
        }
        
        // Update wave display
        if (waveText != null)
        {
            int currentWave = 0;
            int totalWaves = 0;
            
            // Prioritize getting waves from WaveManager
            if (waveManager != null)
            {
                currentWave = waveManager.GetCurrentWave();
                totalWaves = waveManager.GetTotalWaves();
            }
            // If no WaveManager, try to get from GameManager
            else if (gameManager != null)
            {
                currentWave = gameManager.GetCurrentWave();
            }
            
            if (totalWaves > 0)
            {
                waveText.text = $"Completed All Waves: {currentWave}/{totalWaves}";
            }
            else
            {
                waveText.text = $"Completed All Waves: {currentWave}";
            }
        }
        
        // Save high score
        SaveHighScore();
    }
    
    /// <summary>
    /// Save high score
    /// </summary>
    private void SaveHighScore()
    {
        if (gameManager != null)
        {
            int currentScore = gameManager.GetScore();
            int highScore = PlayerPrefs.GetInt(highScoreKey, 0);
            
            if (currentScore > highScore)
            {
                PlayerPrefs.SetInt(highScoreKey, currentScore);
                PlayerPrefs.Save();
                
                Debug.Log($"[WinPanel] New high score: {currentScore}");
            }
        }
    }
    
    /// <summary>
    /// Enable input
    /// </summary>
    private void EnableInput()
    {
        inputEnabled = true;
    }
    
    /// <summary>
    /// Continue game (enter next level or special content)
    /// </summary>
    public void ContinueGame()
    {
        if (!inputEnabled) return;
        
        Debug.Log("[WinPanel] Continuing game");
        
        // Restore normal time scale
        Time.timeScale = 1f;
        
        // Hide victory panel
        gameObject.SetActive(false);
        
        // Logic for entering the next level can be added here
        // For example: SceneManager.LoadScene("NextLevel");
        
        // Temporary: Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    /// <summary>
    /// Return to main menu
    /// </summary>
    public void ReturnToMainMenu()
    {
        if (!inputEnabled) return;
        
        // Restore normal time scale
        Time.timeScale = 1f;
        
        // Load main menu scene
        try
        {
            SceneManager.LoadScene("MainMenu");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WinPanel] Failed to load MainMenu scene: {e.Message}");
            
            // Try to load scene at index 0 (usually the main menu)
            try
            {
                SceneManager.LoadScene(0);
            }
            catch
            {
                Debug.LogError("[WinPanel] Failed to load scene at index 0");
            }
        }
    }
    
    /// <summary>
    /// Set statistics for the win panel
    /// </summary>
    public void SetStats(int score, int wave)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Final Score: {score}";
        }
        
        if (waveText != null)
        {
            int totalWaves = waveManager != null ? waveManager.GetTotalWaves() : 0;
            
            if (totalWaves > 0)
            {
                waveText.text = $"Completed All Waves: {wave}/{totalWaves}";
            }
            else
            {
                waveText.text = $"Completed All Waves: {wave}";
            }
        }
        
        // Save high score based on provided score
        if (score > PlayerPrefs.GetInt(highScoreKey, 0))
        {
            PlayerPrefs.SetInt(highScoreKey, score);
            PlayerPrefs.Save();
            
            Debug.Log($"[WinPanel] New high score saved: {score}");
        }
    }
} 