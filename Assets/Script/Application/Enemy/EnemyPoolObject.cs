using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
public class EnemyPoolObject : ResuableObject
{
    private EnemyMovement movement;
    private Rigidbody2D rb;
    private Animator animator;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    public override void OnSpawn()
    {
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
        
        Debug.Log($"敌人 {gameObject.name} 被激活，位置：{transform.position}");
    }

    public override void OnDespawn()
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
    }
} 