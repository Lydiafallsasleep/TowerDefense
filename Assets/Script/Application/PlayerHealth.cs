using UnityEngine;
using UnityEngine.UI;
using TMPro; // 添加TextMeshPro命名空间
using System.Collections;

/// <summary>
/// 玩家生命值系统，管理玩家的生命值和相关UI显示
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("生命值设置")]
    public int maxLives = 10;
    private int currentLives;
    
    [Header("UI引用")]
    public Text livesText; // 传统UGUI Text
    public TextMeshProUGUI livesTMP; // TextMeshPro UGUI
    
    [Header("受伤效果")]
    public GameObject damageEffect; // 受伤特效
    public AudioClip damageSound; // 受伤音效
    public float screenShakeDuration = 0.5f; // 屏幕震动持续时间
    public float screenShakeIntensity = 0.2f; // 屏幕震动强度
    
    [Header("游戏结束设置")]
    public GameObject gameOverPanel; // 游戏结束面板
    public float gameOverDelay = 1.5f; // 游戏结束延迟时间
    public AudioClip gameOverSound; // 游戏结束音效
    public bool slowMotionOnGameOver = true; // 游戏结束时是否启用慢动作
    public float slowMotionScale = 0.3f; // 慢动作时间缩放比例
    
    // 事件
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
        Debug.Log("[PlayerHealth] Awake - 初始化组件");
        
        // 设置初始生命值，确保不会为0
        currentLives = maxLives;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (damageSound != null || gameOverSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        gameManager = GameManager.Instance;
        
        // 如果游戏管理器存在，同步最大生命值
        if (gameManager != null)
        {
            Debug.Log($"[PlayerHealth] Awake - 从GameManager获取生命值: {gameManager.maxLives}");
            maxLives = gameManager.maxLives;
            currentLives = maxLives;
        }
        
        // 确保游戏结束面板初始隐藏
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // 立即更新UI，确保在Awake阶段就能显示正确的生命值
        UpdateLivesUI();
    }
    
    void Start()
    {
        Debug.Log($"[PlayerHealth] Start - 当前生命值: {currentLives}/{maxLives}");
        
        // 再次确保生命值不会为0
        if (currentLives <= 0)
        {
            currentLives = maxLives;
            Debug.LogWarning("[PlayerHealth] 检测到生命值为0，已重置为最大值");
        }
        
        // 更新UI显示
        UpdateLivesUI();
        
        // 手动触发一次生命值变化事件，确保所有订阅者都能收到最新的生命值
        if (OnLivesChanged != null)
        {
            Debug.Log("[PlayerHealth] 触发OnLivesChanged事件");
            OnLivesChanged.Invoke(currentLives, maxLives);
        }
    }
    
    void OnEnable()
    {
        // 确保组件启用时UI会更新
        UpdateLivesUI();
    }
    
    /// <summary>
    /// 玩家受到伤害时调用此方法
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(int damage)
    {
        // 如果已经游戏结束或处于无敌状态，不受伤害
        if (isGameOver || isInvulnerable) return;
        
        Debug.Log($"[PlayerHealth] 受到伤害: {damage}, 当前生命值: {currentLives} -> {currentLives - damage}");
        
        // 减少生命值
        currentLives = Mathf.Max(0, currentLives - damage);
        
        // 播放受伤效果
        PlayDamageEffects();
        
        // 更新UI
        UpdateLivesUI();
        
        // 触发事件
        if (OnLivesChanged != null)
        {
            OnLivesChanged.Invoke(currentLives, maxLives);
        }
        
        // 检查是否游戏结束
        if (currentLives <= 0)
        {
            TriggerGameOver();
        }
        else
        {
            // 只有在未游戏结束时才应用短暂无敌时间
            StartCoroutine(InvulnerabilityPeriod(1.0f));
        }
    }
    
    /// <summary>
    /// 恢复生命值
    /// </summary>
    /// <param name="amount">恢复数量</param>
    public void HealLives(int amount)
    {
        // 如果已经游戏结束，不能恢复生命值
        if (isGameOver) return;
        
        currentLives = Mathf.Min(maxLives, currentLives + amount);
        Debug.Log($"[PlayerHealth] 恢复生命值: {amount}, 当前生命值: {currentLives}/{maxLives}");
        
        // 更新UI
        UpdateLivesUI();
        
        // 触发事件
        if (OnLivesChanged != null)
        {
            OnLivesChanged.Invoke(currentLives, maxLives);
        }
    }
    
    /// <summary>
    /// 更新生命值UI显示
    /// </summary>
    private void UpdateLivesUI()
    {
        // 确保生命值在有效范围内
        currentLives = Mathf.Clamp(currentLives, 0, maxLives);
        
        // 更新文本显示
        if (livesText != null)
        {
            livesText.text = $"生命: {currentLives}/{maxLives}";
            Debug.Log($"[PlayerHealth] 更新UI Text: {livesText.text}");
        }
        
        // 更新TextMeshPro文本显示
        if (livesTMP != null)
        {
            livesTMP.text = $"生命: {currentLives}/{maxLives}";
            Debug.Log($"[PlayerHealth] 更新UI TMP: {livesTMP.text}");
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] livesTMP 引用为空");
        }
    }
    
    /// <summary>
    /// 播放受伤效果
    /// </summary>
    private void PlayDamageEffects()
    {
        // 播放音效
        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
        
        // 显示受伤特效
        if (damageEffect != null)
        {
            Instantiate(damageEffect, transform.position, Quaternion.identity);
        }
        
        // 震动屏幕
        StartCoroutine(ShakeScreen());
    }
    
    /// <summary>
    /// 屏幕震动效果
    /// </summary>
    private IEnumerator ShakeScreen()
    {
        Vector3 originalPosition = Camera.main.transform.position;
        float elapsed = 0f;
        
        while (elapsed < screenShakeDuration)
        {
            float x = Random.Range(-1f, 1f) * screenShakeIntensity;
            float y = Random.Range(-1f, 1f) * screenShakeIntensity;
            
            Camera.main.transform.position = new Vector3(
                originalPosition.x + x,
                originalPosition.y + y,
                originalPosition.z
            );
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Camera.main.transform.position = originalPosition;
    }
    
    /// <summary>
    /// 短暂无敌时间
    /// </summary>
    private IEnumerator InvulnerabilityPeriod(float duration)
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(duration);
        isInvulnerable = false;
    }
    
    /// <summary>
    /// 获取当前生命值
    /// </summary>
    public int GetCurrentLives()
    {
        return currentLives;
    }
    
    /// <summary>
    /// 获取最大生命值
    /// </summary>
    public int GetMaxLives()
    {
        return maxLives;
    }
    
    /// <summary>
    /// 手动设置生命值
    /// </summary>
    public void SetLives(int lives)
    {
        // 如果已经游戏结束，不能设置生命值
        if (isGameOver) return;
        
        currentLives = Mathf.Clamp(lives, 0, maxLives);
        Debug.Log($"[PlayerHealth] 手动设置生命值: {currentLives}/{maxLives}");
        UpdateLivesUI();
        
        if (OnLivesChanged != null)
        {
            OnLivesChanged.Invoke(currentLives, maxLives);
        }
        
        // 检查是否游戏结束
        if (currentLives <= 0)
        {
            TriggerGameOver();
        }
    }
    
    /// <summary>
    /// 触发游戏结束
    /// </summary>
    private void TriggerGameOver()
    {
        // 避免重复触发游戏结束
        if (isGameOver) return;
        
        isGameOver = true;
        Debug.Log("[PlayerHealth] 游戏结束！");
        
        // 应用慢动作效果
        if (slowMotionOnGameOver)
        {
            Time.timeScale = slowMotionScale;
        }
        
        // 播放游戏结束音效
        if (audioSource != null && gameOverSound != null)
        {
            audioSource.PlayOneShot(gameOverSound);
        }
        
        // 清除场上所有敌人
        ClearAllEnemies();
        
        // 延迟显示游戏结束面板
        StartCoroutine(ShowGameOverPanel());
        
        // 触发游戏结束事件
        if (OnGameOver != null)
        {
            OnGameOver.Invoke();
        }
        
        // 通知GameManager游戏结束
        if (gameManager != null)
        {
            gameManager.GameOver();
        }
    }
    
    /// <summary>
    /// 清除场上所有敌人
    /// </summary>
    private void ClearAllEnemies()
    {
        Debug.Log("[PlayerHealth] 清除场上所有敌人");
        
        // 查找所有敌人
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int enemyCount = enemies.Length;
        
        // 销毁或回收所有敌人
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null && enemy.activeSelf)
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
        
        Debug.Log($"[PlayerHealth] 已清除 {enemyCount} 个敌人");
    }
    
    /// <summary>
    /// 显示游戏结束面板
    /// </summary>
    private IEnumerator ShowGameOverPanel()
    {
        // 等待指定的延迟时间
        yield return new WaitForSecondsRealtime(gameOverDelay);
        
        // 显示游戏结束面板
        if (gameOverPanel != null)
        {
            Debug.Log("[PlayerHealth] 显示游戏结束面板");
            gameOverPanel.SetActive(true);
            
            // 尝试激活所有子对象的动画组件
            Animator[] animators = gameOverPanel.GetComponentsInChildren<Animator>();
            foreach (Animator animator in animators)
            {
                if (animator != null)
                {
                    animator.enabled = true;
                }
            }
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] 游戏结束面板未设置！");
        }
        
        // 完全停止游戏时间（如果需要）
        if (slowMotionOnGameOver)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            Time.timeScale = 0f;
        }
    }
    
    /// <summary>
    /// 重新开始游戏（可以从UI按钮调用）
    /// </summary>
    public void RestartGame()
    {
        // 恢复正常时间缩放
        Time.timeScale = 1f;
        
        // 如果有GameManager，让它处理重新开始
        if (gameManager != null)
        {
            gameManager.RestartGame();
        }
        else
        {
            // 直接重新加载当前场景
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
    
    /// <summary>
    /// 重置玩家生命值（不重新加载场景）
    /// </summary>
    public void ResetHealth()
    {
        Debug.Log("[PlayerHealth] 重置玩家生命值");
        
        // 重置状态
        isGameOver = false;
        isInvulnerable = false;
        
        // 重置生命值
        currentLives = maxLives;
        
        // 更新UI
        UpdateLivesUI();
        
        // 触发生命值变化事件
        if (OnLivesChanged != null)
        {
            OnLivesChanged.Invoke(currentLives, maxLives);
        }
        
        // 隐藏游戏结束面板
        if (gameOverPanel != null && gameOverPanel.activeSelf)
        {
            gameOverPanel.SetActive(false);
        }
        
        Debug.Log($"[PlayerHealth] 玩家生命值已重置为 {currentLives}/{maxLives}");
    }
}