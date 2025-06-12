using UnityEngine;
using UnityEngine.UI;
using TMPro; // 添加TextMeshPro命名空间
using System.Collections;

/// <summary>
/// 生命值数字显示组件，增强生命值数字显示效果
/// </summary>
public class LivesDisplay : MonoBehaviour
{
    [Header("显示设置")]
    public Text livesText; // 传统UGUI Text组件
    public TextMeshProUGUI livesTMP; // TextMeshPro UGUI组件
    public bool showAsPercentage = false; // 是否显示为百分比
    public string prefixText = "生命: "; // 前缀文本
    
    [Header("动画设置")]
    public bool animateChanges = true; // 是否动画显示变化
    public float animationDuration = 0.5f; // 动画持续时间
    public Color damageColor = new Color(1f, 0.3f, 0.3f); // 受伤颜色
    public Color healColor = new Color(0.3f, 1f, 0.3f); // 治疗颜色
    
    [Header("闪烁警告")]
    public bool enableLowHealthWarning = true; // 是否启用低生命值警告
    public int lowHealthThreshold = 3; // 低生命值阈值
    public float warningBlinkRate = 1.5f; // 警告闪烁速率
    
    [Header("调试设置")]
    public bool initializeWithDefaultValues = true; // 是否使用默认值初始化
    public int defaultLives = 10; // 默认生命值
    public int defaultMaxLives = 10; // 默认最大生命值
    
    private PlayerHealth playerHealth; // 玩家生命值组件
    private int lastLives; // 上一次的生命值
    private Coroutine blinkCoroutine; // 闪烁协程
    private Color originalTextColor; // 原始文本颜色
    private Color originalTMPColor; // 原始TMP文本颜色
    private bool usingTMP = false; // 是否使用TMP
    
    void Awake()
    {
        Debug.Log("[LivesDisplay] Awake - 初始化组件");
        
        // 检查使用哪种文本组件
        usingTMP = livesTMP != null;
        
        if (usingTMP)
            Debug.Log("[LivesDisplay] 使用TMP文本组件");
        else
            Debug.Log("[LivesDisplay] 使用标准Text组件");
        
        // 保存原始文本颜色
        if (livesText != null)
        {
            originalTextColor = livesText.color;
        }
        if (livesTMP != null)
        {
            originalTMPColor = livesTMP.color;
        }
        
        // 如果启用了默认值初始化，立即显示默认值
        if (initializeWithDefaultValues)
        {
            Debug.Log($"[LivesDisplay] 使用默认值初始化: {defaultLives}/{defaultMaxLives}");
            UpdateDisplay(defaultLives, defaultMaxLives, false);
        }
    }
    
    void Start()
    {
        Debug.Log("[LivesDisplay] Start - 查找PlayerHealth组件");
        
        // 获取玩家生命值组件
        playerHealth = FindObjectOfType<PlayerHealth>();
        
        // 如果找到了PlayerHealth组件，连接到生命值变化事件
        if (playerHealth != null)
        {
            Debug.Log($"[LivesDisplay] 找到PlayerHealth组件，当前生命值: {playerHealth.GetCurrentLives()}/{playerHealth.GetMaxLives()}");
            playerHealth.OnLivesChanged += OnLivesChanged;
            
            // 初始化上一次生命值
            lastLives = playerHealth.GetCurrentLives();
            
            // 初始更新显示
            UpdateDisplay(playerHealth.GetCurrentLives(), playerHealth.GetMaxLives(), false);
        }
        else
        {
            Debug.LogWarning("[LivesDisplay] 未找到PlayerHealth组件，使用默认值");
            
            // 如果没有找到PlayerHealth组件，尝试从GameManager获取
            if (GameManager.Instance != null)
            {
                int currentLives = GameManager.Instance.GetCurrentLives();
                int maxLives = GameManager.Instance.maxLives;
                Debug.Log($"[LivesDisplay] 从GameManager获取生命值: {currentLives}/{maxLives}");
                UpdateDisplay(currentLives, maxLives, false);
            }
            else if (initializeWithDefaultValues)
            {
                // 使用默认值
                Debug.Log($"[LivesDisplay] 使用默认值: {defaultLives}/{defaultMaxLives}");
                UpdateDisplay(defaultLives, defaultMaxLives, false);
            }
        }
        
        // 确保显示不为空
        if (livesTMP != null && string.IsNullOrEmpty(livesTMP.text))
        {
            Debug.LogWarning("[LivesDisplay] TMP文本为空，使用默认值");
            livesTMP.text = $"{prefixText}{defaultLives}/{defaultMaxLives}";
        }
        
        if (livesText != null && string.IsNullOrEmpty(livesText.text))
        {
            Debug.LogWarning("[LivesDisplay] Text文本为空，使用默认值");
            livesText.text = $"{prefixText}{defaultLives}/{defaultMaxLives}";
        }
    }
    
    void OnEnable()
    {
        // 确保组件启用时显示正确的值
        if (playerHealth != null)
        {
            UpdateDisplay(playerHealth.GetCurrentLives(), playerHealth.GetMaxLives(), false);
        }
        else if (initializeWithDefaultValues)
        {
            UpdateDisplay(defaultLives, defaultMaxLives, false);
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        if (playerHealth != null)
        {
            playerHealth.OnLivesChanged -= OnLivesChanged;
        }
        
        // 停止所有协程
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
    }
    
    /// <summary>
    /// 处理生命值变化事件
    /// </summary>
    private void OnLivesChanged(int currentLives, int maxLives)
    {
        Debug.Log($"[LivesDisplay] 收到生命值变化事件: {currentLives}/{maxLives} (之前: {lastLives})");
        
        // 检查生命值是否变化
        bool isDecrease = currentLives < lastLives;
        
        // 更新显示
        UpdateDisplay(currentLives, maxLives, isDecrease);
        
        // 更新上一次生命值
        lastLives = currentLives;
    }
    
    /// <summary>
    /// 更新显示
    /// </summary>
    private void UpdateDisplay(int currentLives, int maxLives, bool isDecrease)
    {
        Debug.Log($"[LivesDisplay] 更新显示: {currentLives}/{maxLives}");
        
        // 构建显示文本
        string displayText;
        if (showAsPercentage)
        {
            float percentage = (float)currentLives / maxLives * 100f;
            displayText = $"{prefixText}{percentage:F0}%";
        }
        else
        {
            displayText = $"{prefixText}{currentLives}/{maxLives}";
        }
        
        // 设置文本
        if (livesText != null)
        {
            livesText.text = displayText;
            Debug.Log($"[LivesDisplay] 更新Text: {displayText}");
        }
        if (livesTMP != null)
        {
            livesTMP.text = displayText;
            Debug.Log($"[LivesDisplay] 更新TMP: {displayText}");
        }
        
        // 如果启用了动画，并且生命值发生了变化
        if (animateChanges && lastLives != currentLives && lastLives != 0)
        {
            // 停止任何正在进行的动画
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
            
            // 开始新的动画
            StartCoroutine(AnimateTextColor(isDecrease ? damageColor : healColor));
        }
        
        // 检查是否需要启动低生命值警告
        CheckLowHealthWarning(currentLives, maxLives);
    }
    
    /// <summary>
    /// 动画显示文本颜色变化
    /// </summary>
    private IEnumerator AnimateTextColor(Color targetColor)
    {
        float elapsed = 0f;
        
        // 选择正确的文本组件
        if (usingTMP && livesTMP != null)
        {
            Color startColor = livesTMP.color;
            
            // 从当前颜色渐变到目标颜色
            while (elapsed < animationDuration)
            {
                livesTMP.color = Color.Lerp(startColor, targetColor, elapsed / animationDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保到达目标颜色
            livesTMP.color = targetColor;
            
            // 短暂停留后恢复原始颜色
            yield return new WaitForSeconds(0.2f);
            
            // 从目标颜色渐变回原始颜色
            elapsed = 0f;
            while (elapsed < animationDuration)
            {
                livesTMP.color = Color.Lerp(targetColor, originalTMPColor, elapsed / animationDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保恢复原始颜色
            livesTMP.color = originalTMPColor;
        }
        else if (livesText != null)
        {
            Color startColor = livesText.color;
            
            // 从当前颜色渐变到目标颜色
            while (elapsed < animationDuration)
            {
                livesText.color = Color.Lerp(startColor, targetColor, elapsed / animationDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保到达目标颜色
            livesText.color = targetColor;
            
            // 短暂停留后恢复原始颜色
            yield return new WaitForSeconds(0.2f);
            
            // 从目标颜色渐变回原始颜色
            elapsed = 0f;
            while (elapsed < animationDuration)
            {
                livesText.color = Color.Lerp(targetColor, originalTextColor, elapsed / animationDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保恢复原始颜色
            livesText.color = originalTextColor;
        }
    }
    
    /// <summary>
    /// 检查是否需要启动低生命值警告
    /// </summary>
    private void CheckLowHealthWarning(int currentLives, int maxLives)
    {
        // 如果启用了低生命值警告，并且当前生命值低于阈值
        if (enableLowHealthWarning && currentLives <= lowHealthThreshold)
        {
            // 如果没有正在运行的闪烁协程，启动一个
            if (blinkCoroutine == null)
            {
                blinkCoroutine = StartCoroutine(BlinkText());
            }
        }
        else
        {
            // 如果生命值恢复正常，停止闪烁
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
                
                // 确保文本恢复原始颜色
                if (usingTMP && livesTMP != null)
                {
                    livesTMP.color = originalTMPColor;
                }
                else if (livesText != null)
                {
                    livesText.color = originalTextColor;
                }
            }
        }
    }
    
    /// <summary>
    /// 文本闪烁效果
    /// </summary>
    private IEnumerator BlinkText()
    {
        while (true)
        {
            if (usingTMP && livesTMP != null)
            {
                // 在原始颜色和警告颜色之间切换
                livesTMP.color = livesTMP.color == originalTMPColor ? damageColor : originalTMPColor;
            }
            else if (livesText != null)
            {
                // 在原始颜色和警告颜色之间切换
                livesText.color = livesText.color == originalTextColor ? damageColor : originalTextColor;
            }
            
            // 等待一段时间
            yield return new WaitForSeconds(1f / warningBlinkRate);
        }
    }
    
    /// <summary>
    /// 手动更新显示
    /// </summary>
    public void ForceUpdateDisplay()
    {
        if (playerHealth != null)
        {
            UpdateDisplay(playerHealth.GetCurrentLives(), playerHealth.GetMaxLives(), false);
        }
        else if (GameManager.Instance != null)
        {
            UpdateDisplay(GameManager.Instance.GetCurrentLives(), GameManager.Instance.maxLives, false);
        }
        else
        {
            UpdateDisplay(defaultLives, defaultMaxLives, false);
        }
    }
}