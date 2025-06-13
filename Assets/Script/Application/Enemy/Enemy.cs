using UnityEngine;

/// <summary>
/// Base enemy class that defines common properties and methods for all enemy types
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Basic Settings")]
    public string enemyName = "Basic Enemy";
    public int enemyLevel = 1;
    
    [Header("Damage Settings")]
    public int damageOnReachingEnd = 1;    // Damage to player when enemy reaches the end
    public int scoreValue = 10;            // Score awarded for killing this enemy
    public int goldValue = 5;              // Gold awarded for killing this enemy
    
    [Header("References")]
    protected EnemyHealth healthSystem;
    protected EnemyMovement movementSystem;
    
    protected virtual void Awake()
    {
        // Get or add required components
        healthSystem = GetComponent<EnemyHealth>();
        if (healthSystem == null)
        {
            healthSystem = gameObject.AddComponent<EnemyHealth>();
        }
        
        movementSystem = GetComponent<EnemyMovement>();
        if (movementSystem == null)
        {
            movementSystem = gameObject.AddComponent<EnemyMovement>();
        }
    }
    
    protected virtual void Start()
    {
        // Additional initialization if needed
    }
    
    /// <summary>
    /// Called when the enemy reaches the end of the path
    /// </summary>
    public virtual void ReachedEnd()
    {
        // Notify GameManager or PlayerHealth about the damage
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.PlayerTakeDamage(damageOnReachingEnd);
        }
        
        // Return to pool or destroy
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
    /// Called when the enemy is killed
    /// </summary>
    /// <param name="awardPoints">Whether to award points and gold for this kill</param>
    public virtual void Die(bool awardPoints = true)
    {
        if (awardPoints)
        {
            // Award score and gold
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(scoreValue);
                GameManager.Instance.AddGold(goldValue);
            }
            
            // Alternative way to add coins if CoinManager exists
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.AddCoins(goldValue);
            }
        }
        
        // Notify wave manager if exists
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.EnemyDefeated();
        }
        
        // Return to pool or destroy
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
    /// Take damage from a tower or other source
    /// </summary>
    public virtual void TakeDamage(float damage)
    {
        if (healthSystem != null)
        {
            healthSystem.TakeDamage(damage);
        }
    }
    
    /// <summary>
    /// Reset the enemy state (used when recycling from object pool)
    /// </summary>
    public virtual void ResetState()
    {
        // Reset health
        if (healthSystem != null)
        {
            healthSystem.ResetState();
        }
        
        // Reset movement
        if (movementSystem != null)
        {
            movementSystem.ResetPath();
        }
    }
} 