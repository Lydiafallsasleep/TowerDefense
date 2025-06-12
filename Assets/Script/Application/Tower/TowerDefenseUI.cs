using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 塔防UI管理器，负责处理塔防相关界面
/// </summary>
public class TowerDefenseUI : MonoBehaviour
{
    [Header("面板引用")]
    public GameObject buildPanel;
    public GameObject towerPanel;
    
    [Header("塔信息显示")]
    public Text towerInfoText;
    public Button upgradeButton;
    public Button sellButton;
    
    [Header("资源显示")]
    public Text goldText;
    public TextMeshProUGUI goldTMP;
    public Text waveText;
    public TextMeshProUGUI waveTMP;
    public Text livesText;
    public TextMeshProUGUI livesTMP;
    
    [Header("游戏控制")]
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
        // 获取塔管理器引用
        towerManager = TowerManager.Instance;
        
        // 获取游戏管理器引用
        gameManager = GameManager.Instance;
        
        // 获取玩家生命值组件
        playerHealth = FindObjectOfType<PlayerHealth>();
        
        // 如果找到了PlayerHealth组件，连接到生命值变化事件
        if (playerHealth != null)
        {
            playerHealth.OnLivesChanged += OnPlayerLivesChanged;
            
            // 如果livesText在Inspector中已经设置，则传递给PlayerHealth
            if (livesText != null)
            {
                playerHealth.livesText = livesText;
            }
            
            // 如果livesTMP在Inspector中已经设置，则传递给PlayerHealth
            if (livesTMP != null)
            {
                playerHealth.livesTMP = livesTMP;
            }
        }
        
        // 将UI引用设置到塔管理器
        if (towerManager != null)
        {
            towerManager.goldText = goldText;
            towerManager.buildPanel = buildPanel;
            towerManager.upgradePanel = towerPanel;
            towerManager.towerInfoText = towerInfoText;
            towerManager.upgradeButton = upgradeButton;
            towerManager.sellButton = sellButton;
        }
        
        // 初始显示建造面板
        ShowBuildPanel();
        
        // 更新资源显示
        UpdateResourceDisplay();
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        if (playerHealth != null)
        {
            playerHealth.OnLivesChanged -= OnPlayerLivesChanged;
        }
    }
    
    // 处理玩家生命值变化事件
    private void OnPlayerLivesChanged(int currentLives, int maxLives)
    {
        if (livesText != null)
        {
            livesText.text = $"生命: {currentLives}/{maxLives}";
        }
        
        if (livesTMP != null)
        {
            livesTMP.text = $"生命: {currentLives}/{maxLives}";
        }
    }
    
    // 更新资源显示
    public void UpdateResourceDisplay()
    {
        // 更新金币显示
        if (towerManager != null)
        {
            if (goldText != null)
            {
                goldText.text = $"金币: {towerManager.currentGold}";
            }
            
            if (goldTMP != null)
            {
                goldTMP.text = $"金币: {towerManager.currentGold}";
            }
        }
        
        // 可以从GameManager获取波次信息
        if (waveText != null)
        {
            waveText.text = $"波次: 1/10";
        }
        
        if (waveTMP != null)
        {
            waveTMP.text = $"波次: 1/10";
        }
        
        // 更新生命值显示
        if (livesText != null || livesTMP != null)
        {
            // 如果有PlayerHealth组件，从它获取生命值
            if (playerHealth != null)
            {
                int currentLives = playerHealth.GetCurrentLives();
                int maxLives = playerHealth.GetMaxLives();
                
                if (livesText != null)
                {
                    livesText.text = $"生命: {currentLives}/{maxLives}";
                }
                
                if (livesTMP != null)
                {
                    livesTMP.text = $"生命: {currentLives}/{maxLives}";
                }
            }
            // 否则从GameManager获取
            else if (gameManager != null)
            {
                int currentLives = gameManager.GetCurrentLives();
                int maxLives = gameManager.maxLives;
                
                if (livesText != null)
                {
                    livesText.text = $"生命: {currentLives}/{maxLives}";
                }
                
                if (livesTMP != null)
                {
                    livesTMP.text = $"生命: {currentLives}/{maxLives}";
                }
            }
            else
            {
                if (livesText != null)
                {
                    livesText.text = $"生命: 10";
                }
                
                if (livesTMP != null)
                {
                    livesTMP.text = $"生命: 10";
                }
            }
        }
    }
    
    // 显示建造面板
    public void ShowBuildPanel()
    {
        if (buildPanel != null)
            buildPanel.SetActive(true);
            
        if (towerPanel != null)
            towerPanel.SetActive(false);
    }
    
    // 显示升级面板
    public void ShowTowerPanel()
    {
        if (buildPanel != null)
            buildPanel.SetActive(false);
            
        if (towerPanel != null)
            towerPanel.SetActive(true);
    }
    
    // 按钮回调：选择炮塔
    public void OnCannonTowerButton()
    {
        if (towerManager != null)
        {
            towerManager.SelectTowerType(TowerType.Cannon);
        }
    }
    
    // 按钮回调：选择箭塔
    public void OnArrowTowerButton()
    {
        if (towerManager != null)
        {
            towerManager.SelectTowerType(TowerType.Arrow);
        }
    }
    
    // 按钮回调：选择激光塔
    public void OnLaserTowerButton()
    {
        if (towerManager != null)
        {
            towerManager.SelectTowerType(TowerType.Laser);
        }
    }
    
    // 按钮回调：升级当前选中的塔
    public void OnUpgradeButton()
    {
        if (towerManager != null)
        {
            towerManager.UpgradeSelectedTower();
        }
    }
    
    // 按钮回调：出售当前选中的塔
    public void OnSellButton()
    {
        if (towerManager != null)
        {
            towerManager.SellSelectedTower();
        }
    }
    
    // 按钮回调：暂停/继续游戏
    public void OnPlayPauseButton()
    {
        isGamePaused = !isGamePaused;
        
        if (isGamePaused)
        {
            Time.timeScale = 0f;
            string pauseText = "继续";
            
            if (playPauseButton != null && playPauseButton.GetComponentInChildren<Text>() != null)
            {
                playPauseButton.GetComponentInChildren<Text>().text = pauseText;
            }
            
            // 如果有TextMeshPro组件，也更新它
            TextMeshProUGUI tmpText = playPauseButton?.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = pauseText;
            }
        }
        else
        {
            Time.timeScale = gameSpeed;
            string playText = "暂停";
            
            if (playPauseButton != null && playPauseButton.GetComponentInChildren<Text>() != null)
            {
                playPauseButton.GetComponentInChildren<Text>().text = playText;
            }
            
            // 如果有TextMeshPro组件，也更新它
            TextMeshProUGUI tmpText = playPauseButton?.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = playText;
            }
        }
    }
    
    // 按钮回调：调整游戏速度
    public void OnSpeedButton()
    {
        if (isGamePaused)
            return;
            
        // 在1x、2x和3x之间切换
        gameSpeed = (gameSpeed % 3) + 1;
        
        // 设置游戏时间缩放
        Time.timeScale = gameSpeed;
        
        // 更新UI显示
        string speedStr = $"{gameSpeed}x";
        
        if (speedText != null)
        {
            speedText.text = speedStr;
        }
        
        if (speedTMP != null)
        {
            speedTMP.text = speedStr;
        }
    }
    
    // 测试按钮：生成敌人
    public void OnSpawnEnemyButton()
    {
        // 获取敌人生成器并生成敌人
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.SpawnEnemy();
        }
    }
    
    // 测试按钮：添加金币
    public void OnAddGoldButton()
    {
        if (towerManager != null)
        {
            towerManager.AddGold(100);
            UpdateResourceDisplay();
        }
    }
} 