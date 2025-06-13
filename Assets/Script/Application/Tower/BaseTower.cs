using UnityEngine;
using System.Collections;

public enum TargetPriority 
{
    First,      // First enemy to enter range
    Last,       // Last enemy to enter range
    Strongest,  // Enemy with highest health
    Weakest,    // Enemy with lowest health
    Closest,    // Closest enemy
    Furthest    // Furthest enemy
}

/// <summary>
/// Base tower class, parent class for all towers
/// </summary>
public class BaseTower : MonoBehaviour
{
    [Header("Basic Properties")]
    public string towerName = "Base Tower";
    public int level = 1;
    public int cost = 100;
    public int sellValue = 70;
    public int upgradePrice = 150;
    
    [Header("Position Settings")]
    public Vector3 positionOffset = Vector3.zero; // Offset for tower position
    
    [Header("Status")]
    public bool isPlaced = false;
    public bool canUpgrade = true;
    public int maxLevel = 3;
    
    protected TowerAttackSystem attackSystem;
    
    [Header("Basic Attributes")]
    public float range = 1000f;
    public float damage = 10f;
    public float fireRate = 1f; // Attacks per second
    public TargetPriority targetPriority = TargetPriority.First;
    public GameObject rangeIndicator;

    [Header("Upgrade Bonuses")]
    public float damageIncreasePerLevel = 5f;
    public float rangeIncreasePerLevel = 100f;
    public float fireRateIncreasePerLevel = 0.2f;

    [Header("Component References")]
    protected SpriteRenderer spriteRenderer;
    public Sprite[] levelSprites; // Different appearance for different levels

    [Header("Status")]
    protected Transform target;
    protected float fireCountdown = 0f;
    protected bool isActive = true;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Get attack system
        attackSystem = GetComponent<TowerAttackSystem>();
        if (attackSystem == null)
        {
            attackSystem = gameObject.AddComponent<TowerAttackSystem>();
        }
    }

    protected virtual void Start()
    {
        InvokeRepeating("UpdateTarget", 0f, 0.5f); // Update target every 0.5 seconds
        UpdateVisuals();
    }

    protected virtual void Update()
    {
        if (!isActive || target == null)
            return;

        // Handle attack cooldown
        if (fireCountdown > 0)
        {
            fireCountdown -= Time.deltaTime;
        }
        else
        {
            // Call attack system to perform attack, not abstract method
            PerformAttack();
            fireCountdown = 1f / fireRate;
        }
    }
    
    // Perform attack, using attack system
    protected virtual void PerformAttack()
    {
        // Default implementation, subclasses can override
        if (attackSystem != null && target != null)
        {
            // Use attack system to handle attack logic
            // This implementation is not needed as the attack system will handle it in Update
        }
    }

    protected virtual void UpdateTarget()
    {
        // Get all game objects with "Enemy" tag
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        if (enemies.Length == 0)
            return;

        Transform bestTarget = null;
        float bestTargetValue = 0f;
        bool isValueInitialized = false;

        foreach (GameObject enemy in enemies)
        {
            if (!enemy.activeSelf)
                continue;
                
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            
            // If enemy is out of range, skip
            if (distanceToEnemy > range)
                continue;

            float targetValue = 0f;
            switch (targetPriority)
            {
                case TargetPriority.First:
                    // Enemy with highest path progress
                    EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
                    if (movement != null)
                    {
                        targetValue = movement.GetPathProgress();
                    }
                    break;
                case TargetPriority.Last:
                    // Enemy with lowest path progress
                    movement = enemy.GetComponent<EnemyMovement>();
                    if (movement != null)
                    {
                        targetValue = -movement.GetPathProgress(); // Negative value to prioritize lowest progress
                    }
                    break;
                case TargetPriority.Strongest:
                    // Enemy with highest health
                    var health = enemy.GetComponent<EnemyHealth>(); // Assuming it exists
                    if (health != null)
                    {
                        targetValue = health.GetCurrentHealth();
                    }
                    break;
                case TargetPriority.Weakest:
                    // Enemy with lowest health
                    health = enemy.GetComponent<EnemyHealth>();
                    if (health != null)
                    {
                        targetValue = -health.GetCurrentHealth(); // Negative value to prioritize lowest health
                    }
                    break;
                case TargetPriority.Closest:
                    // Closest enemy
                    targetValue = -distanceToEnemy; // Negative value to prioritize closest distance
                    break;
                case TargetPriority.Furthest:
                    // Furthest enemy (still in range)
                    targetValue = distanceToEnemy;
                    break;
            }

            if (bestTarget == null || !isValueInitialized || targetValue > bestTargetValue)
            {
                bestTargetValue = targetValue;
                isValueInitialized = true;
                bestTarget = enemy.transform;
            }
        }

        target = bestTarget;
    }

    /// <summary>
    /// Upgrade tower
    /// </summary>
    public virtual bool Upgrade()
    {
        if (!canUpgrade || level >= maxLevel)
            return false;

        level++;
        
        // Update attributes
        UpdateStats();
        
        // Update sell value
        sellValue = (int)(cost * 0.7f) + (int)(upgradePrice * (level - 1) * 0.7f);
        
        // Update upgrade price (50% increase per level)
        upgradePrice = (int)(upgradePrice * 1.5f);
        
        // Check if max level reached
        if (level >= maxLevel)
        {
            canUpgrade = false;
        }
        
        Debug.Log($"[{towerName}] Upgraded to {level} level");
        return true;
    }
    
    /// <summary>
    /// Update tower attributes
    /// </summary>
    protected virtual void UpdateStats()
    {
        if (attackSystem != null)
        {
            // 20% attack power and 10% attack speed increase per level
            float damageMultiplier = 1f + (level - 1) * 0.2f;
            float speedMultiplier = 1f + (level - 1) * 0.1f;
            
            attackSystem.attackDamage *= damageMultiplier / (1f + (level - 2) * 0.2f); // Adjusted to relative previous level increase
            attackSystem.attackSpeed *= speedMultiplier / (1f + (level - 2) * 0.1f);   // Adjusted to relative previous level increase
            
            // 5% attack range increase per level
            if (level > 1) // Start increasing range from level 2
            {
                attackSystem.attackRange *= 1.05f;
            }
        }
    }
    
    /// <summary>
    /// Sell tower
    /// </summary>
    public virtual int Sell()
    {
        Debug.Log($"[{towerName}] Sold, returning {sellValue} coins");
        
        // Delay destruction of object to ensure time to handle sell logic
        Destroy(gameObject, 0.1f);
        
        return sellValue;
    }
    
    /// <summary>
    /// Place tower
    /// </summary>
    public virtual void Place()
    {
        isPlaced = true;
        Debug.Log($"[{towerName}] Placed");
    }
    
    /// <summary>
    /// Reset tower status
    /// </summary>
    public virtual void ResetState()
    {
        // Subclasses can override this method to add specific reset logic
    }

    // Update tower visuals, based on level
    protected virtual void UpdateVisuals()
    {
        if (spriteRenderer != null && levelSprites != null && level <= levelSprites.Length)
        {
            spriteRenderer.sprite = levelSprites[level - 1];
        }

        // Update range indicator
        if (rangeIndicator != null)
        {
            rangeIndicator.transform.localScale = new Vector3(range * 2, range * 2, 1f);
        }
    }

    // Draw tower attack range in Unity editor
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }

    // Get upgrade cost
    public virtual int GetUpgradeCost()
    {
        return upgradePrice * level;
    }

    // Get sell value (usually a portion of total construction cost and upgrade cost)
    public virtual int GetSellValue()
    {
        int totalCost = cost;
        for (int i = 1; i < level; i++)
        {
            totalCost += upgradePrice * i;
        }
        return Mathf.FloorToInt(totalCost * 0.7f); // Return 70% of cost
    }
    
    /// <summary>
    /// Called when tower is destroyed
    /// </summary>
    protected virtual void OnDestroy()
    {
        // Base class implementation, subclasses can override
        CancelInvoke(); // Cancel all Invoke calls
    }
} 