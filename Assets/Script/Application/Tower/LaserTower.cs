using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LaserTower : BaseTower
{
    [Header("激光塔特有属性")]
    public LineRenderer laserLineRenderer;
    public Transform firePoint;
    public AudioClip laserSound;
    public float damageRate = 0.1f; // 每秒对敌人造成伤害的频率
    
    [Header("激光效果")]
    public bool hasSlowEffect = false;  // 减速效果
    public float slowFactor = 0.5f;     // 减速幅度，0.5表示速度减半
    public float beamWidth = 0.1f;      // 激光宽度
    public Color beamColor = Color.red; // 激光颜色
    public bool hasPenetration = false; // 是否可以穿透多个敌人
    public int maxTargets = 1;          // 最大目标数量
    
    private AudioSource audioSource;
    private float damageTimer;
    private List<Transform> currentTargets = new List<Transform>();
    private List<EnemyMovement> slowedEnemies = new List<EnemyMovement>();
    
    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
        
        // 如果没有LineRenderer，创建一个
        if (laserLineRenderer == null)
        {
            laserLineRenderer = gameObject.AddComponent<LineRenderer>();
            laserLineRenderer.startWidth = beamWidth;
            laserLineRenderer.endWidth = beamWidth;
            laserLineRenderer.positionCount = 2;
            laserLineRenderer.useWorldSpace = true;
            
            // 使用Unity默认的Material
            laserLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            laserLineRenderer.startColor = beamColor;
            laserLineRenderer.endColor = beamColor;
        }
        
        // 默认禁用激光
        laserLineRenderer.enabled = false;
    }
    
    protected override void Start()
    {
        towerName = "激光塔";
        
        // 激光塔具有持续伤害，每秒伤害较低但稳定
        damage = 5f; // 每次伤害
        fireRate = 0f; // 激光塔不使用普通的攻击频率
        range = 6f;
        
        // 调整升级特性
        damageIncreasePerLevel = 3f;  
        rangeIncreasePerLevel = 0.7f;
        
        buildCost = 175;
        upgradeCost = 125;
        
        base.Start();
        
        // 如果没有设置发射点，使用自身位置
        if (firePoint == null)
        {
            firePoint = transform;
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
        
        // 排序敌人数组，找到优先级最高的敌人
        SortEnemiesByPriority(ref enemies);
        
        // 获取穿透效果允许的目标数量
        int targetsToFind = hasPenetration ? maxTargets : 1;
        
        // 找到范围内的敌人，最多找到允许的目标数量
        foreach (GameObject enemy in enemies)
        {
            if (!enemy.activeSelf || currentTargets.Count >= targetsToFind)
                continue;
                
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            
            // 如果敌人在范围内，添加到目标列表
            if (distanceToEnemy <= range)
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
    
    // 对敌人排序，根据当前的目标优先级
    private void SortEnemiesByPriority(ref GameObject[] enemies)
    {
        // 使用LINQ或其他方法排序敌人数组
        System.Array.Sort(enemies, (a, b) => {
            if (a == null || b == null) return 0;
            
            float valueA = EvaluateTargetPriority(a);
            float valueB = EvaluateTargetPriority(b);
            
            return -valueA.CompareTo(valueB); // 降序排序，高优先级在前
        });
    }
    
    // 根据目标优先级评估敌人值
    private float EvaluateTargetPriority(GameObject enemy)
    {
        float value = 0;
        float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
        
        // 如果超出范围，返回最低优先级
        if (distanceToEnemy > range)
            return float.MinValue;
            
        EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        
        switch (targetPriority)
        {
            case TargetPriority.First:
                if (movement != null) value = movement.GetPathProgress();
                break;
            case TargetPriority.Last:
                if (movement != null) value = -movement.GetPathProgress();
                break;
            case TargetPriority.Strongest:
                if (health != null) value = health.GetCurrentHealth();
                break;
            case TargetPriority.Weakest:
                if (health != null) value = -health.GetCurrentHealth();
                break;
            case TargetPriority.Closest:
                value = -distanceToEnemy;
                break;
            case TargetPriority.Furthest:
                value = distanceToEnemy;
                break;
        }
        
        return value;
    }
    
    protected override void Update()
    {
        // 重写Update方法，不使用基类的攻击冷却逻辑
        if (!isActive)
            return;
            
        // 更新目标
        UpdateTarget();
        
        // 处理激光效果
        if (target != null)
        {
            // 瞄准目标
            AimAt(target);
            
            // 激活激光
            laserLineRenderer.enabled = true;
            
            // 更新激光位置
            UpdateLaser();
            
            // 播放激光声音
            if (audioSource != null && laserSound != null && !audioSource.isPlaying)
            {
                audioSource.clip = laserSound;
                audioSource.Play();
            }
            
            // 处理伤害计时器
            damageTimer += Time.deltaTime;
            if (damageTimer >= damageRate)
            {
                damageTimer = 0f;
                ApplyDamage();
            }
        }
        else
        {
            // 没有目标，停止激光
            laserLineRenderer.enabled = false;
            
            // 停止声音
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            // 清除所有减速效果
            RemoveAllSlowEffects();
        }
    }
    
    // 更新激光位置
    private void UpdateLaser()
    {
        if (firePoint != null && laserLineRenderer != null)
        {
            laserLineRenderer.SetPosition(0, firePoint.position);
            
            // 如果有多个目标且启用了穿透，绘制多段激光
            if (hasPenetration && currentTargets.Count > 1)
            {
                // 计算激光起点和终点
                Vector3 lastPoint = firePoint.position;
                
                // 调整LineRenderer的点数
                laserLineRenderer.positionCount = currentTargets.Count + 1;
                
                // 设置起点
                laserLineRenderer.SetPosition(0, lastPoint);
                
                // 设置每个目标点
                for (int i = 0; i < currentTargets.Count; i++)
                {
                    if (currentTargets[i] != null)
                    {
                        laserLineRenderer.SetPosition(i + 1, currentTargets[i].position);
                    }
                }
            }
            else if (target != null)
            {
                // 单目标模式
                laserLineRenderer.positionCount = 2;
                laserLineRenderer.SetPosition(1, target.position);
            }
        }
    }
    
    // 对目标造成伤害
    private void ApplyDamage()
    {
        foreach (Transform enemyTransform in currentTargets)
        {
            if (enemyTransform == null)
                continue;
                
            EnemyHealth health = enemyTransform.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
            
            // 应用减速效果
            if (hasSlowEffect)
            {
                EnemyMovement movement = enemyTransform.GetComponent<EnemyMovement>();
                if (movement != null && !slowedEnemies.Contains(movement))
                {
                    ApplySlowEffect(movement);
                }
            }
        }
    }
    
    // 应用减速效果
    private void ApplySlowEffect(EnemyMovement enemy)
    {
        if (enemy == null || slowedEnemies.Contains(enemy))
            return;
            
        // 保存原始速度
        enemy.originalMoveSpeed = enemy.moveSpeed;
        
        // 减速
        enemy.moveSpeed *= slowFactor;
        
        // 添加到减速列表
        slowedEnemies.Add(enemy);
    }
    
    // 移除减速效果
    private void RemoveSlowEffect(EnemyMovement enemy)
    {
        if (enemy == null)
            return;
            
        // 恢复速度（如果敌人还活着）
        if (enemy.gameObject.activeSelf)
        {
            enemy.moveSpeed = enemy.originalMoveSpeed;
        }
        
        // 从减速列表中移除
        slowedEnemies.Remove(enemy);
    }
    
    // 移除所有减速效果
    private void RemoveAllSlowEffects()
    {
        foreach (EnemyMovement enemy in slowedEnemies)
        {
            if (enemy != null && enemy.gameObject.activeSelf)
            {
                enemy.moveSpeed = enemy.originalMoveSpeed;
            }
        }
        
        slowedEnemies.Clear();
    }
    
    // 塔被销毁时的清理
    protected void OnDestroy()
    {
        RemoveAllSlowEffects();
    }
    
    // 激光塔特有的瞄准方法
    private void AimAt(Transform target)
    {
        if (target != null && firePoint != null)
        {
            Vector3 dir = target.position - firePoint.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            firePoint.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
    
    protected override void Attack()
    {
        // 激光塔不使用常规Attack方法，而是在Update中处理持续伤害
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
        
        // 二级：获得穿透效果
        else if (level == 3)
        {
            slowFactor = 0.3f;  // 更强减速
            hasPenetration = true;  // 获得穿透效果
            maxTargets = 3;  // 最多穿透3个敌人
            beamWidth = 0.15f;  // 增加激光宽度
            laserLineRenderer.startWidth = beamWidth;
            laserLineRenderer.endWidth = beamWidth;
            beamColor = Color.cyan;  // 更改激光颜色
            laserLineRenderer.startColor = beamColor;
            laserLineRenderer.endColor = beamColor;
        }
        
        return true;
    }
} 