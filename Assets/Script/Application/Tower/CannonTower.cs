using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class CannonTower : BaseTower
{
    [Header("炮塔特有属性")]
    public GameObject cannonballPrefab;
    public Transform firePoint;
    public float explosionRadius = 1.5f; // 爆炸范围
    public float projectileSpeed = 15f;
    public AudioClip shootSound;
    
    private AudioSource audioSource;
    
    [Header("升级特性")]
    public float explosionRadiusIncreasePerLevel = 0.3f;
    
    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
    }
    
    protected override void Start()
    {
        towerName = "炮塔";
        
        // 炮塔具有较高的伤害，但攻击速度较慢
        damage = 30f;
        fireRate = 0.8f;
        range = 6f;
        
        // 调整升级特性
        damageIncreasePerLevel = 15f; // 每级伤害加15
        rangeIncreasePerLevel = 0.5f;  // 每级范围加0.5
        fireRateIncreasePerLevel = 0.1f; // 每级攻速加0.1
        
        buildCost = 150;
        upgradeCost = 100;
        
        base.Start();
    }
    
    protected override void Attack()
    {
        // 播放射击音效
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        
        // 创建炮弹
        GameObject cannonballGO = Instantiate(cannonballPrefab, firePoint.position, firePoint.rotation);
        Projectile cannonball = cannonballGO.GetComponent<Projectile>();
        
        if (cannonball != null)
        {
            cannonball.damage = this.damage;
            cannonball.explosionRadius = this.explosionRadius;
            cannonball.speed = this.projectileSpeed;
            cannonball.Seek(target);
        }
    }
    
    public override bool Upgrade()
    {
        if (!base.Upgrade())
            return false;
            
        // 增加爆炸范围
        explosionRadius += explosionRadiusIncreasePerLevel;
        
        return true;
    }
    
    // 炮塔特有的瞄准方法
    protected void AimAt(Transform target)
    {
        // 炮塔旋转逻辑
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
        
        // 实时旋转炮塔朝向目标
        if (target != null)
        {
            AimAt(target);
        }
    }
} 