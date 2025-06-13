using UnityEngine;

/// <summary>
/// Base class for obstacles, all obstacles should inherit from this class
/// </summary>
public class Obstacle : MonoBehaviour
{
    [Header("Basic Properties")]
    public string obstacleName;
    public int buildCost = 50;
    public int health = 100;
    public int maxHealth = 100;
    
    [Header("Status")]
    protected bool isActive = true;
    protected bool isDestroyed = false;
    
    [Header("Component References")]
    protected SpriteRenderer spriteRenderer;
    
    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    protected virtual void Start()
    {
        // Initialization
    }
    
    /// <summary>
    /// Obstacle takes damage
    /// </summary>
    public virtual void TakeDamage(int damage)
    {
        if (!isActive || isDestroyed)
            return;
            
        health -= damage;
        
        // Check if destroyed
        if (health <= 0)
        {
            health = 0;
            OnDestroyed();
        }
    }
    
    /// <summary>
    /// Called when obstacle is destroyed
    /// </summary>
    protected virtual void OnDestroyed()
    {
        isDestroyed = true;
        isActive = false;
        
        // Can add destruction effects, like particle effects
        
        // Disable collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // Change appearance
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 0.5f;
            spriteRenderer.color = color;
        }
    }
    
    /// <summary>
    /// Repair obstacle
    /// </summary>
    public virtual void Repair(int amount)
    {
        if (isDestroyed)
            return;
            
        health = Mathf.Min(health + amount, maxHealth);
        
        if (health > 0 && !isActive)
        {
            isActive = true;
            
            // Enable collider
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = true;
            }
            
            // Restore appearance
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 1f;
                spriteRenderer.color = color;
            }
        }
    }
    
    /// <summary>
    /// Get current health
    /// </summary>
    public int GetHealth()
    {
        return health;
    }
    
    /// <summary>
    /// Get maximum health
    /// </summary>
    public int GetMaxHealth()
    {
        return maxHealth;
    }
    
    /// <summary>
    /// Get health percentage
    /// </summary>
    public float GetHealthPercentage()
    {
        return (float)health / maxHealth;
    }
    
    /// <summary>
    /// Reset obstacle state
    /// </summary>
    public virtual void ResetState()
    {
        Debug.Log($"[{obstacleName}] Resetting obstacle state");
        
        // Reset health
        health = maxHealth;
        
        // Reset status
        isActive = true;
        isDestroyed = false;
        
        // Enable collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }
        
        // Restore appearance
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }
    }
} 