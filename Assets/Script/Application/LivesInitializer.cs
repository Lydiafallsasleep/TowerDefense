using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 生命值初始化辅助组件，确保生命值显示正确初始化
/// </summary>
public class LivesInitializer : MonoBehaviour
{
    [Header("引用设置")]
    public PlayerHealth playerHealth;
    public LivesDisplay livesDisplay;
    public TextMeshProUGUI livesTMP;
    public Text livesText;
    
    [Header("初始值设置")]
    public int initialLives = 10;
    public int maxLives = 10;
    public string prefixText = "生命: ";
    
    [Header("执行设置")]
    public bool initializeOnAwake = true;
    public bool initializeOnStart = true;
    public bool initializeOnEnable = true;
    public float initializationDelay = 0.1f;
    
    private bool initialized = false;
    
    void Awake()
    {
        if (initializeOnAwake)
        {
            Initialize();
        }
    }
    
    void Start()
    {
        if (initializeOnStart)
        {
            Initialize();
        }
        
        // 延迟初始化，确保所有组件都已准备好
        if (initializationDelay > 0)
        {
            Invoke("DelayedInitialize", initializationDelay);
        }
    }
    
    void OnEnable()
    {
        if (initializeOnEnable)
        {
            Initialize();
        }
    }
    
    /// <summary>
    /// 初始化生命值显示
    /// </summary>
    public void Initialize()
    {
        Debug.Log("[LivesInitializer] 开始初始化生命值显示");
        
        // 查找引用（如果尚未设置）
        FindReferences();
        
        // 初始化PlayerHealth
        if (playerHealth != null)
        {
            Debug.Log($"[LivesInitializer] 设置PlayerHealth生命值: {initialLives}/{maxLives}");
            playerHealth.SetLives(initialLives);
        }
        
        // 初始化LivesDisplay
        if (livesDisplay != null)
        {
            Debug.Log("[LivesInitializer] 强制更新LivesDisplay");
            livesDisplay.ForceUpdateDisplay();
        }
        
        // 直接设置文本（作为备用方案）
        string displayText = $"{prefixText}{initialLives}/{maxLives}";
        
        if (livesTMP != null)
        {
            Debug.Log($"[LivesInitializer] 直接设置TMP文本: {displayText}");
            livesTMP.text = displayText;
        }
        
        if (livesText != null)
        {
            Debug.Log($"[LivesInitializer] 直接设置Text文本: {displayText}");
            livesText.text = displayText;
        }
        
        initialized = true;
    }
    
    /// <summary>
    /// 延迟初始化
    /// </summary>
    private void DelayedInitialize()
    {
        Debug.Log("[LivesInitializer] 执行延迟初始化");
        Initialize();
    }
    
    /// <summary>
    /// 查找引用
    /// </summary>
    private void FindReferences()
    {
        // 查找PlayerHealth（如果尚未设置）
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
            Debug.Log(playerHealth != null 
                ? "[LivesInitializer] 找到PlayerHealth组件" 
                : "[LivesInitializer] 未找到PlayerHealth组件");
        }
        
        // 查找LivesDisplay（如果尚未设置）
        if (livesDisplay == null)
        {
            livesDisplay = FindObjectOfType<LivesDisplay>();
            Debug.Log(livesDisplay != null 
                ? "[LivesInitializer] 找到LivesDisplay组件" 
                : "[LivesInitializer] 未找到LivesDisplay组件");
        }
        
        // 查找TextMeshPro（如果尚未设置）
        if (livesTMP == null)
        {
            if (livesDisplay != null && livesDisplay.livesTMP != null)
            {
                livesTMP = livesDisplay.livesTMP;
                Debug.Log("[LivesInitializer] 从LivesDisplay获取TMP引用");
            }
            else
            {
                livesTMP = GetComponentInChildren<TextMeshProUGUI>();
                if (livesTMP == null)
                {
                    // 尝试在当前游戏对象或子对象中查找
                    livesTMP = FindObjectOfType<TextMeshProUGUI>();
                }
                Debug.Log(livesTMP != null 
                    ? "[LivesInitializer] 找到TMP组件" 
                    : "[LivesInitializer] 未找到TMP组件");
            }
        }
        
        // 查找Text（如果尚未设置）
        if (livesText == null)
        {
            if (livesDisplay != null && livesDisplay.livesText != null)
            {
                livesText = livesDisplay.livesText;
                Debug.Log("[LivesInitializer] 从LivesDisplay获取Text引用");
            }
            else
            {
                livesText = GetComponentInChildren<Text>();
                if (livesText == null)
                {
                    // 尝试在当前游戏对象或子对象中查找
                    livesText = FindObjectOfType<Text>();
                }
                Debug.Log(livesText != null 
                    ? "[LivesInitializer] 找到Text组件" 
                    : "[LivesInitializer] 未找到Text组件");
            }
        }
    }
} 