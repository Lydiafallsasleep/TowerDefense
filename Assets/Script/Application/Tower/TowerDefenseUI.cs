using UnityEngine;
using UnityEngine.UI;

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
    public Text waveText;
    public Text livesText;
    
    [Header("游戏控制")]
    public Button playPauseButton;
    public Button speedUpButton;
    public Text speedText;
    
    private TowerManager towerManager;
    private bool isGamePaused = false;
    private int gameSpeed = 1;
    
    void Start()
    {
        // 获取塔管理器引用
        towerManager = TowerManager.Instance;
        
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
    
    // 更新资源显示
    public void UpdateResourceDisplay()
    {
        if (towerManager != null && goldText != null)
        {
            goldText.text = $"金币: {towerManager.currentGold}";
        }
        
        // 可以从GameManager获取波次和生命信息
        if (waveText != null)
        {
            waveText.text = $"波次: 1/10";
        }
        
        if (livesText != null)
        {
            livesText.text = $"生命: 20";
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
            if (playPauseButton != null && playPauseButton.GetComponentInChildren<Text>() != null)
            {
                playPauseButton.GetComponentInChildren<Text>().text = "继续";
            }
        }
        else
        {
            Time.timeScale = gameSpeed;
            if (playPauseButton != null && playPauseButton.GetComponentInChildren<Text>() != null)
            {
                playPauseButton.GetComponentInChildren<Text>().text = "暂停";
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
        if (speedText != null)
        {
            speedText.text = $"{gameSpeed}x";
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