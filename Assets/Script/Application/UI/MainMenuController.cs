using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Main menu controller, manages main menu interface functionality
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("UI Elements")]
    public Button startGameButton; // Start game button
    public Button settingsButton; // Settings button
    public Button quitButton; // Quit game button
    public TextMeshProUGUI highScoreText; // High score display
    public GameObject settingsPanel; // Settings panel
    
    [Header("Game Scenes")]
    public string gameSceneName = "GameScene"; // Game scene name
    
    [Header("Sound Effects")]
    public AudioClip buttonClickSound; // Button click sound
    public AudioClip backgroundMusic; // Background music
    
    [Header("Save Settings")]
    public string highScoreKey = "HighScore"; // High score save key name
    
    private AudioSource audioSource;
    
    void Start()
    {
        // Get audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Play background music
        if (backgroundMusic != null && audioSource != null)
        {
            audioSource.clip = backgroundMusic;
            audioSource.loop = true;
            audioSource.Play();
        }
        
        // Set button click events
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(ToggleSettings);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
        
        // Hide settings panel
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        
        // Display high score
        UpdateHighScoreDisplay();
    }
    
    /// <summary>
    /// Update high score display
    /// </summary>
    private void UpdateHighScoreDisplay()
    {
        if (highScoreText != null)
        {
            int highScore = PlayerPrefs.GetInt(highScoreKey, 0);
            highScoreText.text = $"High Score: {highScore}";
        }
    }
    
    /// <summary>
    /// Start game
    /// </summary>
    public void StartGame()
    {
        PlayButtonSound();
        
        try
        {
            SceneManager.LoadScene(gameSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MainMenuController] Cannot load game scene: {e.Message}");
            Debug.LogWarning("[MainMenuController] Please ensure the game scene is added to build settings, or modify the scene name");
            
            // Try to load scene at index 1 (usually the game scene)
            try
            {
                SceneManager.LoadScene(1);
            }
            catch
            {
                Debug.LogError("[MainMenuController] Cannot load scene at index 1, please check build settings");
            }
        }
    }
    
    /// <summary>
    /// Toggle settings panel
    /// </summary>
    public void ToggleSettings()
    {
        PlayButtonSound();
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }
    
    /// <summary>
    /// Quit game
    /// </summary>
    public void QuitGame()
    {
        PlayButtonSound();
        
        Debug.Log("Quitting game");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    /// <summary>
    /// Reset high score
    /// </summary>
    public void ResetHighScore()
    {
        PlayButtonSound();
        
        PlayerPrefs.SetInt(highScoreKey, 0);
        PlayerPrefs.Save();
        
        UpdateHighScoreDisplay();
        
        Debug.Log("[MainMenuController] High score has been reset");
    }
    
    /// <summary>
    /// Play button sound
    /// </summary>
    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
} 