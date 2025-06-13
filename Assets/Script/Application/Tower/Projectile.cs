using UnityEngine;

/// <summary>
/// Projectile class, handles movement and collision of arrows, cannonballs and other projectiles
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Basic Settings")]
    public float speed = 20f;
    public float damage = 10f;
    public float lifeTime = 3f;
    public bool useGravity = false;
    public float gravityScale = 1f;
    public float explosionRadius = 0f; // Explosion radius, 0 means no explosion effect
    
    [Header("Visual Effects")]
    public GameObject impactEffect;
    public bool useTrailEffect = true;
    
    private GameObject target;
    private Vector3 targetLastPosition;
    private Rigidbody2D rb;
    private float timer = 0f;
    private bool hasHit = false;
    private GameObject trailEffect;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        rb.gravityScale = useGravity ? gravityScale : 0f;
        rb.isKinematic = !useGravity;
    }
    
    void Start()
    {
        // Add trail effect
        if (useTrailEffect)
        {
            // Use particle effect manager to add trail
            if (TowerParticleEffects.Instance != null)
            {
                // Choose different trail effects based on projectile type
                if (gameObject.name.Contains("Arrow"))
                {
                    trailEffect = TowerParticleEffects.Instance.AddArrowTrailEffect(gameObject);
                }
                else if (gameObject.name.Contains("Cannon"))
    {
                    trailEffect = TowerParticleEffects.Instance.AddCannonballTrailEffect(gameObject);
                }
            }
        }
    }

    void Update()
    {
        // If already hit target, don't update
        if (hasHit)
            return;
            
        // Lifetime timer
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        // If target exists, track it
        if (target != null && target.activeSelf)
        {
            targetLastPosition = target.transform.position;
        
            // Calculate direction
            Vector3 dir = (targetLastPosition - transform.position).normalized;
        
            // Set velocity
            if (!useGravity)
            {
                rb.velocity = dir * speed;
            }
            
            // Rotate projectile towards target
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else
        {
            // If target doesn't exist, continue moving in current direction
            if (!useGravity && rb.velocity.magnitude < 0.1f)
        {
                // If velocity is too small, give a default direction
                rb.velocity = transform.right * speed;
            }
        }
    }
    
    /// <summary>
    /// Initialize projectile
    /// </summary>
    public void Initialize(GameObject target, float damage)
    {
        this.target = target;
        this.damage = damage;
        
        if (target != null)
        {
            targetLastPosition = target.transform.position;
            
            // Calculate initial direction
            Vector3 dir = (targetLastPosition - transform.position).normalized;
            
            // Set initial velocity
            rb.velocity = dir * speed;
            
            // Set initial rotation
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    /// <summary>
    /// Set projectile speed
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }
    
    /// <summary>
    /// Set projectile gravity
    /// </summary>
    public void SetGravity(bool useGrav, float scale = 1f)
        {
        useGravity = useGrav;
        gravityScale = scale;
        rb.gravityScale = useGravity ? gravityScale : 0f;
    }
    
    void OnTriggerEnter2D(Collider2D other)
        {
        // If already hit, don't process
        if (hasHit)
            return;
            
        // Check if hit an enemy
        if (other.CompareTag("Enemy"))
        {
            // Mark as hit
            hasHit = true;
            
            // Deal damage to enemy
            EnemyHealth health = other.GetComponent<EnemyHealth>();
                if (health != null)
            {
                    health.TakeDamage(damage);
                }
            
            // Play hit effect
            if (TowerParticleEffects.Instance != null)
            {
                // Choose different hit effects based on projectile type
                if (gameObject.name.Contains("Arrow"))
            {
                    TowerParticleEffects.Instance.PlayArrowImpactEffect(transform.position);
                }
                else if (gameObject.name.Contains("Cannon"))
                {
                    TowerParticleEffects.Instance.PlayExplosionEffect(transform.position, 0.7f);
            }
            }
            else if (impactEffect != null)
            {
                // Use traditional way to create hit effect
                Instantiate(impactEffect, transform.position, Quaternion.identity);
            }
            
            // Destroy projectile
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        // Clean up resources
        if (trailEffect != null)
        {
            Destroy(trailEffect);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw projectile path prediction
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.transform.position);
        }
    }
} 