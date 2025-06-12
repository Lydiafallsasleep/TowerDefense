using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : Singleton<GameManager>
{
    [Header("游戏设置")]
    public int maxLives = 10;
    private int currentLives;
    
    [Header("游戏状态")]
    public bool isGameOver = false;
    public bool isPaused = false;
    
    [Header("游戏进度")]
    private int currentScore = 0;
    private int currentWave = 1;
    
    // UI引用
    [Header("UI引用")]
    public GameObject gameOverPanel;
    public GameObject pausePanel;
    
    // 添加PlayerHealth引用
    private PlayerHealth playerHealth;
    private EnemySpawner enemySpawner;
    
    void Start()
    {
        // 初始化游戏状态
        currentLives = maxLives;
        isGameOver = false;
        Time.timeScale = 1f; // 确保游戏是正常速度
        
        // 隐藏面板
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        
        // 查找PlayerHealth组件
        playerHealth = FindObjectOfType<PlayerHealth>();
        
        // 查找EnemySpawner组件
        enemySpawner = FindObjectOfType<EnemySpawner>();
    }
    
    void Update()
    {
        // 处理暂停逻辑
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }
    
    // 玩家受到伤害（敌人到达终点时调用）
    public void PlayerTakeDamage(int damage)
    {
        if (isGameOver) return;
        
        // 如果找到了PlayerHealth组件，则通过它处理伤害
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            // PlayerHealth组件会自行处理生命值和UI更新
            currentLives = playerHealth.GetCurrentLives();
        }
        else
        {
            // 向后兼容：如果没有PlayerHealth组件，则直接处理
            currentLives -= damage;
            Debug.Log($"玩家受到{damage}点伤害，剩余生命：{currentLives}");
            
            // 检查游戏结束条件
            if (currentLives <= 0)
            {
                GameOver();
            }
        }
    }
    
    // 增加金币（击杀敌人时调用）
    public void AddGold(int amount)
    {
        if (isGameOver) return;
        
        if (TowerManager.Instance != null)
        {
            TowerManager.Instance.AddGold(amount);
        }
    }
    
    // 增加分数
    public void AddScore(int points)
    {
        if (isGameOver) return;
        
        currentScore += points;
        Debug.Log($"增加{points}分，当前分数：{currentScore}");
    }
    
    // 设置当前波数
    public void SetCurrentWave(int wave)
    {
        currentWave = wave;
    }
    
    // 增加波数
    public void IncreaseWave()
    {
        currentWave++;
        Debug.Log($"当前波数：{currentWave}");
    }
    
    // 游戏结束
    public void GameOver()
    {
        // 避免重复触发
        if (isGameOver) return;
        
        isGameOver = true;
        Debug.Log("游戏结束！");
        
        // 通知EnemySpawner停止生成敌人
        if (enemySpawner != null)
        {
            enemySpawner.SetGameOver(true);
        }
        
        // 显示游戏结束面板
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("GameOverPanel未设置！");
        }
        
        // 可以选择减慢游戏速度，而不是完全暂停
        // 完全暂停会在PlayerHealth的ShowGameOverPanel协程中处理
        Time.timeScale = 0.3f;
    }
    
    // 暂停/继续游戏
    public void TogglePause()
    {
        // 如果游戏已结束，不处理暂停
        if (isGameOver) return;
        
        isPaused = !isPaused;
        
        if (isPaused)
        {
            // 暂停游戏
            Time.timeScale = 0f;
            if (pausePanel != null) pausePanel.SetActive(true);
            Debug.Log("游戏已暂停");
        }
        else
        {
            // 继续游戏
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
            Debug.Log("游戏已继续");
        }
    }
    
    // 重新开始游戏
    public void RestartGame()
    {
        // 恢复正常时间缩放
        Time.timeScale = 1f;
        
        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    // 重置游戏状态（不重新加载场景）
    public void ResetGameState()
    {
        Debug.Log("[GameManager] 重置游戏状态");
        
        // 重置游戏状态标志
        isGameOver = false;
        isPaused = false;
        
        // 重置分数和波数
        currentScore = 0;
        currentWave = 1;
        
        // 重置生命值
        currentLives = maxLives;
        
        // 恢复正常时间缩放
        Time.timeScale = 1f;
        
        // 隐藏游戏结束面板
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // 隐藏暂停面板
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        
        // 通知EnemySpawner重置状态
        if (enemySpawner != null)
        {
            enemySpawner.SetGameOver(false);
        }
        
        Debug.Log("[GameManager] 游戏状态已重置");
    }
    
    // 返回主菜单
    public void ReturnToMainMenu()
    {
        // 恢复正常时间缩放
        Time.timeScale = 1f;
        
        // 假设主菜单是场景索引0
        SceneManager.LoadScene(0);
    }
    
    // 退出游戏
    public void QuitGame()
    {
        Debug.Log("退出游戏");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    // 获取当前生命值
    public int GetCurrentLives()
    {
        // 如果存在PlayerHealth组件，则从它获取生命值
        if (playerHealth != null)
        {
            return playerHealth.GetCurrentLives();
        }
        return currentLives;
    }
    
    // 获取当前分数
    public int GetScore()
    {
        return currentScore;
    }
    
    // 获取当前波数
    public int GetCurrentWave()
    {
        return currentWave;
    }
    
    /// <summary>
    /// 游戏胜利处理
    /// </summary>
    public void Victory()
    {
        if (isGameOver) return;
        
        Debug.Log("[GameManager] 游戏胜利！");
        
        // 设置游戏状态
        isGameOver = true;
        
        // 通知EnemySpawner停止生成敌人
        if (enemySpawner != null)
        {
            enemySpawner.SetGameOver(true);
        }
        
        // 通知WaveManager游戏结束
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.SetGameOver(true);
        }
        
        // 显示胜利UI（如果有）
        GameObject victoryPanel = GameObject.Find("VictoryPanel");
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
        else
        {
            // 如果没有专门的胜利面板，可以使用游戏结束面板
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                
                // 尝试修改游戏结束面板上的文本
                TextMeshProUGUI[] texts = gameOverPanel.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var text in texts)
                {
                    if (text.name.Contains("Title") || text.name.Contains("Header"))
                    {
                        text.text = "胜利！";
                        break;
                    }
                }
            }
        }
        
        // 应用慢动作效果（可选）
        Time.timeScale = 0.5f;
        
        // 播放胜利音效（如果有）
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            AudioClip victorySound = Resources.Load<AudioClip>("Sounds/Victory");
            if (victorySound != null)
            {
                audioSource.PlayOneShot(victorySound);
            }
        }
        
        // 给予额外奖励
        int victoryBonus = currentScore / 2; // 额外奖励为当前分数的一半
        currentScore += victoryBonus;
        
        Debug.Log($"[GameManager] 胜利奖励: {victoryBonus}分，总分: {currentScore}");
    }
} 