using UnityEngine;

/// <summary>
/// 障碍物基类，所有障碍物都应该继承此类
/// </summary>
public class Obstacle : MonoBehaviour
{
    [Header("基本属性")]
    public string obstacleName;
    public int buildCost = 50;
    public int health = 100;
    public int maxHealth = 100;
    
    [Header("状态")]
    protected bool isActive = true;
    protected bool isDestroyed = false;
    
    [Header("组件引用")]
    protected SpriteRenderer spriteRenderer;
    
    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    protected virtual void Start()
    {
        // 初始化
    }
    
    /// <summary>
    /// 障碍物受到伤害
    /// </summary>
    public virtual void TakeDamage(int damage)
    {
        if (!isActive || isDestroyed)
            return;
            
        health -= damage;
        
        // 检查是否被摧毁
        if (health <= 0)
        {
            health = 0;
            OnDestroyed();
        }
    }
    
    /// <summary>
    /// 障碍物被摧毁时调用
    /// </summary>
    protected virtual void OnDestroyed()
    {
        isDestroyed = true;
        isActive = false;
        
        // 可以添加摧毁效果，如粒子效果
        
        // 禁用碰撞器
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // 更改外观
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 0.5f;
            spriteRenderer.color = color;
        }
    }
    
    /// <summary>
    /// 修复障碍物
    /// </summary>
    public virtual void Repair(int amount)
    {
        if (isDestroyed)
            return;
            
        health = Mathf.Min(health + amount, maxHealth);
        
        if (health > 0 && !isActive)
        {
            isActive = true;
            
            // 启用碰撞器
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = true;
            }
            
            // 恢复外观
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 1f;
                spriteRenderer.color = color;
            }
        }
    }
    
    /// <summary>
    /// 获取当前生命值
    /// </summary>
    public int GetHealth()
    {
        return health;
    }
    
    /// <summary>
    /// 获取最大生命值
    /// </summary>
    public int GetMaxHealth()
    {
        return maxHealth;
    }
    
    /// <summary>
    /// 获取生命值百分比
    /// </summary>
    public float GetHealthPercentage()
    {
        return (float)health / maxHealth;
    }
    
    /// <summary>
    /// 重置障碍物状态
    /// </summary>
    public virtual void ResetState()
    {
        Debug.Log($"[{obstacleName}] 重置障碍物状态");
        
        // 重置生命值
        health = maxHealth;
        
        // 重置状态
        isActive = true;
        isDestroyed = false;
        
        // 启用碰撞器
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }
        
        // 恢复外观
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }
    }
} 