using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    [Header("游戏设置")]
    public int maxLives = 10;
    private int currentLives;
    
    [Header("游戏状态")]
    public bool isGameOver = false;
    public bool isPaused = false;
    
    // UI引用
    [Header("UI引用")]
    public GameObject gameOverPanel;
    public GameObject pausePanel;
    
    void Start()
    {
        // 初始化游戏状态
        currentLives = maxLives;
        isGameOver = false;
        Time.timeScale = 1f; // 确保游戏是正常速度
        
        // 隐藏面板
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
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
        
        currentLives -= damage;
        
        Debug.Log($"玩家受到{damage}点伤害，剩余生命：{currentLives}");
        
        // 检查游戏结束条件
        if (currentLives <= 0)
        {
            GameOver();
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
    
    // 游戏结束
    void GameOver()
    {
        isGameOver = true;
        Debug.Log("游戏结束！");
        
        // 显示游戏结束面板
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // 可以选择暂停游戏
        Time.timeScale = 0f;
    }
    
    // 暂停/继续游戏
    public void TogglePause()
    {
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
        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    // 返回主菜单
    public void ReturnToMainMenu()
    {
        // 假设主菜单是场景索引0
        SceneManager.LoadScene(0);
    }
    
    // 退出游戏
    public void QuitGame()
    {
        Debug.Log("退出游戏");
        Application.Quit();
    }
    
    // 获取当前生命值
    public int GetCurrentLives()
    {
        return currentLives;
    }
} 