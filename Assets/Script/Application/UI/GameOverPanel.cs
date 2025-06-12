using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 游戏结束面板控制器
/// </summary>
public class GameOverPanel : MonoBehaviour
{
    [Header("UI元素")]
    public TextMeshProUGUI scoreText; // 分数显示
    public TextMeshProUGUI highScoreText; // 最高分显示
    public TextMeshProUGUI waveText; // 波数显示
    public Button restartButton; // 重新开始按钮
    public Button retryButton; // 重试按钮（不重新加载场景）
    public Button mainMenuButton; // 主菜单按钮
    public Button quitButton; // 退出按钮
    
    [Header("动画设置")]
    public Animator panelAnimator; // 面板动画控制器
    public float delayBeforeInput = 1.0f; // 允许输入前的延迟时间
    
    [Header("音效")]
    public AudioClip appearSound; // 面板出现音效
    public AudioClip buttonClickSound; // 按钮点击音效
    
    [Header("存档设置")]
    public string highScoreKey = "HighScore"; // 最高分存档键名
    
    private AudioSource audioSource;
    private GameManager gameManager;
    private PlayerHealth playerHealth;
    private WaveManager waveManager;
    private bool inputEnabled = false;
    
    void Awake()
    {
        // 获取组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (appearSound != null || buttonClickSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 查找GameManager
        gameManager = GameManager.Instance;
        
        // 查找PlayerHealth
        playerHealth = FindObjectOfType<PlayerHealth>();
        
        // 查找WaveManager
        waveManager = FindObjectOfType<WaveManager>();
        
        // 初始隐藏面板
        gameObject.SetActive(false);
    }
    
    void OnEnable()
    {
        // 播放出现音效
        if (audioSource != null && appearSound != null)
        {
            audioSource.PlayOneShot(appearSound);
        }
        
        // 更新UI显示
        UpdateUI();
        
        // 设置按钮点击事件
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
        
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(RetryGame);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
        
        // 延迟启用输入
        inputEnabled = false;
        Invoke("EnableInput", delayBeforeInput);
        
        // 播放动画
        if (panelAnimator != null)
        {
            panelAnimator.SetTrigger("Show");
        }
        
        // 保存最高分
        SaveHighScore();
    }
    
    void OnDisable()
    {
        // 移除按钮点击事件
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
        }
        
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(RetryGame);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
        }
    }
    
    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        // 更新分数显示
        if (scoreText != null && gameManager != null)
        {
            int currentScore = gameManager.GetScore();
            scoreText.text = $"当前分数: {currentScore}";
        }
        
        // 更新最高分显示
        if (highScoreText != null)
        {
            int highScore = PlayerPrefs.GetInt(highScoreKey, 0);
            highScoreText.text = $"最高分数: {highScore}";
        }
        
        // 更新波数显示
        if (waveText != null)
        {
            int currentWave = 0;
            int totalWaves = 0;
            
            // 优先从WaveManager获取波数
            if (waveManager != null)
            {
                currentWave = waveManager.GetCurrentWave();
                totalWaves = waveManager.GetTotalWaves();
            }
            // 如果没有WaveManager，尝试从GameManager获取
            else if (gameManager != null)
            {
                currentWave = gameManager.GetCurrentWave();
            }
            
            if (totalWaves > 0)
            {
                waveText.text = $"当前波数: {currentWave}/{totalWaves}";
            }
            else
            {
                waveText.text = $"当前波数: {currentWave}";
            }
        }
    }
    
    /// <summary>
    /// 保存最高分
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
                
                // 更新UI
                if (highScoreText != null)
                {
                    highScoreText.text = $"最高分数: {currentScore} (新纪录!)";
                }
                
                Debug.Log($"[GameOverPanel] 新的最高分: {currentScore}");
            }
        }
    }
    
    /// <summary>
    /// 启用输入
    /// </summary>
    private void EnableInput()
    {
        inputEnabled = true;
        
        // 启用所有按钮
        if (restartButton != null) restartButton.interactable = true;
        if (retryButton != null) retryButton.interactable = true;
        if (mainMenuButton != null) mainMenuButton.interactable = true;
        if (quitButton != null) quitButton.interactable = true;
    }
    
    /// <summary>
    /// 重新开始游戏（重新加载场景）
    /// </summary>
    public void RestartGame()
    {
        if (!inputEnabled) return;
        
        // 播放按钮音效
        PlayButtonSound();
        
        // 恢复正常时间缩放
        Time.timeScale = 1f;
        
        // 如果有PlayerHealth组件，使用它的重启方法
        if (playerHealth != null)
        {
            playerHealth.RestartGame();
        }
        // 如果有GameManager，使用它的重启方法
        else if (gameManager != null)
        {
            gameManager.RestartGame();
        }
        // 直接重新加载当前场景
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
    
    /// <summary>
    /// 重试游戏（不重新加载场景，仅重置状态）
    /// </summary>
    public void RetryGame()
    {
        if (!inputEnabled) return;
        
        // 播放按钮音效
        PlayButtonSound();
        
        // 恢复正常时间缩放
        Time.timeScale = 1f;
        
        // 隐藏游戏结束面板
        gameObject.SetActive(false);
        
        // 开始重置游戏状态
        StartCoroutine(ResetGameState());
    }
    
    /// <summary>
    /// 重置游戏状态的协程
    /// </summary>
    private IEnumerator ResetGameState()
    {
        Debug.Log("[GameOverPanel] 开始重置游戏状态");
        
        // 1. 重置GameManager状态
        if (gameManager != null)
        {
            gameManager.ResetGameState();
        }
        
        // 2. 重置PlayerHealth
        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }
        
        // 3. 销毁所有敌人
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (enemy.activeSelf)
            {
                // 如果使用对象池，则返回对象池
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
        
        // 4. 重置所有塔
        BaseTower[] towers = GameObject.FindObjectsOfType<BaseTower>();
        foreach (BaseTower tower in towers)
        {
            if (tower != null)
            {
                tower.ResetState();
            }
        }
        
        // 5. 重置TowerManager（金币等）
        if (TowerManager.Instance != null)
        {
            TowerManager.Instance.ResetState();
        }
        
        // 6. 重置障碍物
        Obstacle[] obstacles = GameObject.FindObjectsOfType<Obstacle>();
        foreach (Obstacle obstacle in obstacles)
        {
            if (obstacle != null)
            {
                obstacle.ResetState();
            }
        }
        
        // 7. 重置EnemySpawner
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.ResetState();
        }
        
        // 8. 重置WaveManager
        if (waveManager != null)
        {
            waveManager.ResetState();
        }
        
        // 短暂延迟，确保所有组件都有时间重置
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("[GameOverPanel] 游戏状态重置完成");
    }
    
    /// <summary>
    /// 返回主菜单
    /// </summary>
    public void ReturnToMainMenu()
    {
        if (!inputEnabled) return;
        
        // 播放按钮音效
        PlayButtonSound();
        
        // 恢复正常时间缩放
        Time.timeScale = 1f;
        
        // 加载主菜单场景
        // 注意：您需要确保主菜单场景已添加到构建设置中
        // 如果主菜单场景名为"MainMenu"，则使用以下代码
        try
        {
            SceneManager.LoadScene("MainMenu");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameOverPanel] 无法加载主菜单场景: {e.Message}");
            Debug.LogWarning("[GameOverPanel] 请确保主菜单场景已添加到构建设置中，或者修改场景名称");
            
            // 尝试加载索引为0的场景（通常是主菜单）
            try
            {
                SceneManager.LoadScene(0);
            }
            catch
            {
                Debug.LogError("[GameOverPanel] 无法加载索引为0的场景，请检查构建设置");
            }
        }
    }
    
    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        if (!inputEnabled) return;
        
        // 播放按钮音效
        PlayButtonSound();
        
        Debug.Log("退出游戏");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    /// <summary>
    /// 播放按钮音效
    /// </summary>
    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
} 