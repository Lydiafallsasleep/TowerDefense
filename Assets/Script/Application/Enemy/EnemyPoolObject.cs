using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
public class EnemyPoolObject : ResuableObject
{
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