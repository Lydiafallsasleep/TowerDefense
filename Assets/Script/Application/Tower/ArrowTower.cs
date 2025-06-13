using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Arrow Tower: Fast attack speed, higher damage
/// </summary>
public class ArrowTower : BaseTower
{
    SpriteRenderer spriteRend;

    [Header("Arrow Tower Special Settings")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float baseAttackSpeed = 1.5f; // Base attack speed (attacks per second)
    public float baseDamage = 15f;       // Base damage
    public float baseRange = 7.5f;       // Increased base attack range
    
    [Header("Arrow Tower Specific Properties")]
    public float arrowSpeed = 20f;
    public float arrowLifetime = 2f;
    
    [Header("Multi-shot")]
    public bool multiShot = false;  // Whether it has multi-shot ability
    public int arrowCount = 1;      // Number of arrows fired at once
    public float spreadAngle = 15f; // Spread angle for multi-shot
    
    [Header("Sound Effects")]
    public AudioClip shootSound;    // 射击音效
    
    private AudioSource audioSource;
    private Collider2D towerCollider;
    
    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
        
        // Make sure tower has a collider
        towerCollider = GetComponent<Collider2D>();
        if (towerCollider == null)
        {
            // If no collider, add one
            CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
            // Adjust collider size to match tower size
            circleCollider.radius = 0.5f;
        }
        
        // Initialize audio source
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Initialize tower properties
        towerName = "Arrow Tower";
        cost = 100;
        sellValue = 70;
        upgradePrice = 150;
        
        // Set tower specific offset
        positionOffset = new Vector3(0, 0.3f, 0.1f); // Arrow tower slightly lower and slightly forward
        
        // Set attack system properties
        if (attackSystem != null)
        {
            attackSystem.towerType = TowerAttackSystem.TowerType.Arrow;
            attackSystem.attackDamage = baseDamage;
            attackSystem.attackSpeed = baseAttackSpeed;
            attackSystem.attackRange = baseRange;
            attackSystem.arrowPrefab = arrowPrefab;
            attackSystem.firePoint = firePoint;
        }
        
        // Try to load shoot sound effect
        if (shootSound == null)
        {
            shootSound = Resources.Load<AudioClip>("Sounds/ArrowShoot");
        }
    }
    private void OnMouseEnter()
    {
        spriteRend.color = new Vector4(0.8f, 0.8f, 0.8f, 1f);
    }

    private void OnMouseExit()
    {
        spriteRend.color = new Vector4(1f, 1f, 1f, 1f);
    }
    protected override void Start()
    {
        // Arrow tower has high attack speed but low damage
        damage = 8f;
        fireRate = 1.5f;
        range = 1000f;
        
        // Adjust upgrade characteristics
        damageIncreasePerLevel = 4f;
        rangeIncreasePerLevel = 0.5f;
        fireRateIncreasePerLevel = 0.3f;
        
        base.Start();
    }
    
    // Override PerformAttack method instead of Attack
    protected override void PerformAttack()
    {
        if (target == null)
            return;
            
        // Play shooting sound effect
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        
        // Multi-shot
        if (multiShot && arrowCount > 1)
        {
            float angleStep = spreadAngle / (arrowCount - 1);
            float startAngle = -spreadAngle / 2;
            
            for (int i = 0; i < arrowCount; i++)
            {
                float angle = startAngle + angleStep * i;
                FireArrow(angle);
            }
        }
        else
        {
            // Single arrow shot
            FireArrow(0f);
        }
    }
    
    // Fire arrow
    private void FireArrow(float angleOffset)
    {
        if (target == null || firePoint == null)
            return;
            
        // Calculate direction
        Vector3 dir = target.position - firePoint.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffset;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        
        // Create arrow
        if (arrowPrefab != null)
        {
            GameObject arrow = Instantiate(arrowPrefab, firePoint.position, rotation);
            Projectile projectile = arrow.GetComponent<Projectile>();
            
            if (projectile != null)
            {
                projectile.Initialize(target.gameObject, damage);
            }
            else
            {
                Destroy(arrow, 5f);
            }
        }
    }
    
    /// <summary>
    /// Special processing when upgrading
    /// </summary>
    public override bool Upgrade()
    {
        bool upgraded = base.Upgrade();
        
        if (upgraded)
        {
            // Special processing when upgrading tower
            UpdateVisuals();
            
            // Tower gains an additional 5% attack speed per level
            if (attackSystem != null)
            {
                attackSystem.attackSpeed *= 1.05f;
                multiShot = true;
            }
        }
        
        return upgraded;
    }
    
    /// <summary>
    /// Update tower appearance
    /// </summary>
    protected override void UpdateVisuals()
    {
        // First call base class's UpdateVisuals method
        base.UpdateVisuals();
        
        // Update tower appearance based on level
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && levelSprites != null && level <= levelSprites.Length)
        {
            spriteRenderer.sprite = levelSprites[level - 1];
        }
        
        // Can add upgrade effect
        GameObject upgradeEffect = Resources.Load<GameObject>("Effects/UpgradeEffect");
        if (upgradeEffect != null)
        {
            Instantiate(upgradeEffect, transform.position, Quaternion.identity);
        }
    }
    
    /// <summary>
    /// Reset tower state
    /// </summary>
    public override void ResetState()
    {
        base.ResetState();
        
        // Reset to 1st level state
        level = 1;
        canUpgrade = true;
        
        // Reset attack system properties
        if (attackSystem != null)
        {
            attackSystem.attackDamage = baseDamage;
            attackSystem.attackSpeed = baseAttackSpeed;
            attackSystem.attackRange = baseRange;
        }
        
        // Update appearance
        UpdateVisuals();
        
        Debug.Log($"[{towerName}] State reset");
    }
    
    // Tower specific aiming method
    protected void AimAt(Transform target)
    {
        if (target != null && firePoint != null)
        {
            Vector3 dir = target.position - firePoint.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            firePoint.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        // Real-time rotation of tower towards target
        if (target != null)
        {
            AimAt(target);
        }
        
        // Detect mouse click
        if (Input.GetMouseButtonDown(0))
        {
            CheckMouseClick();
        }
    }
    
    // Detect mouse click
    private void CheckMouseClick()
    {
        // Get mouse click position
        Vector3 mousePosition = Input.mousePosition;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        worldPosition.z = transform.position.z; // Ensure Z axis position is the same
        
        // Check if click is within tower collider
        if (towerCollider != null && towerCollider is BoxCollider2D boxCollider)
        {
            // Create a point for raycast detection
            Vector2 point = new Vector2(worldPosition.x, worldPosition.y);
            
            // Check if point is within collider
            if (boxCollider.OverlapPoint(point))
            {
                OnTowerClicked(worldPosition);
            }
        }
        else
        {
            // If no BoxCollider2D, try using simple distance detection
            float clickDistance = Vector3.Distance(worldPosition, transform.position);
            if (clickDistance < 0.5f) // Appropriate threshold, can be adjusted based on tower's actual size
            {
                OnTowerClicked(worldPosition);
            }
        }
    }
    
    // Handle tower click event
    private void OnTowerClicked(Vector3 clickPosition)
    {
        Debug.Log($"Arrow tower clicked: Position {clickPosition}");
        
        // Call operation panel to show
        if (TowerOperationPanel.Instance != null)
        {
            TowerOperationPanel.Instance.ShowPanelAtTower(this, clickPosition);
        }
        else
        {
            // If operation panel doesn't exist, use TowerManager to select tower
            if (TowerManager.Instance != null)
            {
                // Notify TowerManager to select this tower
                TowerManager.Instance.OnTowerSelected(this);
            }
            else
            {
                Debug.LogWarning("TowerOperationPanel instance and TowerManager instance not found!");
            }
        }
    }
} 