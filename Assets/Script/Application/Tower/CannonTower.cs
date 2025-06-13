using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Cannon Tower class: Highest damage, slow attack speed, area-of-effect attack
/// </summary>
public class CannonTower : BaseTower
{
    [Header("Cannon Tower Special Settings")]
    public GameObject cannonballPrefab;
    public GameObject explosionPrefab;
    public Transform firePoint;
    public float baseDamage = 25f;
    public float baseAttackSpeed = 7f;
    public float baseRange = 5000f;
    
    [Header("Explosion Settings")]
    public float explosionRadius = 25f;
    public float projectileSpeed = 15f;
    public float projectileArc = 1f;
    
    [Header("Visuals and SFX")]
    public AudioClip fireSound;
    public AudioClip explosionSound;
    public GameObject muzzleFlash;
    public float recoilDistance = 0.2f;
    public Transform cannonBarrel;
    
    private AudioSource audioSource;
    private Vector3 originalBarrelPosition;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Initialize tower properties
        towerName = "Cannon Tower";
        cost = 200;
        sellValue = 140;
        upgradePrice = 250;
        
        // Set tower-specific position offset
        positionOffset = new Vector3(0, 0.7f, -0.1f); // Slightly higher and back
        
        // Initialize audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Save original barrel position
        if (cannonBarrel != null)
        {
            originalBarrelPosition = cannonBarrel.localPosition;
        }
        
        // Set attack system properties
        if (attackSystem != null)
        {
            attackSystem.towerType = TowerAttackSystem.TowerType.Cannon;
            attackSystem.attackDamage = baseDamage;
            attackSystem.attackSpeed = baseAttackSpeed;
            attackSystem.attackRange = baseRange;
            attackSystem.explosionPrefab = explosionPrefab;
            attackSystem.explosionRadius = explosionRadius;
        }
    }
    
    protected override void Start()
    {
        // Cannon tower has high damage but slow attack speed
        damage = 25f;
        fireRate = 0.5f;
        range = 2000f;
        
        // Adjust upgrade characteristics
        damageIncreasePerLevel = 10f;
        rangeIncreasePerLevel = 0.3f;
        fireRateIncreasePerLevel = 0.1f;
        
        base.Start();
    }
    
    // Override PerformAttack method instead of Attack
    protected override void PerformAttack()
    {
        if (target == null)
            return;
            
        // Play firing sound
        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound);
        }
        
        // Show muzzle flash
        if (muzzleFlash != null && firePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlash, firePoint.position, firePoint.rotation);
            Destroy(flash, 0.1f);
        }
        
        // Apply recoil effect
        if (cannonBarrel != null)
        {
            StartCoroutine(ApplyRecoil());
        }
        
        // Fire cannonball
        FireCannonball();
    }
    
    private void FireCannonball()
    {
        if (target == null || firePoint == null)
            return;
        
        // Create cannonball
        if (cannonballPrefab != null)
        {
            GameObject cannonball = Instantiate(cannonballPrefab, firePoint.position, Quaternion.identity);
            CannonProjectile projectile = cannonball.GetComponent<CannonProjectile>();
            
            if (projectile != null)
            {
                projectile.Initialize(target.gameObject, damage, explosionRadius, explosionPrefab);
                projectile.SetSpeed(projectileSpeed);
                projectile.SetArc(projectileArc);
                
                // Set explosion sound
                if (explosionSound != null)
                {
                    projectile.SetExplosionSound(explosionSound);
                }
            }
            else
            {
                // If no CannonProjectile component, create explosion directly at target
                StartCoroutine(CreateDelayedExplosion(target.position, 0.5f));
                Destroy(cannonball, 0.1f);
            }
        }
        else
        {
            // If no cannonball prefab, create explosion directly at target
            StartCoroutine(CreateDelayedExplosion(target.position, 0.5f));
        }
    }
    
    private IEnumerator CreateDelayedExplosion(Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Create explosion effect
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
            Destroy(explosion, 2f);
            
            // Play explosion sound
            if (audioSource != null && explosionSound != null)
            {
                audioSource.PlayOneShot(explosionSound);
            }
        }
        
        // Damage enemies in range
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, explosionRadius);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                EnemyHealth health = collider.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    // Calculate damage falloff based on distance
                    float distance = Vector2.Distance(position, collider.transform.position);
                    float damageFactor = 1f - (distance / explosionRadius);
                    float actualDamage = damage * Mathf.Max(0.5f, damageFactor);
                    
                    health.TakeDamage(actualDamage);
                }
            }
        }
    }
    
    private IEnumerator ApplyRecoil()
    {
        // Apply recoil effect
        cannonBarrel.localPosition -= new Vector3(recoilDistance, 0, 0);
        
        yield return new WaitForSeconds(0.1f);
        
        // Return to original position
        cannonBarrel.localPosition = originalBarrelPosition;
    }
    
    /// <summary>
    /// Special handling during upgrade
    /// </summary>
    public override bool Upgrade()
    {
        bool upgraded = base.Upgrade();
        
        if (upgraded)
        {
            // Special handling for cannon tower upgrade
            UpdateVisuals();
            
            // Increase explosion radius per level
            explosionRadius += 0.3f;
            
            // Update attack system parameters
            if (attackSystem != null)
            {
                attackSystem.explosionRadius = explosionRadius;
            }
        }
        
        return upgraded;
    }
    
    /// <summary>
    /// Reset tower state
    /// </summary>
    public override void ResetState()
    {
        base.ResetState();
        
        // Reset to level 1 state
        level = 1;
        canUpgrade = true;
        explosionRadius = 1.5f;
        
        // Reset attack system properties
        if (attackSystem != null)
        {
            attackSystem.attackDamage = baseDamage;
            attackSystem.attackSpeed = baseAttackSpeed;
            attackSystem.attackRange = baseRange;
            attackSystem.explosionRadius = explosionRadius;
        }
        
        // Update visuals
        UpdateVisuals();
        
        Debug.Log($"[{towerName}] State reset");
    }
    
    /// <summary>
    /// Update tower visuals
    /// </summary>
    protected override void UpdateVisuals()
    {
        // Update tower appearance based on level
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && levelSprites != null && level <= levelSprites.Length)
        {
            spriteRenderer.sprite = levelSprites[level - 1];
        }
        
        // Add upgrade effect if available
        GameObject upgradeEffect = Resources.Load<GameObject>("Effects/UpgradeEffect");
        if (upgradeEffect != null)
        {
            Instantiate(upgradeEffect, transform.position, Quaternion.identity);
        }
    }
    
    /// <summary>
    /// Draw attack range in scene view
    /// </summary>
    protected override void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, baseRange);
        
        // Draw explosion radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
    
    // Cannon tower specific aiming method
    protected void AimAt(Transform target)
    {
        // Cannon rotation logic
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
        
        // Continuously rotate tower toward target
        if (target != null)
        {
            AimAt(target);
        }
    }
}

/// <summary>
/// Cannon projectile class, handles projectile flight and explosion logic
/// </summary>
public class CannonProjectile : MonoBehaviour
{
    private GameObject target;
    private float damage;
    private float explosionRadius;
    private GameObject explosionPrefab;
    private float speed = 15f;
    private float arcHeight = 1f;
    private AudioClip explosionSound;
    
    private Vector3 startPosition;
    private float journeyLength;
    private float startTime;
    
    public void Initialize(GameObject target, float damage, float explosionRadius, GameObject explosionPrefab)
    {
        this.target = target;
        this.damage = damage;
        this.explosionRadius = explosionRadius;
        this.explosionPrefab = explosionPrefab;
        
        startPosition = transform.position;
        startTime = Time.time;
        
        if (target != null)
        {
            journeyLength = Vector3.Distance(startPosition, target.transform.position);
        }
    }
    
    public void SetSpeed(float speed)
    {
        this.speed = speed;
    }
    
    public void SetArc(float arcHeight)
    {
        this.arcHeight = arcHeight;
    }
    
    public void SetExplosionSound(AudioClip sound)
    {
        this.explosionSound = sound;
    }
    
    void Update()
    {
        if (target == null)
        {
            Explode();
            return;
        }
        
        // Calculate flight time
        float distanceCovered = (Time.time - startTime) * speed;
        float fractionOfJourney = distanceCovered / journeyLength;
        
        // Parabolic trajectory
        Vector3 currentPosition = Vector3.Lerp(startPosition, target.transform.position, fractionOfJourney);
        
        // Add parabolic height
        float parabola = Mathf.Sin(fractionOfJourney * Mathf.PI) * arcHeight;
        currentPosition.y += parabola;
        
        transform.position = currentPosition;
        
        // Rotate projectile toward flight direction
        if (fractionOfJourney > 0.01f)
        {
            Vector3 direction = (currentPosition - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        // Reach target
        if (fractionOfJourney >= 0.99f)
        {
            Explode();
        }
    }
    
    void Explode()
    {
        // Create explosion effect
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 2f);
            
            // Play explosion sound
            if (explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(explosionSound, transform.position, 0.8f);
            }
        }
        
        // Damage enemies in range
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                EnemyHealth health = collider.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    // Calculate damage falloff based on distance
                    float distance = Vector2.Distance(transform.position, collider.transform.position);
                    float damageFactor = 1f - (distance / explosionRadius);
                    float actualDamage = damage * Mathf.Max(0.5f, damageFactor);
                    
                    health.TakeDamage(actualDamage);
                }
            }
        }
        
        // Destroy projectile
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Draw explosion radius in scene view
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}