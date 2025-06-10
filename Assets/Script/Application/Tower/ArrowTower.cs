using UnityEngine;
using System.Collections;

public class ArrowTower : BaseTower
{
    [Header("箭塔特有属性")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float projectileSpeed = 30f;
    public AudioClip shootSound;
    
    [Header("多重射击")]
    public bool multiShot = false; // 是否启用多重射击
    public int arrowCount = 1;    // 每次射击的箭矢数量
    public float spreadAngle = 15f; // 箭矢散射角度
    
    private AudioSource audioSource;
    
    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
    }
    
    protected override void Start()
    {
        towerName = "箭塔";
        
        // 箭塔具有较高的攻击速度，但伤害较低
        damage = 8f;
        fireRate = 2.5f;  // 每秒2.5次攻击
        range = 5f;
        
        // 调整升级特性
        damageIncreasePerLevel = 4f;  // 每级伤害加4
        rangeIncreasePerLevel = 0.3f;  // 每级范围加0.3
        fireRateIncreasePerLevel = 0.5f; // 每级攻速加0.5
        
        buildCost = 100;
        upgradeCost = 75;
        
        base.Start();
    }
    
    protected override void Attack()
    {
        // 播放射击音效
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        
        if (multiShot && arrowCount > 1)
        {
            // 多重射击
            float startAngle = -spreadAngle / 2f;
            float angleStep = spreadAngle / (arrowCount - 1);
            
            for (int i = 0; i < arrowCount; i++)
            {
                float currentAngle = startAngle + angleStep * i;
                ShootArrow(currentAngle);
            }
        }
        else
        {
            // 单箭射击
            ShootArrow(0f);
        }
    }
    
    void ShootArrow(float angleOffset)
    {
        // 根据角度偏移调整发射方向
        Quaternion spreadRotation = Quaternion.Euler(0f, 0f, angleOffset);
        Quaternion finalRotation = firePoint.rotation * spreadRotation;
        
        // 创建箭矢
        GameObject arrowGO = Instantiate(arrowPrefab, firePoint.position, finalRotation);
        Projectile arrow = arrowGO.GetComponent<Projectile>();
        
        if (arrow != null)
        {
            arrow.damage = this.damage;
            arrow.speed = this.projectileSpeed;
            
            // 如果有角度偏移，需要手动计算目标位置
            if (angleOffset != 0f && target != null)
            {
                Vector3 direction = target.position - firePoint.position;
                direction = Quaternion.Euler(0, 0, angleOffset) * direction;
                
                // 创建一个虚拟目标点
                GameObject virtualTarget = new GameObject("VirtualTarget");
                virtualTarget.transform.position = firePoint.position + direction.normalized * 50f; // 足够远的距离
                
                arrow.Seek(virtualTarget.transform);
                
                // 在箭矢销毁时销毁虚拟目标
                StartCoroutine(DestroyVirtualTarget(virtualTarget, 5f));
            }
            else if (target != null)
            {
                arrow.Seek(target);
            }
        }
    }
    
    IEnumerator DestroyVirtualTarget(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
            Destroy(target);
    }
    
    public override bool Upgrade()
    {
        if (!base.Upgrade())
            return false;
            
        // 在升级到最高级时获得多重射击能力
        if (level == maxLevel)
        {
            multiShot = true;
            arrowCount = 3;
        }
        
        return true;
    }
    
    // 箭塔特有的瞄准方法
    protected void AimAt(Transform target)
    {
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
        
        // 实时旋转箭塔朝向目标
        if (target != null)
        {
            AimAt(target);
        }
    }
} 