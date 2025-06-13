using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : Singleton<GameManager>
{
    [Header("Game Settings")]
    public int maxLives = 10;
    private int currentLives;
    
    [Header("Game State")]
    public bool isGameOver = false;
    public bool isPaused = false;
    
    [Header("Game Progress")]
    private int currentScore = 0;
    private int currentWave = 1;
    
    // UI references
    [Header("UI References")]
    public GameObject gameOverPanel;
    public GameObject pausePanel;
    
    // Add PlayerHealth reference
    private PlayerHealth playerHealth;
    private EnemySpawner enemySpawner;
    
    void Start()
    {
        // Initialize game state
        currentLives = maxLives;
        isGameOver = false;
        Time.timeScale = 1f; // Ensure game is at normal speed
        
        // Hide panels
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        
        // Find PlayerHealth component
        playerHealth = FindObjectOfType<PlayerHealth>();
        
        // Find EnemySpawner component
        enemySpawner = FindObjectOfType<EnemySpawner>();
        
        // Initialize particle effect system
        InitializeParticleSystem();
    }
    
    // Initialize particle effect system
    private void InitializeParticleSystem()
    {
        // Check if particle effect system already exists
        TowerParticleEffects particleEffects = FindObjectOfType<TowerParticleEffects>();
        if (particleEffects == null)
        {
            // Create particle effect system
            GameObject particleSystem = new GameObject("TowerParticleEffects");
            particleEffects = particleSystem.AddComponent<TowerParticleEffects>();
            DontDestroyOnLoad(particleSystem);
            Debug.Log("Particle effect system created");
        }
        
        // Check if initializer already exists
        TowerAttackSystemInitializer initializer = FindObjectOfType<TowerAttackSystemInitializer>();
        if (initializer == null)
        {
            // Create initializer
            GameObject initObj = new GameObject("TowerAttackSystemInitializer");
            initializer = initObj.AddComponent<TowerAttackSystemInitializer>();
            initializer.enableParticleEffects = true;
            initializer.particleEffectScale = 1.0f;
            initializer.particleEffectDuration = 1.0f;
            Debug.Log("Particle effect system initializer created");
        }
    }
    
    void Update()
    {
        // Handle pause logic
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }
    
    // Player takes damage (called when enemies reach the end)
    public void PlayerTakeDamage(int damage)
    {
        if (isGameOver) return;
        
        // If PlayerHealth component is found, handle damage through it
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            // PlayerHealth component will handle lives and UI updates itself
            currentLives = playerHealth.GetCurrentLives();
        }
        else
        {
            // Backward compatibility: If no PlayerHealth component, handle directly
        currentLives -= damage;
        Debug.Log($"Player took {damage} damage, remaining lives: {currentLives}");
        
        // Check game over condition
        if (currentLives <= 0)
        {
            GameOver();
            }
        }
    }
    
    // Add gold (called when killing enemies)
    public void AddGold(int amount)
    {
        if (isGameOver) return;
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(amount);
        }
    }
    
    // Add score
    public void AddScore(int points)
    {
        if (isGameOver) return;
        
        currentScore += points;
        Debug.Log($"Added {points} points, current score: {currentScore}");
    }
    
    // Set current wave
    public void SetCurrentWave(int wave)
    {
        currentWave = wave;
    }
    
    // Increase wave
    public void IncreaseWave()
    {
        currentWave++;
        Debug.Log($"Current wave: {currentWave}");
    }
    
    // Game over
    public void GameOver(string reason = "")
    {
        // Avoid triggering multiple times
        if (isGameOver) return;
        
        isGameOver = true;
        Debug.Log($"Game Over! Reason: {(string.IsNullOrEmpty(reason) ? "Not specified" : reason)}");
        
        // Notify EnemySpawner to stop spawning enemies
        if (enemySpawner != null)
        {
            enemySpawner.SetGameOver(true);
        }
        
        // Clear all enemies from the field
        ClearAllEnemies();

        // Show game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            // Set game over reason
        }
    }
    
    /// <summary>
    /// Clear all enemies from the field
    /// </summary>
    private void ClearAllEnemies()
    {
        Debug.Log("[GameManager] Clearing all enemies from the field");
        
        // Find all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int enemyCount = enemies.Length;
        
        // Destroy or recycle all enemies
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null && enemy.activeSelf)
            {
                // If using object pool, return to object pool
                if (ObjectPool.Instance != null)
                {
                    ObjectPool.Instance.OnDespawn(enemy);
                }
                else
                {
                    Destroy(enemy);
                }
            }
        }
        
        Debug.Log($"Cleared {enemyCount} enemies");
    }
    
    /// <summary>
    /// Toggle game pause
    /// </summary>
    public void TogglePause()
    {
        isPaused = !isPaused;
        
        // Update time scale
        Time.timeScale = isPaused ? 0f : 1f;
        
        // Show/hide pause panel
        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }
        
        Debug.Log($"Game {(isPaused ? "paused" : "resumed")}");
        }
    
    /// <summary>
    /// Restart the current level
    /// </summary>
    public void RestartGame()
    {
        // Reset time scale
        Time.timeScale = 1f;
        
        // Reload current scene
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
    
    /// <summary>
    /// Reset game state without reloading the scene
    /// </summary>
    public void ResetGameState()
    {
        // Reset time scale
        Time.timeScale = 1f;
        
        // Reset game variables
        isGameOver = false;
        isPaused = false;
        currentLives = maxLives;
        currentScore = 0;
        currentWave = 1;
        
        // Hide panels
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        
        // Reset player health
        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }
        
        // Reset enemy spawner
        if (enemySpawner != null)
        {
            enemySpawner.ResetState();
        }
        
        // Reset all towers
        TowerManager towerManager = FindObjectOfType<TowerManager>();
        if (towerManager != null)
        {
            towerManager.ResetState();
        }
        
        Debug.Log("Game state reset");
    }
    
    /// <summary>
    /// Return to main menu
    /// </summary>
    public void ReturnToMainMenu()
    {
        // Reset time scale
        Time.timeScale = 1f;
        
        // Load main menu scene
        SceneManager.LoadScene("MainMenu");
    }
    
    /// <summary>
    /// Quit the game
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quitting game");
        
        // Save any data if needed
        if (CoinManager.Instance != null)
        {
            // Save coins or other progress data
        }
        
        // Quit application
        Application.Quit();
    }
    
    /// <summary>
    /// Get current lives
    /// </summary>
    public int GetCurrentLives()
    {
        // If PlayerHealth exists, get lives from it
        if (playerHealth != null)
        {
            return playerHealth.GetCurrentLives();
        }
        
        // Otherwise return internal value
        return currentLives;
    }
    
    /// <summary>
    /// Get current score
    /// </summary>
    public int GetScore()
    {
        return currentScore;
    }
    
    /// <summary>
    /// Get current wave
    /// </summary>
    public int GetCurrentWave()
    {
        return currentWave;
    }
    
    /// <summary>
    /// Victory condition met
    /// </summary>
    public void Victory()
    {
        // Avoid triggering if already game over
        if (isGameOver) return;
        
        Debug.Log("Victory!");
        
        // Set game over but with victory reason
        isGameOver = true;
        
        // Notify EnemySpawner to stop spawning
        if (enemySpawner != null)
        {
            enemySpawner.SetGameOver(true);
        }
        
        // Show victory UI
        GameObject victoryPanel = GameObject.Find("VictoryPanel");
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            
            // Try to get WinPanel component
            WinPanel winPanel = victoryPanel.GetComponent<WinPanel>();
            if (winPanel != null)
            {
                // Update UI with final score and stats
                winPanel.SetStats(currentScore, currentWave);
            }
        }
        else
        {
            // If no victory panel, show game over with victory message
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                
               
            }
        }
        
        // Save progress
        SaveGameProgress();
        
        // Slow down time slightly for dramatic effect
        Time.timeScale = 0.5f;
    }
    
    // Save game progress
    private void SaveGameProgress()
    {
        // Save highest wave reached
        int highestWave = PlayerPrefs.GetInt("HighestWave", 0);
        if (currentWave > highestWave)
            {
            PlayerPrefs.SetInt("HighestWave", currentWave);
        }
        
        // Save highest score
        int highestScore = PlayerPrefs.GetInt("HighestScore", 0);
        if (currentScore > highestScore)
        {
            PlayerPrefs.SetInt("HighestScore", currentScore);
        }
        
        // Save data
        PlayerPrefs.Save();
        
        Debug.Log($"Game progress saved. Highest wave: {Mathf.Max(highestWave, currentWave)}, Highest score: {Mathf.Max(highestScore, currentScore)}");
    }
    
    /// <summary>
    /// Check if victory conditions are met
    /// </summary>
    public void CheckVictoryCondition()
    {
        // Get reference to WaveManager
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        
        // If no WaveManager or already game over, return
        if (waveManager == null || isGameOver)
            return;
        
        // Check if all waves are completed and no enemies are left
        if (waveManager.AreAllWavesCompleted())
        {
            // Count remaining enemies
            GameObject[] remainingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        
            // If no enemies left, trigger victory
            if (remainingEnemies.Length == 0)
        {
                Victory();
            }
        }
    }
    
    // Scene transition methods and other game functions can be added here
    public string mainMenuSceneName = "MainMenu";
    public string creditsSceneName = "Credits";
            
    // Method to load a specific level
    public void LoadLevel(int levelNumber)
            {
        // Reset time scale
        Time.timeScale = 1f;
        
        // Load the level scene
        string levelSceneName = "Level" + levelNumber;
        
        // Check if scene exists in build settings
        if (Application.CanStreamedLevelBeLoaded(levelSceneName))
        {
            SceneManager.LoadScene(levelSceneName);
                }
                else
                {
            Debug.LogError($"Scene {levelSceneName} not found in build settings!");
        }
    }
} 