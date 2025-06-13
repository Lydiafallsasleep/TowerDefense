using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 敌人血条控制器，负责可视化敌人血量
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("引用")]
    public Slider healthSlider;          // 血条滑块
    public Image fillImage;              // 填充图像
    public TextMeshProUGUI healthText;   // 血量文本（可选）
    
    [Header("颜色设置")]
    public Color highHealthColor = Color.green;     // 高血量颜色
    public Color mediumHealthColor = Color.yellow;  // 中血量颜色
    public Color lowHealthColor = Color.red;        // 低血量颜色
    public float mediumHealthThreshold = 0.6f;      // 中血量阈值（百分比）
    public float lowHealthThreshold = 0.3f;         // 低血量阈值（百分比）
    
    [Header("动画设置")]
    public bool useAnimations = true;               // 是否使用动画效果
    public float damageFlashDuration = 0.2f;        // 受伤闪烁持续时间
    public Color damageFlashColor = Color.white;    // 受伤闪烁颜色
    public float healthBarShowDuration = 3f;        // 血条显示持续时间
    public float fadeSpeed = 2f;                    // 淡出速度
    
    // 私有变量
    private EnemyHealth enemyHealth;                // 敌人血量组件
    private float lastDamageTime;                   // 上次受伤时间
    private bool isFlashing = false;                // 是否正在闪烁
    private Color originalFillColor;                // 原始填充颜色
    private CanvasGroup canvasGroup;                // 画布组（用于淡入淡出）
    private Camera mainCamera;                      // 主相机
    
    void Awake()
    {
        // 获取组件
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // 保存原始填充颜色
        if (fillImage != null)
        {
            originalFillColor = fillImage.color;
        }
        
        // 获取主相机
        mainCamera = Camera.main;
        
        // 获取敌人血量组件
        enemyHealth = GetComponentInParent<EnemyHealth>();
    }
    
    void Start()
    {
        // 初始化血条
        UpdateHealthBar();
    }
    
    void OnEnable()
    {
        // 重置状态
        lastDamageTime = 0f;
        isFlashing = false;
        
        // 显示血条
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
        
        // 更新血条
        UpdateHealthBar();
    }
    
    void Update()
    {
        // 如果使用动画，处理血条淡出
        if (useAnimations && canvasGroup != null)
        {
            // 如果敌人最近受伤，显示血条
            if (Time.time - lastDamageTime < healthBarShowDuration)
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed * 2f);
            }
            // 否则淡出血条
            else
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
            }
        }
        
        // 始终面向相机
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }
    
    /// <summary>
    /// 更新血条显示
    /// </summary>
    public void UpdateHealthBar()
    {
        if (enemyHealth == null || healthSlider == null)
            return;
        
        // 更新血条值
        float currentHealth = enemyHealth.GetCurrentHealth();
        float maxHealth = enemyHealth.GetMaxHealth();
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
        
        // 更新血量文本（如果有）
        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(currentHealth)}/{maxHealth}";
        }
        
        // 根据血量百分比更新颜色
        if (fillImage != null)
        {
            float healthPercent = currentHealth / maxHealth;
            
            if (healthPercent <= lowHealthThreshold)
            {
                fillImage.color = lowHealthColor;
            }
            else if (healthPercent <= mediumHealthThreshold)
            {
                fillImage.color = mediumHealthColor;
            }
            else
            {
                fillImage.color = highHealthColor;
            }
        }
    }
    
    /// <summary>
    /// 触发受伤效果
    /// </summary>
    public void TriggerDamageEffect()
    {
        // 记录受伤时间
        lastDamageTime = Time.time;
        
        // 如果使用动画且没有正在闪烁，开始闪烁效果
        if (useAnimations && !isFlashing && fillImage != null)
        {
            StartCoroutine(FlashEffect());
        }
    }
    
    /// <summary>
    /// 闪烁效果协程
    /// </summary>
    private System.Collections.IEnumerator FlashEffect()
    {
        isFlashing = true;
        
        // 保存原始颜色
        Color originalColor = fillImage.color;
        
        // 切换到闪烁颜色
        fillImage.color = damageFlashColor;
        
        // 等待一小段时间
        yield return new WaitForSeconds(damageFlashDuration);
        
        // 恢复原始颜色
        fillImage.color = originalColor;
        
        isFlashing = false;
    }
} 