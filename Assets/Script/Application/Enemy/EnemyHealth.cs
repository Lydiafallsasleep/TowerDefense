using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 敌人生命值系统，管理敌人的生命值和伤害处理
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("生命值设置")]
    public float baseHealth = 100f;
    public float currentHealth;
    
    [Header("UI设置")]
    public GameObject healthBarPrefab;
    public Vector3 healthBarOffset = new Vector3(0, 0.7f, 0);
    public float healthBarScale = 1f;
    
    [Header("视觉效果")]
    public GameObject hitEffectPrefab;
    public GameObject deathEffectPrefab;
    public float hitEffectDuration = 0.2f;
    
    [Header("精英敌人设置")]
    public Color eliteColor = new Color(1f, 0.5f, 0f, 1f); // 橙色
    public float eliteScale = 1.2f; // 精英敌人体型增大
    
    // 引用
    private SpriteRenderer spriteRenderer;
    private GameObject healthBarInstance;
    private Slider healthSlider;
    private EnemyHealthBar healthBar;  // 新增的健康条引用
    private WaveManager waveManager;
    private GameManager gameManager;
    
    // 状态
    private float healthMultiplier = 1f;
    private bool isElite = false;
    private bool isDead = false;
    private Color originalColor;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        waveManager = FindObjectOfType<WaveManager>();
        gameManager = GameManager.Instance;
    }
    
    void Start()
    {
        // 初始化生命值
        InitializeHealth();
        
        // 创建生命值条
        CreateHealthBar();
    }
    
    void OnEnable()
    {
        // 重置状态
        isDead = false;
        
        // 初始化生命值
        InitializeHealth();
        
        // 更新生命值条
        UpdateHealthBar();
    }
    
    /// <summary>
    /// 初始化敌人生命值
    /// </summary>
    private void InitializeHealth()
    {
        // 应用血量倍率
        currentHealth = baseHealth * healthMultiplier;
        
        // 如果是精英敌人，应用精英设置
        if (isElite)
        {
            ApplyEliteSettings();
        }
    }
    
    /// <summary>
    /// 创建生命值条
    /// </summary>
    private void CreateHealthBar()
    {
        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform.position + healthBarOffset, Quaternion.identity);
            healthBarInstance.transform.localScale = new Vector3(healthBarScale, healthBarScale, 1f);
            healthBarInstance.transform.SetParent(transform);
            
            // 获取健康条组件
            healthBar = healthBarInstance.GetComponent<EnemyHealthBar>();
            if (healthBar == null)
            {
                healthBar = healthBarInstance.AddComponent<EnemyHealthBar>();
            }
            
            // 获取滑块组件（向后兼容）
            healthSlider = healthBarInstance.GetComponentInChildren<Slider>();
            if (healthSlider != null && healthBar.healthSlider == null)
    {
                healthBar.healthSlider = healthSlider;
            }
            
            // 获取填充图像
            if (healthSlider != null && healthBar.fillImage == null)
            {
                healthBar.fillImage = healthSlider.fillRect.GetComponent<Image>();
            }
            
            // 初始化血条
            healthBar.UpdateHealthBar();
        }
    }
    
    /// <summary>
    /// 更新生命值条显示
    /// </summary>
    private void UpdateHealthBar()
    {
        // 优先使用新的健康条组件
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar();
            return;
        }
        
        // 向后兼容：使用旧的滑块
        if (healthSlider != null)
        {
            healthSlider.maxValue = baseHealth * healthMultiplier;
            healthSlider.value = currentHealth;
        }
    }
    
    /// <summary>
    /// 应用精英敌人设置
    /// </summary>
    private void ApplyEliteSettings()
    {
        // 应用精英颜色
        if (spriteRenderer != null)
        {
            spriteRenderer.color = eliteColor;
        }
        
        // 增大体型
        transform.localScale = new Vector3(eliteScale, eliteScale, 1f);
        
        // 增加生命值显示大小
        if (healthBarInstance != null)
        {
            healthBarInstance.transform.localScale = new Vector3(healthBarScale * 1.2f, healthBarScale * 1.2f, 1f);
        }
    }
    
    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead)
            return;
        
        currentHealth -= damage;
        
        // 更新生命值条
        UpdateHealthBar();
        
        // 触发血条受伤效果
        if (healthBar != null)
        {
            healthBar.TriggerDamageEffect();
        }
        
        // 显示受击效果 - 只在对象激活时启动协程
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ShowHitEffect());
        }
        
        // 检查是否死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 显示受击效果
    /// </summary>
    private IEnumerator ShowHitEffect()
    {
        // 实例化受击特效
        if (hitEffectPrefab != null)
        {
            GameObject hitEffect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(hitEffect, hitEffectDuration);
        }
        
        // 闪烁效果
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = isElite ? eliteColor : originalColor;
        }
        else
        {
            yield return null;
        }
    }
    
    /// <summary>
    /// 敌人死亡
    /// </summary>
    private void Die()
    {
        isDead = true;
        
        // 实例化死亡特效
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // 通知WaveManager敌人被击败
        if (waveManager != null)
        {
            waveManager.EnemyDefeated();
        }
        
        // 增加分数和金币
        if (CoinManager.Instance != null)
        {
            int scoreValue = isElite ? 20 : 10;
            int goldValue = isElite ? 15 : 5;
            CoinManager.Instance.AddCoins(goldValue);
            if (gameManager != null)
                gameManager.AddScore(scoreValue);
        }
        else if (gameManager != null)
        {
            int scoreValue = isElite ? 20 : 10;
            int goldValue = isElite ? 15 : 5;
            gameManager.AddScore(scoreValue);
            gameManager.AddGold(goldValue);
        }
        
        // 回收或销毁敌人对象
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.OnDespawn(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 设置生命值倍率
    /// </summary>
    public void SetHealthMultiplier(float multiplier)
    {
        healthMultiplier = multiplier;
        
        // 如果已经初始化，则更新当前生命值
        if (gameObject.activeSelf)
        {
            float healthPercentage = currentHealth / (baseHealth * healthMultiplier);
            currentHealth = baseHealth * multiplier * healthPercentage;
            UpdateHealthBar();
        }
    }
    
    /// <summary>
    /// 设置是否为精英敌人
    /// </summary>
    public void SetElite(bool elite)
    {
        isElite = elite;
        
        if (isElite && gameObject.activeSelf)
        {
            ApplyEliteSettings();
        }
    }
    
    /// <summary>
    /// 获取当前生命值
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    /// <summary>
    /// 获取最大生命值
    /// </summary>
    public float GetMaxHealth()
    {
        return baseHealth * healthMultiplier;
    }
    
    /// <summary>
    /// 是否为精英敌人
    /// </summary>
    public bool IsElite()
    {
        return isElite;
    }
    
    /// <summary>
    /// 重置敌人状态
    /// </summary>
    public void ResetState()
    {
        // 重置生命值
        currentHealth = baseHealth * healthMultiplier;
        
        // 重置死亡状态
        isDead = false;
        
        // 重置颜色
        if (spriteRenderer != null)
    {
            spriteRenderer.color = isElite ? eliteColor : originalColor;
        }
        
        // 更新生命值条
        UpdateHealthBar();
        
        Debug.Log($"敌人生命值已重置: {currentHealth}/{baseHealth * healthMultiplier}");
    }
    
    /// <summary>
    /// 重置敌人生命值
    /// </summary>
    public void ResetHealth()
    {
        // 重置生命值
        currentHealth = baseHealth * healthMultiplier;
        
        // 重置死亡状态
        isDead = false;
        
        // 更新生命值条
        UpdateHealthBar();
    }

    /// <summary>
    /// 对目标应用伤害
    /// </summary>
    private void ApplyDamage(GameObject target, float damage)
    {
    if (target == null) return;
        
    EnemyHealth health = target.GetComponent<EnemyHealth>();
    if (health != null)
    {
        health.TakeDamage(damage);
    }
}
    
    void OnDisable()
    {
        // 取消所有协程
        StopAllCoroutines();
    }
} 