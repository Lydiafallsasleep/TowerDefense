using UnityEngine;
using System.Collections;

public enum TargetPriority 
{
    First,      // 第一个进入范围的敌人
    Last,       // 最后一个进入范围的敌人
    Strongest,  // 生命值最高的敌人
    Weakest,    // 生命值最低的敌人
    Closest,    // 最近的敌人
    Furthest    // 最远的敌人
}

public abstract class BaseTower : MonoBehaviour
{
    [Header("基础属性")]
    public string towerName;
    public int level = 1;
    public int maxLevel = 3;
    public int buildCost = 100;
    public int upgradeCost = 150;
    public float range = 5f;
    public float damage = 10f;
    public float fireRate = 1f; // 每秒攻击次数
    public TargetPriority targetPriority = TargetPriority.First;
    public GameObject rangeIndicator;

    [Header("升级加成")]
    public float damageIncreasePerLevel = 5f;
    public float rangeIncreasePerLevel = 0.5f;
    public float fireRateIncreasePerLevel = 0.2f;

    [Header("状态")]
    protected Transform target;
    protected float fireCountdown = 0f;
    protected bool isActive = true;

    [Header("组件引用")]
    protected SpriteRenderer spriteRenderer;
    public Sprite[] levelSprites; // 不同等级的外观

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected virtual void Start()
    {
        InvokeRepeating("UpdateTarget", 0f, 0.5f); // 每0.5秒更新目标
        UpdateVisuals();
    }

    protected virtual void Update()
    {
        if (!isActive || target == null)
            return;

        // 处理攻击冷却
        if (fireCountdown > 0)
        {
            fireCountdown -= Time.deltaTime;
        }
        else
        {
            Attack();
            fireCountdown = 1f / fireRate;
        }
    }

    protected virtual void UpdateTarget()
    {
        // 获取所有带有"Enemy"标签的游戏对象
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        if (enemies.Length == 0)
            return;

        Transform bestTarget = null;
        float bestTargetValue = 0f;
        bool isValueInitialized = false;

        foreach (GameObject enemy in enemies)
        {
            if (!enemy.activeSelf)
                continue;
                
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            
            // 如果敌人超出范围，跳过
            if (distanceToEnemy > range)
                continue;

            float targetValue = 0f;
            switch (targetPriority)
            {
                case TargetPriority.First:
                    // 路径进度最高的敌人
                    EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
                    if (movement != null)
                    {
                        targetValue = movement.GetPathProgress();
                    }
                    break;
                case TargetPriority.Last:
                    // 路径进度最低的敌人
                    movement = enemy.GetComponent<EnemyMovement>();
                    if (movement != null)
                    {
                        targetValue = -movement.GetPathProgress(); // 取负值使最小值成为优先目标
                    }
                    break;
                case TargetPriority.Strongest:
                    // 生命值最高的敌人
                    var health = enemy.GetComponent<EnemyHealth>(); // 假设有此组件
                    if (health != null)
                    {
                        targetValue = health.GetCurrentHealth();
                    }
                    break;
                case TargetPriority.Weakest:
                    // 生命值最低的敌人
                    health = enemy.GetComponent<EnemyHealth>();
                    if (health != null)
                    {
                        targetValue = -health.GetCurrentHealth(); // 取负值使最小值成为优先目标
                    }
                    break;
                case TargetPriority.Closest:
                    // 距离最近的敌人
                    targetValue = -distanceToEnemy; // 取负值使最小距离成为优先目标
                    break;
                case TargetPriority.Furthest:
                    // 距离最远的敌人（仍在范围内）
                    targetValue = distanceToEnemy;
                    break;
            }

            if (bestTarget == null || !isValueInitialized || targetValue > bestTargetValue)
            {
                bestTargetValue = targetValue;
                isValueInitialized = true;
                bestTarget = enemy.transform;
            }
        }

        target = bestTarget;
    }

    // 子类必须实现攻击方法
    protected abstract void Attack();

    // 升级塔
    public virtual bool Upgrade()
    {
        if (level >= maxLevel)
            return false;

        level++;
        damage += damageIncreasePerLevel;
        range += rangeIncreasePerLevel;
        fireRate += fireRateIncreasePerLevel;

        UpdateVisuals();
        return true;
    }

    // 更新塔的视觉效果，根据等级
    protected virtual void UpdateVisuals()
    {
        if (spriteRenderer != null && levelSprites != null && level <= levelSprites.Length)
        {
            spriteRenderer.sprite = levelSprites[level - 1];
        }

        // 更新范围指示器
        if (rangeIndicator != null)
        {
            rangeIndicator.transform.localScale = new Vector3(range * 2, range * 2, 1f);
        }
    }

    // 在Unity编辑器中绘制塔的攻击范围
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }

    // 获取升级费用
    public virtual int GetUpgradeCost()
    {
        return upgradeCost * level;
    }

    // 获取售出价值（一般为建造成本和升级成本总和的一部分）
    public virtual int GetSellValue()
    {
        int totalCost = buildCost;
        for (int i = 1; i < level; i++)
        {
            totalCost += upgradeCost * i;
        }
        return Mathf.FloorToInt(totalCost * 0.7f); // 返回70%的成本
    }

    // 重置塔的状态
    public virtual void ResetState()
    {
        Debug.Log($"[{towerName}] 重置塔状态");
        
        // 重置目标
        target = null;
        
        // 重置攻击冷却
        fireCountdown = 0f;
        
        // 重置等级（如果需要）
        // 如果需要保留当前等级，则注释掉以下代码
        if (level > 1)
        {
            level = 1;
            damage = damage - damageIncreasePerLevel * (level - 1);
            range = range - rangeIncreasePerLevel * (level - 1);
            fireRate = fireRate - fireRateIncreasePerLevel * (level - 1);
            UpdateVisuals();
        }
        
        // 重新开始寻找目标
        CancelInvoke("UpdateTarget");
        InvokeRepeating("UpdateTarget", 0f, 0.5f);
    }
} 