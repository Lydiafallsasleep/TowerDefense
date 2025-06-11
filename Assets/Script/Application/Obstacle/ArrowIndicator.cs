using UnityEngine;

/// <summary>
/// 增强版箭头指示器，提供更丰富的动画效果
/// </summary>
public class ArrowIndicator : MonoBehaviour
{
    [Header("浮动效果")]
    public bool enableBobbing = true;    // 启用上下浮动
    public float bobHeight = 0.2f;       // 上下浮动高度
    public float bobSpeed = 2.0f;        // 浮动速度
    
    [Header("旋转效果")]
    public bool enableRotation = false;  // 启用旋转
    public float rotationSpeed = 30.0f;  // 旋转速度
    public bool randomizeRotation = true;// 随机旋转方向和速度
    
    [Header("脉冲效果")]
    public bool enablePulsing = true;    // 启用缩放脉冲
    public float minScale = 0.8f;        // 最小缩放
    public float maxScale = 1.2f;        // 最大缩放
    public float pulseSpeed = 1.5f;      // 脉冲速度
    
    [Header("颜色效果")]
    public bool enableColorPulse = false;   // 启用颜色脉冲
    public Color startColor = Color.white;  // 起始颜色
    public Color endColor = Color.yellow;   // 结束颜色
    public float colorSpeed = 1.0f;         // 颜色变换速度
    
    private Vector3 startPosition;       // 初始位置
    private Vector3 originalScale;       // 原始缩放
    private SpriteRenderer spriteRenderer; // 精灵渲染器
    private float randomRotationFactor;  // 随机旋转因子
    
    // 脚本初始化
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        randomRotationFactor = Random.Range(0.8f, 1.2f);
    }
    
    // 当箭头被激活时
    void OnEnable()
    {
        // 记录初始位置
        startPosition = transform.position;
        
        // 重置缩放
        transform.localScale = originalScale;
        
        // 设置为逆时针旋转90度的位置（向左指向）
        transform.rotation = Quaternion.Euler(0, 0, -90);
    }
    
    void Update()
    {
        // 上下浮动效果
        if (enableBobbing && bobHeight > 0)
        {
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPosition.x, startPosition.y + yOffset, startPosition.z);
        }
        
        // 确保箭头始终保持在逆时针旋转90度的位置
        transform.rotation = Quaternion.Euler(0, 0, -90);
        
        // 旋转效果被禁用，因为我们需要保持固定角度
        // 如果您希望它能在90度基础上轻微摆动，可以修改下面的代码
        /*
        if (enableRotation)
        {
            float speed = rotationSpeed;
            if (randomizeRotation)
            {
                speed *= randomRotationFactor;
            }
            
            // 在90度基础上小幅度摆动
            float wobble = Mathf.Sin(Time.time * speed) * 10; // 10度摆动幅度
            transform.rotation = Quaternion.Euler(0, 0, 90 + wobble);
        }
        */
        
        // 缩放脉冲效果
        if (enablePulsing)
        {
            float scaleFactor = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * pulseSpeed) + 1) * 0.5f);
            transform.localScale = originalScale * scaleFactor;
        }
        
        // 颜色脉冲效果
        if (enableColorPulse && spriteRenderer != null)
        {
            float colorFactor = (Mathf.Sin(Time.time * colorSpeed) + 1) * 0.5f;
            spriteRenderer.color = Color.Lerp(startColor, endColor, colorFactor);
        }
    }
    
    // 重置箭头状态
    public void ResetArrow()
    {
        transform.position = startPosition;
        transform.localScale = originalScale;
        // 重置为逆时针90度旋转
        transform.rotation = Quaternion.Euler(0, 0, -90);
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = startColor;
        }
    }
}