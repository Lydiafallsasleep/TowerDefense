using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MagicTower : BaseTower
{
    [Header("魔法塔特有属性")]
    public GameObject magicProjectilePrefab;
    public Transform[] firePoints;  // 多个发射点
    public float projectileSpeed = 20f;
    public AudioClip castSound;
    
    [Header("魔法效果")]
    public bool hasSlowEffect = false;  // 减速效果
    public float slowFactor = 0.5f;     // 减速幅度，0.5表示速度减半
    public float slowDuration = 2f;     // 减速持续时间
    public bool hasChainLightning = false;  // 连锁闪电效果
    public int chainCount = 2;          // 连锁次数
    public float chainRange = 3f;       // 连锁范围
    
    private AudioSource audioSource;
    private List<Transform> currentTargets = new List<Transform>();
    
    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
    }
    
    protected override void Start()
    {
        towerName = "魔法塔";
        
        // 魔法塔具有中等伤害和攻击速度，但可以同时攻击多个目标
        damage = 15f;
        fireRate = 1.2f;
        range = 5f;
        
        // 调整升级特性
        damageIncreasePerLevel = 8f;  // 每级伤害加8
        rangeIncreasePerLevel = 0.5f;  // 每级范围加0.5
        fireRateIncreasePerLevel = 0.2f; // 每级攻速加0.2
        
        buildCost = 175;
        upgradeCost = 125;
        
        base.Start();
        
        // 如果没有设置发射点，创建一个默认发射点
        if (firePoints == null || firePoints.Length == 0)
        {
            firePoints = new Transform[1] { transform };
        }
    }
    
    protected override void UpdateTarget()
    {
        // 获取所有带有"Enemy"标签的游戏对象
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        if (enemies.Length == 0)
        {
            currentTargets.Clear();
            target = null;
            return;
        }
        
        currentTargets.Clear();
        
        // 找到范围内的所有敌人，最多找到firePoints.Length个
        foreach (GameObject enemy in enemies)
        {
            if (!enemy.activeSelf)
                continue;
                
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            
            // 如果敌人在范围内且目标数量小于发射点数量，添加到目标列表
            if (distanceToEnemy <= range && currentTargets.Count < firePoints.Length)
            {
                currentTargets.Add(enemy.transform);
            }
        }
        
        // 如果有目标，将第一个目标设为主目标
        if (currentTargets.Count > 0)
        {
            target = currentTargets[0];
        }
        else
        {
            target = null;
        }
    }
    
    protected override void Attack()
    {
        // 播放施法音效
        if (audioSource != null && castSound != null)
        {
            audioSource.PlayOneShot(castSound);
        }
        
        // 对每个目标发射魔法弹
        for (int i = 0; i < Mathf.Min(currentTargets.Count, firePoints.Length); i++)
        {
            Transform currentFirePoint = firePoints[i];
            Transform currentTarget = currentTargets[i];
            
            if (currentFirePoint != null && currentTarget != null)
            {
                // 创建魔法弹
                GameObject magicProjectileGO = Instantiate(magicProjectilePrefab, currentFirePoint.position, currentFirePoint.rotation);
                MagicProjectile magicProjectile = magicProjectileGO.GetComponent<MagicProjectile>();
                
                if (magicProjectile == null)
                {
                    // 如果没有特殊的MagicProjectile组件，使用基础的Projectile组件
                    Projectile projectile = magicProjectileGO.GetComponent<Projectile>();
                    if (projectile != null)
                    {
                        projectile.damage = this.damage;
                        projectile.speed = this.projectileSpeed;
                        projectile.Seek(currentTarget);
                    }
                }
                else
                {
                    // 设置魔法弹属性
                    magicProjectile.damage = this.damage;
                    magicProjectile.speed = this.projectileSpeed;
                    magicProjectile.Seek(currentTarget);
                    
                    // 设置特殊效果
                    if (hasSlowEffect)
                    {
                        magicProjectile.SetSlowEffect(slowFactor, slowDuration);
                    }
                    
                    if (hasChainLightning)
                    {
                        magicProjectile.SetChainLightning(chainCount, chainRange, damage * 0.5f);
                    }
                }
            }
        }
    }
    
    public override bool Upgrade()
    {
        if (!base.Upgrade())
            return false;
        
        // 一级：获得减速效果
        if (level == 2)
        {
            hasSlowEffect = true;
        }
        
        // 二级：增强减速效果，减速更强更持久
        else if (level == 3)
        {
            slowFactor = 0.3f;  // 更强减速
            slowDuration = 3f;  // 更长持续时间
            hasChainLightning = true;  // 获得连锁闪电效果
        }
        
        return true;
    }
}

// 魔法弹特殊组件，继承自基础弹药类
public class MagicProjectile : Projectile
{
    // 减速效果
    private bool hasSlowEffect = false;
    private float slowFactor = 1f;
    private float slowDuration = 0f;
    
    // 连锁闪电效果
    private bool hasChainLightning = false;
    private int chainCount = 0;
    private float chainRange = 0f;
    private float chainDamage = 0f;
    
    public void SetSlowEffect(float factor, float duration)
    {
        hasSlowEffect = true;
        slowFactor = factor;
        slowDuration = duration;
    }
    
    public void SetChainLightning(int count, float range, float damagePerChain)
    {
        hasChainLightning = true;
        chainCount = count;
        chainRange = range;
        chainDamage = damagePerChain;
    }
    
    // 重写伤害方法，添加特殊效果
    protected override void Damage(Transform enemy)
    {
        // 基础伤害
        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }
        
        // 应用减速效果
        if (hasSlowEffect)
        {
            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                StartCoroutine(SlowEnemy(movement));
            }
        }
        
        // 应用连锁闪电效果
        if (hasChainLightning && chainCount > 0)
        {
            StartCoroutine(ChainLightning(enemy, chainCount));
        }
    }
    
    IEnumerator SlowEnemy(EnemyMovement enemy)
    {
        // 保存原始速度
        float originalSpeed = enemy.moveSpeed;
        
        // 减速
        enemy.moveSpeed *= slowFactor;
        
        // 等待持续时间
        yield return new WaitForSeconds(slowDuration);
        
        // 恢复速度（如果敌人还活着）
        if (enemy != null && enemy.gameObject.activeSelf)
        {
            enemy.moveSpeed = originalSpeed;
        }
    }
    
    IEnumerator ChainLightning(Transform source, int remainingChains)
    {
        if (remainingChains <= 0)
            yield break;
            
        // 等待一小段时间，看起来更像是连锁效果
        yield return new WaitForSeconds(0.1f);
        
        // 查找范围内的其他敌人
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform closestEnemy = null;
        float closestDistance = chainRange;
        
        foreach (GameObject enemy in enemies)
        {
            if (enemy.transform == source || !enemy.activeSelf)
                continue;
                
            float distance = Vector3.Distance(source.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestEnemy = enemy.transform;
                closestDistance = distance;
            }
        }
        
        if (closestEnemy != null)
        {
            // 创建闪电效果
            LineRenderer lightning = new GameObject("Lightning").AddComponent<LineRenderer>();
            lightning.SetPosition(0, source.position);
            lightning.SetPosition(1, closestEnemy.position);
            lightning.startWidth = 0.1f;
            lightning.endWidth = 0.1f;
            lightning.material = new Material(Shader.Find("Sprites/Default"));
            lightning.startColor = Color.cyan;
            lightning.endColor = Color.blue;
            
            // 造成伤害
            EnemyHealth health = closestEnemy.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.TakeDamage(chainDamage);
            }
            
            // 销毁闪电效果
            Destroy(lightning.gameObject, 0.2f);
            
            // 继续连锁
            StartCoroutine(ChainLightning(closestEnemy, remainingChains - 1));
        }
    }
} 