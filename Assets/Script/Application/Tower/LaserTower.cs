using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Laser Tower class: Continuous attack, low damage, with slowing effect
/// </summary>
public class LaserTower : BaseTower
{
    [Header("Laser Tower Special Settings")]
    public LineRenderer laserBeam;
    public Transform firePoint;
    public float baseAttackSpeed = 1.0f;  // Base attack speed (damage instances per second)
    public float baseDamage = 5f;         // Base damage (per second)
    public float baseRange = 6.5f;        // Increased base attack range
    
    [Header("Slow Effect")]
    public float slowFactor = 0.5f;       // Slow factor (percentage of movement speed)
    public float slowDuration = 1.0f;     // Slow duration
    public Color laserColor = Color.cyan; // Laser color
    
    [Header("Visual Effects")]
    public GameObject impactEffect;
    public float laserWidth = 0.1f;
    public AudioClip laserSound;
    
    private AudioSource audioSource;
    private bool isLaserActive = false;
    private GameObject currentImpactEffect;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Initialize laser tower properties
        towerName = "Laser Tower";
        cost = 150;
        sellValue = 105;
        upgradePrice = 200;
        
        // Set laser tower-specific position offset
        positionOffset = new Vector3(0, 0.5f, -0.2f); // Medium height, slightly back
        
        // Initialize audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.volume = 0.5f;
        }
        
        // Initialize laser renderer
        if (laserBeam == null)
        {
            laserBeam = GetComponentInChildren<LineRenderer>();
            if (laserBeam == null)
            {
                GameObject laserObj = new GameObject("LaserBeam");
                laserObj.transform.SetParent(transform);
                laserObj.transform.localPosition = Vector3.zero;
                laserBeam = laserObj.AddComponent<LineRenderer>();
            }
        }
        
        // Configure laser renderer
        laserBeam.startWidth = laserWidth;
        laserBeam.endWidth = laserWidth * 0.8f;
        laserBeam.material = new Material(Shader.Find("Sprites/Default"));
        laserBeam.startColor = laserColor;
        laserBeam.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.5f);
        laserBeam.positionCount = 2;
        laserBeam.enabled = false;
        
        // Set attack system properties
        if (attackSystem != null)
        {
            attackSystem.towerType = TowerAttackSystem.TowerType.Laser;
            attackSystem.attackDamage = baseDamage;
            attackSystem.attackSpeed = baseAttackSpeed;
            attackSystem.attackRange = baseRange;
            attackSystem.laserBeam = laserBeam;
            attackSystem.slowFactor = slowFactor;
            attackSystem.slowDuration = slowDuration;
        }
    }
    
    protected override void Start()
    {
        // Laser tower has low damage but continuous attack and slowing
        damage = 5f;
        fireRate = 0f; // Laser tower uses continuous damage, not fireRate
        range = 1500f;
        
        // Adjust upgrade characteristics
        damageIncreasePerLevel = 3f;
        rangeIncreasePerLevel = 0.4f;
        fireRateIncreasePerLevel = 0f; // Not using fireRate
        
        base.Start();
    }
    
    protected override void Update()
    {
        // Laser tower has its own update logic, doesn't use base Update
        if (!isActive)
            return;
            
        UpdateTarget(); // Update target
        
        // If has target, activate laser
        if (target != null)
        {
            if (!isLaserActive)
            {
                ActivateLaser();
            }
            
            // Update laser position
            UpdateLaser();
            
            // Apply continuous damage
            ApplyDamage();
        }
        else if (isLaserActive)
        {
            DeactivateLaser();
        }
    }
    
    // Override PerformAttack method instead of Attack
    protected override void PerformAttack()
    {
        // Laser tower handles attack logic in Update, no additional implementation needed here
    }
    
    private void ActivateLaser()
    {
        isLaserActive = true;
        laserBeam.enabled = true;
        
        // Play laser sound
        if (audioSource != null && laserSound != null)
            {
                audioSource.clip = laserSound;
                audioSource.Play();
            }
            
        // Create laser impact effect
        if (impactEffect != null && target != null)
        {
            currentImpactEffect = Instantiate(impactEffect, target.position, Quaternion.identity);
            currentImpactEffect.transform.parent = target;
        }
    }
    
    private void UpdateLaser()
    {
        if (!isLaserActive || target == null)
            return;
            
        // Update laser start and end points
        Vector3 startPosition = transform.position;
        Vector3 endPosition = target.position;
        
        laserBeam.SetPosition(0, startPosition);
        laserBeam.SetPosition(1, endPosition);
        
        // Update impact effect position
        if (currentImpactEffect != null)
        {
            currentImpactEffect.transform.position = endPosition;
        }
    }
    
    private void DeactivateLaser()
    {
        isLaserActive = false;
        laserBeam.enabled = false;
        
        // Stop laser sound
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        
        // Destroy impact effect
        if (currentImpactEffect != null)
        {
            Destroy(currentImpactEffect);
            currentImpactEffect = null;
        }
    }
    
    private void ApplyDamage()
    {
        if (!isLaserActive || target == null)
            return;
            
        // Get target's health component
        GameObject targetObj = target.gameObject;
        EnemyHealth enemyHealth = targetObj.GetComponent<EnemyHealth>();
        
        if (enemyHealth != null)
        {
            // Apply damage per frame, scaled by time
            enemyHealth.TakeDamage(damage * Time.deltaTime);
            
            // Apply slow effect
            EnemyMovement movement = targetObj.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.ApplySlow(slowFactor, slowDuration);
            }
        }
    }
    
    /// <summary>
    /// Special handling during upgrade
    /// </summary>
    public override bool Upgrade()
    {
        bool upgraded = base.Upgrade();
        
        if (upgraded)
        {
            // Special handling for laser tower upgrade
            UpdateVisuals();
            
            // Laser tower gets additional 5% slow effect and duration per level
            if (attackSystem != null)
            {
                slowFactor *= 0.95f; // Reduce to 95% (stronger slow)
                slowDuration *= 1.05f; // Increase duration by 5%
                
                attackSystem.slowFactor = slowFactor;
                attackSystem.slowDuration = slowDuration;
                
                // Increase laser width
                if (laserBeam != null)
                {
                    laserBeam.startWidth *= 1.1f;
                    laserBeam.endWidth *= 1.1f;
                }
            }
        }
        
        return upgraded;
    }
    
    /// <summary>
    /// Update laser tower visuals
    /// </summary>
    protected override void UpdateVisuals()
    {
        // First call base class UpdateVisuals method
        base.UpdateVisuals();
        
        // Update laser tower appearance based on level
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && levelSprites != null && level <= levelSprites.Length)
        {
            spriteRenderer.sprite = levelSprites[level - 1];
        }
        
        // Update laser color based on level
        if (laserBeam != null)
        {
            Color newColor;
            switch (level)
            {
                case 2:
                    newColor = new Color(0.5f, 1f, 1f); // Light blue
                    break;
                case 3:
                    newColor = new Color(0f, 1f, 1f);   // Cyan
                    break;
                default:
                    newColor = laserColor;
                    break;
            }
            
            laserBeam.startColor = newColor;
            laserBeam.endColor = new Color(newColor.r, newColor.g, newColor.b, 0.5f);
        }
        
        // Add upgrade effect if available
        GameObject upgradeEffect = Resources.Load<GameObject>("Effects/UpgradeEffect");
        if (upgradeEffect != null)
        {
            Instantiate(upgradeEffect, transform.position, Quaternion.identity);
        }
    }
    
    protected override void OnDestroy()
    {
        // Ensure cleanup of all effects
        DeactivateLaser();
    }
    
    /// <summary>
    /// Reset laser tower state
    /// </summary>
    public override void ResetState()
    {
        base.ResetState();
        
        // Reset to level 1 state
        level = 1;
        canUpgrade = true;
        
        // Reset attack system properties
        if (attackSystem != null)
        {
            attackSystem.attackDamage = baseDamage;
            attackSystem.attackSpeed = baseAttackSpeed;
            attackSystem.attackRange = baseRange;
            attackSystem.slowFactor = slowFactor;
            attackSystem.slowDuration = slowDuration;
        }
        
        // Reset laser properties
        if (laserBeam != null)
        {
            laserBeam.startWidth = 0.1f;
            laserBeam.endWidth = 0.05f;
            laserBeam.startColor = laserColor;
            laserBeam.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.5f);
            laserBeam.enabled = false;
        }
        
        // Update visuals
        UpdateVisuals();
        
        Debug.Log($"[{towerName}] State reset");
    }
}