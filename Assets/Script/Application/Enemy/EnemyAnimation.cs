using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(EnemyMovement))]
public class EnemyAnimation : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public RuntimeAnimatorController slimeAnimator;
    public RuntimeAnimatorController fishAnimator;
    
    private SpriteRenderer spriteRenderer;
    private EnemyMovement movement;
    private Rigidbody2D rb;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        movement = GetComponent<EnemyMovement>();
        rb = GetComponent<Rigidbody2D>();
        
        if(animator == null)
            animator = gameObject.AddComponent<Animator>();
    }

    private void OnEnable()
    {
        // 根据敌人类型设置对应的动画控制器
        if (movement != null && animator != null)
        {
            if (movement.monsterType == EnemyMovement.MonsterType.Slime && slimeAnimator != null)
            {
                animator.runtimeAnimatorController = slimeAnimator;
            }
            else if (movement.monsterType == EnemyMovement.MonsterType.Fish && fishAnimator != null)
            {
                animator.runtimeAnimatorController = fishAnimator;
            }
        }
        
        // 确保动画正在播放
        if (animator != null)
        {
            animator.enabled = true;
        }
    }

    private void Update()
    {
        // 根据移动方向翻转精灵
        if (rb != null && rb.velocity.x != 0)
        {
            spriteRenderer.flipX = rb.velocity.x < 0;
        }
    }
}