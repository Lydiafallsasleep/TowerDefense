using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tower Defense UI Manager, responsible for handling tower defense related interfaces
/// </summary>
public class TowerDefenseUI : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject buildPanel;
    public GameObject towerPanel;
    
    [Header("Tower Info Display")]
    public Text towerInfoText;
    public Button upgradeButton;
    public Button sellButton;
    
    [Header("Resource Display")]
    public Text goldText;
    public TextMeshProUGUI goldTMP;
    public Text waveText;
    public TextMeshProUGUI waveTMP;
    public Text livesText;
    public TextMeshProUGUI livesTMP;
    
    [Header("Game Controls")]
    public Button playPauseButton;
    public Button speedUpButton;
    public Text speedText;
    public TextMeshProUGUI speedTMP;
    
    private TowerManager towerManager;
    private GameManager gameManager;
    private PlayerHealth playerHealth;
    private bool isGamePaused = false;
    private int gameSpeed = 1;
    
    void Start()
    {
        // Get tower manager reference
        towerManager = TowerManager.Instance;
        
        // Get game manager reference
        gameManager = GameManager.Instance;
        
        // Get player health component
        playerHealth = FindObjectOfType<PlayerHealth>();
        
        // If PlayerHealth component is found, connect to lives changed event
        if (playerHealth != null)
        {
            playerHealth.OnLivesChanged += OnPlayerLivesChanged;
            
            // If livesText is already set in the Inspector, pass it to PlayerHealth
            if (livesText != null)
            {
                playerHealth.livesText = livesText;
            }
            
            // If livesTMP is already set in the Inspector, pass it to PlayerHealth
            if (livesTMP != null)
            {
                playerHealth.livesTMP = livesTMP;
            }
        }
        
        // Set UI references to tower manager
        if (towerManager != null)
        {
            towerManager.goldText = goldText;
            towerManager.buildPanel = buildPanel;
            towerManager.upgradePanel = towerPanel;
            towerManager.towerInfoText = towerInfoText;
            towerManager.upgradeButton = upgradeButton;
            towerManager.sellButton = sellButton;
        }
        
        // Initially show build panel
        ShowBuildPanel();
        
        // Update resource display
        UpdateResourceDisplay();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (playerHealth != null)
        {
            playerHealth.OnLivesChanged -= OnPlayerLivesChanged;
        }
    }
    
    // Handle player lives changed event
    private void OnPlayerLivesChanged(int currentLives, int maxLives)
    {
        if (livesText != null)
        {
            livesText.text = $"Lives: {currentLives}/{maxLives}";
        }
        
        if (livesTMP != null)
        {
            livesTMP.text = $"Lives: {currentLives}/{maxLives}";
        }
    }
    
    // Update resource display
    public void UpdateResourceDisplay()
    {
        // Update gold display
        if (towerManager != null)
        {
            if (goldText != null)
        {
                goldText.text = $"Gold: {towerManager.currentGold}";
        }
            
            if (goldTMP != null)
            {
                goldTMP.text = $"Gold: {towerManager.currentGold}";
            }
        }
        
        // Can get wave information from GameManager
        if (waveText != null)
        {
            waveText.text = $"Wave: 1/10";
        }
        
        if (waveTMP != null)
        {
            waveTMP.text = $"Wave: 1/10";
        }
        
        // Update lives display
        if (livesText != null || livesTMP != null)
        {
            // If there is a PlayerHealth component, get lives from it
            if (playerHealth != null)
            {
                int currentLives = playerHealth.GetCurrentLives();
                int maxLives = playerHealth.GetMaxLives();
                
                if (livesText != null)
        {
                    livesText.text = $"Lives: {currentLives}/{maxLives}";
                }
                
                if (livesTMP != null)
                {
                    livesTMP.text = $"Lives: {currentLives}/{maxLives}";
                }
            }
            // Otherwise get from GameManager
            else if (gameManager != null)
            {
                int currentLives = gameManager.GetCurrentLives();
                int maxLives = gameManager.maxLives;
                
                if (livesText != null)
                {
                    livesText.text = $"Lives: {currentLives}/{maxLives}";
                }
                
                if (livesTMP != null)
                {
                    livesTMP.text = $"Lives: {currentLives}/{maxLives}";
                }
            }
            else
            {
                if (livesText != null)
                {
                    livesText.text = $"Lives: 10";
                }
                
                if (livesTMP != null)
                {
                    livesTMP.text = $"Lives: 10";
                }
            }
        }
    }
    
    // Show build panel
    public void ShowBuildPanel()
    {
        if (buildPanel != null)
            buildPanel.SetActive(true);
            
        if (towerPanel != null)
            towerPanel.SetActive(false);
    }
    
    // Hide build panel
    public void HideBuildButtonsPanel()
    {
        if (buildPanel != null)
            buildPanel.SetActive(false);
    }
    
    // Show upgrade panel
    public void ShowTowerPanel()
    {
        if (buildPanel != null)
            buildPanel.SetActive(false);
            
        if (towerPanel != null)
            towerPanel.SetActive(true);
    }
    
    // Button callback: Select cannon tower
    public void OnCannonTowerButton()
    {
        if (towerManager != null)
        {
            towerManager.SelectTowerType(TowerType.Cannon);
        }
    }
    
    // Button callback: Select arrow tower
    public void OnArrowTowerButton()
    {
        if (towerManager != null)
        {
            towerManager.SelectTowerType(TowerType.Arrow);
        }
    }
    
    // Button callback: Select laser tower
    public void OnLaserTowerButton()
    {
        if (towerManager != null)
        {
            towerManager.SelectTowerType(TowerType.Laser);
        }
    }
    
    // Button callback: Upgrade the currently selected tower
    public void OnUpgradeButton()
    {
        if (towerManager != null)
        {
            towerManager.UpgradeSelectedTower();
        }
    }
 
    // Button callback: Toggle game pause
    public void OnPlayPauseButton()
    {
        isGamePaused = !isGamePaused;
        
        // Update button text
            if (playPauseButton != null && playPauseButton.GetComponentInChildren<Text>() != null)
            {
            playPauseButton.GetComponentInChildren<Text>().text = isGamePaused ? "Play" : "Pause";
            }
            
        // Update game time scale
        if (gameManager != null)
            {
            gameManager.isPaused = isGamePaused;
            gameManager.TogglePause();
        }
        else
        {
            Time.timeScale = isGamePaused ? 0f : gameSpeed;
        }
            }
            
    // Button callback: Toggle game speed
    public void OnSpeedButton()
    {
        // Cycle through game speeds (1x -> 2x -> 3x -> 1x)
        gameSpeed = (gameSpeed % 3) + 1;
        
        // Update button text
        if (speedText != null)
        {
            speedText.text = $"{gameSpeed}x";
        }
        
        if (speedTMP != null)
        {
            speedTMP.text = $"{gameSpeed}x";
        }
        
        // Update game time scale if not paused
        if (!isGamePaused)
        {
            Time.timeScale = gameSpeed;
        }
    }
    
    // Debug button: Spawn enemy
    public void OnSpawnEnemyButton()
    {
        // Find enemy spawner
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.SpawnEnemy();
        }
    }
    
    // Debug button: Add gold
    public void OnAddGoldButton()
    {
        if (towerManager != null)
        {
            towerManager.AddGold(100);
            UpdateResourceDisplay();
        }
        else if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(100);
            UpdateResourceDisplay();
        }
    }
} 