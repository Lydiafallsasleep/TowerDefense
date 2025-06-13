using UnityEngine;

/// <summary>
/// 敌人对象池组件
/// </summary>
[RequireComponent(typeof(EnemyMovement))]
public class EnemyPoolObject : ResuableObject
{
    [Tooltip("敌人存活时间（秒）")]
    public float lifeTime = 60f;
    
    private float timer = 0f;
    private bool isActive = false;
    
    private EnemyMovement movement;
    private Rigidbody2D rb;
    private Animator animator;

    private void Awake()
    {
        // 确保所有必要组件都存在
        EnsureComponents();
    }
    
    private void EnsureComponents()
    {
        // 获取或添加必要组件
        movement = GetComponent<EnemyMovement>();
        if (movement == null)
        {
            Debug.LogWarning($"在{gameObject.name}上添加缺失的EnemyMovement组件");
            movement = gameObject.AddComponent<EnemyMovement>();
        }
        
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogWarning($"在{gameObject.name}上添加缺失的Rigidbody2D组件");
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
        }
        
        // 确保有碰撞体组件
        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            Debug.LogWarning($"在{gameObject.name}上添加缺失的CircleCollider2D组件");
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.radius = 0.7f;  // 设置适当的碰撞体大小
            circleCollider.isTrigger = true;  // 设置为触发器，避免物理碰撞
        }
        
        // Animator是可选的
        animator = GetComponent<Animator>();
        
        // 确保有SpriteRenderer
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            Debug.LogWarning($"在{gameObject.name}上添加缺失的SpriteRenderer组件");
            renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.color = gameObject.name.Contains("Slime") ? Color.green : Color.blue;
        }
        
        // 确保有EnemyHealth
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health == null)
        {
            Debug.LogWarning($"在{gameObject.name}上添加缺失的EnemyHealth组件");
            health = gameObject.AddComponent<EnemyHealth>();
        }
    }

    private void OnEnable()
    {
        timer = 0f;
        isActive = true;
    }
    
    private void Update()
    {
        if (!isActive)
            return;
            
        timer += Time.deltaTime;
        
        if (timer >= lifeTime)
        {
            RecycleToPool();
        }
    }
    
    /// <summary>
    /// 将敌人回收到对象池
    /// </summary>
    public void RecycleToPool()
    {
        isActive = false;
        
        // 重置敌人状态
        ResetState();
        
        // 回收到对象池
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.OnUnspawn(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 重置敌人状态
    /// </summary>
    private void ResetState()
    {
        // 重置敌人的各种组件状态
        
        // 重置生命值
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.ResetHealth();
        }
        
        // 重置移动
        EnemyMovement movement = GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.ResetPath();
            movement.enabled = true;
        }
        
        // 重置动画
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Rebind();
        }
        
        // 重置刚体
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = false;
        }
        
        // 重置计时器
        timer = 0f;
    }

    public override void OnSpawn()
    {
        // 确保所有必要组件都存在
        EnsureComponents();
        
        // 重置敌人状态
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 确保EnemyMovement组件状态被重置
        if (movement != null)
        {
            // 直接重置EnemyMovement的关键状态，而不是通过SetActive触发OnEnable
            movement.ResetState();
        }

        // 激活游戏对象上所有组件
        foreach (var behaviour in GetComponentsInChildren<Behaviour>())
        {
            behaviour.enabled = true;
        }
        
        // 确保动画组件激活
        if (animator != null)
        {
            animator.enabled = true;
        }
        
        // 确保对象有正确的标签
        gameObject.tag = "Enemy";
        
        Debug.Log($"敌人 {gameObject.name} 被激活，位置：{transform.position}");
    }

    public override void OnDespawn()
    {
        try 
        {
            // 停止所有移动
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            // 停止动画
            if (animator != null)
            {
                animator.enabled = false;
            }
            
            // 禁用游戏对象
            gameObject.SetActive(false);
            
            Debug.Log($"敌人 {gameObject.name} 被正确回收");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"回收敌人 {gameObject.name} 时出错: {e.Message}");
            // 确保对象被禁用
            try
            {
                gameObject.SetActive(false);
            }
            catch { }
        }
    }
} 