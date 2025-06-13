using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tower attack system that handles attack logic for different tower types
/// </summary>
public class TowerAttackSystem : MonoBehaviour
{
    public enum TowerType
    {
        Arrow,  // Arrow Tower: Fast attack speed, high damage
        Laser,  // Laser Tower: Continuous attack, low damage, slowing effect
        Cannon  // Cannon Tower: Highest damage, very slow attack speed, area attack
    }

    [Header("Basic Tower Settings")]
    public TowerType towerType = TowerType.Arrow;
    public float attackRange = 1500f; // Further increased default attack range
    public float attackDamage = 10f;
    public float attackSpeed = 1f;  // Attacks per second
    
    [Header("Arrow Tower Special Settings")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    
    [Header("Laser Tower Special Settings")]
    public LineRenderer laserBeam;
    public float slowFactor = 0.5f;  // Slow effect as percentage of movement speed
    public float slowDuration = 1f;  // Slow duration
    
    [Header("Cannon Tower Special Settings")]
    public GameObject explosionPrefab;
    public float explosionRadius = 1.5f;
    
    [Header("Visual Effects")]
    public GameObject attackEffect;
    public AudioClip attackSound;
    public Transform rotatingPart;  // Rotating part (e.g., cannon head)
    
    // Internal variables
    private float attackCooldown = 0f;
    private bool isAttacking = false;
    private GameObject currentTarget;
    private List<GameObject> enemiesInRange = new List<GameObject>();
    private AudioSource audioSource;
    private LineRenderer laserLineRenderer;
    
    void Start()
    {
        // Initialize components
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && attackSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Initialize laser line renderer
        if (towerType == TowerType.Laser)
        {
            if (laserBeam == null)
            {
                laserBeam = GetComponentInChildren<LineRenderer>();
                if (laserBeam == null && firePoint != null)
                {
                    GameObject laserObj = new GameObject("LaserBeam");
                    laserObj.transform.SetParent(firePoint);
                    laserObj.transform.localPosition = Vector3.zero;
                    laserBeam = laserObj.AddComponent<LineRenderer>();
                    
                    // Set basic laser line properties
                    laserBeam.startWidth = 0.1f;
                    laserBeam.endWidth = 0.1f;
                    laserBeam.positionCount = 2;
                    laserBeam.useWorldSpace = true;
                    laserBeam.material = new Material(Shader.Find("Sprites/Default"));
                    laserBeam.startColor = Color.red;
                    laserBeam.endColor = Color.yellow;
                }
            }
            
            // Disable laser initially
            if (laserBeam != null)
            {
                laserBeam.enabled = false;
            }
        }
        
        // Set attack cooldown
        attackCooldown = 1f / attackSpeed;
    }
    
    void Update()
    {
        // Update attack cooldown
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
        }
        
        // Detect enemies in range
        DetectEnemies();
        
        // Execute different attack logic based on tower type
        switch (towerType)
        {
            case TowerType.Arrow:
                UpdateArrowTower();
                break;
            case TowerType.Laser:
                UpdateLaserTower();
                break;
            case TowerType.Cannon:
                UpdateCannonTower();
                break;
        }
    }
    
    /// <summary>
    /// Detect enemies within attack range
    /// </summary>
    private void DetectEnemies()
    {
        // Clear previous enemy list
        enemiesInRange.Clear();
        
        // First try to find all enemies
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (allEnemies.Length > 0)
        {
            Debug.Log($"[TowerAttackSystem] Total enemies found in scene: {allEnemies.Length}");
        }
        
        // Check all enemies to see which are in range
        foreach (GameObject enemy in allEnemies)
        {
            if (enemy != null && enemy.activeSelf)
            {
                // Only consider distance in XY plane, ignore Z-axis differences
                Vector2 towerPos2D = new Vector2(transform.position.x, transform.position.y);
                Vector2 enemyPos2D = new Vector2(enemy.transform.position.x, enemy.transform.position.y);
                float distance = Vector2.Distance(towerPos2D, enemyPos2D);
                
                if (distance <= attackRange)
                {
                    enemiesInRange.Add(enemy);
                    // Reduce excessive log output, only use for debugging
                    // Debug.Log($"[TowerAttackSystem] Found enemy in range: {enemy.name}, position: {enemy.transform.position}, distance: {distance}");
                }
            }
        }
        
        if (enemiesInRange.Count == 0)
        {
            Debug.Log($"[TowerAttackSystem] No enemies detected in range! Current position: {transform.position}");
        }
    }
    
    /// <summary>
    /// Select attack target
    /// Priority: 1. Earliest enemy in range 2. Enemy with lowest health 3. Closest enemy
    /// </summary>
    private GameObject SelectTarget()
    {
        if (enemiesInRange.Count == 0)
            return null;
            
        // For cannon towers, return first enemy since it's area attack
        if (towerType == TowerType.Cannon)
        {
            return enemiesInRange[0];
        }
        
        // For arrow and laser towers, need specific target selection
        // First sort by entry order (assuming list is already in this order)
        
        // If only one enemy, return directly
        if (enemiesInRange.Count == 1)
            return enemiesInRange[0];
            
        // Get enemy with lowest health
        List<GameObject> lowestHealthEnemies = new List<GameObject>();
        float lowestHealth = float.MaxValue;
        
        foreach (GameObject enemy in enemiesInRange)
        {
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null)
            {
                float currentHealth = health.GetCurrentHealth();
                
                if (currentHealth < lowestHealth)
                {
                    lowestHealth = currentHealth;
                    lowestHealthEnemies.Clear();
                    lowestHealthEnemies.Add(enemy);
                }
                else if (Mathf.Approximately(currentHealth, lowestHealth))
                {
                    lowestHealthEnemies.Add(enemy);
                }
            }
            else
            {
                // If no health component, assume full health
                lowestHealthEnemies.Add(enemy);
            }
        }
        
        // If only one lowest health enemy, return directly
        if (lowestHealthEnemies.Count == 1)
            return lowestHealthEnemies[0];
            
        // If multiple enemies with same health, choose nearest
        GameObject nearestEnemy = null;
        float nearestDistance = float.MaxValue;
        
        foreach (GameObject enemy in lowestHealthEnemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }
        
        return nearestEnemy;
    }
    
    /// <summary>
    /// Update arrow tower logic
    /// </summary>
    private void UpdateArrowTower()
    {
        // Select target
        GameObject target = SelectTarget();
        
        // If no target, stop attacking
        if (target == null)
        {
            isAttacking = false;
            return;
        }
        
        // Rotate tower towards target
        RotateTowardsTarget(target);
        
        // If cooldown complete, fire arrow
        if (attackCooldown <= 0)
        {
            // Fire arrow
            FireArrow(target);
            
            // Reset cooldown
            attackCooldown = 1f / attackSpeed;
        }
    }
    
    /// <summary>
    /// Update laser tower logic
    /// </summary>
    private void UpdateLaserTower()
    {
        // Select target
        GameObject target = SelectTarget();
        
        // If no target, stop laser
        if (target == null)
        {
            StopLaser();
            isAttacking = false;
            return;
        }
        
        // Rotate tower towards target
        RotateTowardsTarget(target);
        
        // Continuously fire laser
        FireLaser(target);
        
        // Apply damage every frame
        if (isAttacking && target != null)
        {
            // Deal damage per second
            ApplyDamage(target, attackDamage * Time.deltaTime);
            
            // Apply slow effect
            EnemyMovement movement = target.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.ApplySlow(slowFactor, slowDuration);
            }
        }
    }
    
    /// <summary>
    /// Update cannon tower logic
    /// </summary>
    private void UpdateCannonTower()
    {
        // Check if enemies in range
        if (enemiesInRange.Count == 0)
        {
            isAttacking = false;
            return;
        }
        
        // Select a target for tower rotation
        GameObject target = enemiesInRange[0];
        
        // Rotate tower towards target
        RotateTowardsTarget(target);
        
        // If cooldown complete, fire cannon
        if (attackCooldown <= 0)
        {
            // Fire cannon
            FireCannon(target);
            
            // Reset cooldown
            attackCooldown = 1f / attackSpeed;
        }
    }
    
    /// <summary>
    /// Rotate tower towards target
    /// </summary>
    private void RotateTowardsTarget(GameObject target)
    {
        if (rotatingPart == null || target == null)
            return;
            
        Vector3 direction = target.transform.position - rotatingPart.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        rotatingPart.rotation = Quaternion.Slerp(rotatingPart.rotation, rotation, Time.deltaTime * 5f);
    }
    
    /// <summary>
    /// Fire arrow
    /// </summary>
    private void FireArrow(GameObject target)
    {
        if (target == null)
            return;
            
        isAttacking = true;
        
        // Play attack sound
        PlayAttackSound();
        
        // Show muzzle flash effect
        if (TowerParticleEffects.Instance != null && firePoint != null)
        {
            TowerParticleEffects.Instance.PlayMuzzleFlashEffect(firePoint.position, firePoint.rotation);
        }
        
        // Show attack effect
        ShowAttackEffect();
        
        // Spawn arrow
        if (arrowPrefab != null && firePoint != null)
        {
            GameObject arrow = Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);
            if (arrow != null)
            {
                Projectile projectile = arrow.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(target, attackDamage);
                }
                else
                {
                    // If no Projectile component, deal damage directly
                    ApplyDamage(target, attackDamage);
                    Destroy(arrow, 1f);
                }
            }
        }
        else
        {
            // If no arrow prefab, deal damage directly
            ApplyDamage(target, attackDamage);
        }
    }
    
    /// <summary>
    /// Fire laser
    /// </summary>
    private void FireLaser(GameObject target)
    {
        if (target == null)
            return;
            
        isAttacking = true;
        
        // Use particle system to create laser beam
        if (TowerParticleEffects.Instance != null)
        {
            // If laser beam exists, destroy first
            if (laserBeam != null && laserBeam.enabled)
            {
                laserBeam.enabled = false;
            }
            
            // Create new laser beam
            GameObject laserEffect = TowerParticleEffects.Instance.CreateLaserBeamEffect(
                firePoint != null ? firePoint.position : transform.position, 
                target.transform.position
            );
            
            // Destroy laser beam at end of frame, new one will be created next frame
            if (laserEffect != null)
            {
                Destroy(laserEffect, Time.deltaTime);
            }
            
            // Show laser impact effect at target position
            TowerParticleEffects.Instance.PlayLaserImpactEffect(target.transform.position);
        }
        else if (laserBeam != null && firePoint != null)
        {
            // Enable laser rendering
            laserBeam.enabled = true;
            
            // Set laser start and end points
            laserBeam.SetPosition(0, firePoint.position);
            laserBeam.SetPosition(1, target.transform.position);
        }
        
        // If first attack, play sound
        if (!isAttacking)
        {
            PlayAttackSound();
            ShowAttackEffect();
        }
    }
    
    /// <summary>
    /// Stop laser
    /// </summary>
    private void StopLaser()
    {
        if (laserBeam != null)
        {
            laserBeam.enabled = false;
        }
    }
    
    /// <summary>
    /// Fire cannon
    /// </summary>
    private void FireCannon(GameObject target)
    {
        if (target == null)
            return;
            
        isAttacking = true;
        
        // Play attack sound
        PlayAttackSound();
        
        // Show muzzle flash effect
        if (TowerParticleEffects.Instance != null && firePoint != null)
        {
            TowerParticleEffects.Instance.PlayMuzzleFlashEffect(firePoint.position, firePoint.rotation);
        }
        
        // Show attack effect
        ShowAttackEffect();
        
        // Create explosion effect
        if (TowerParticleEffects.Instance != null && target != null)
        {
            // Play explosion effect at target position
            TowerParticleEffects.Instance.PlayExplosionEffect(target.transform.position, explosionRadius / 2);
        }
        else if (explosionPrefab != null && target != null)
        {
            // Create explosion at target position
            GameObject explosion = Instantiate(explosionPrefab, target.transform.position, Quaternion.identity);
            Destroy(explosion, 2f);
        }
        
        // Deal damage to all enemies in range
        foreach (GameObject enemy in enemiesInRange)
        {
            if (enemy != null && enemy.activeSelf && target != null)
            {
                float distance = Vector2.Distance(target.transform.position, enemy.transform.position);
                if (distance <= explosionRadius)
                {
                    // Calculate damage falloff based on distance
                    float damageFactor = 1f - (distance / explosionRadius);
                    float damage = attackDamage * Mathf.Max(0.5f, damageFactor);
                    
                    ApplyDamage(enemy, damage);
                }
            }
        }
    }
    
    /// <summary>
    /// Apply damage to target
    /// </summary>
    private void ApplyDamage(GameObject target, float damage)
    {
        if (target == null)
            return;
            
        EnemyHealth health = target.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }
    }
    
    /// <summary>
    /// Play attack sound
    /// </summary>
    private void PlayAttackSound()
    {
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }
    
    /// <summary>
    /// Show attack effect
    /// </summary>
    private void ShowAttackEffect()
    {
        if (attackEffect != null && firePoint != null)
        {
            GameObject effect = Instantiate(attackEffect, firePoint.position, firePoint.rotation);
            Destroy(effect, 1f);
        }
    }
    
    /// <summary>
    /// Draw attack range in scene view
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        if (towerType == TowerType.Cannon)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}