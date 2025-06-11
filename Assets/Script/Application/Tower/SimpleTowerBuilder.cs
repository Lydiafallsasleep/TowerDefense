using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 运行时构建塔预制体的工具类，解决塔预制体缺失问题
/// </summary>
public class SimpleTowerBuilder : MonoBehaviour
{
    [Header("预制体保存设置")]
    public bool saveToResources = true;

    [Header("基础组件")]
    public Sprite cannonTowerSprite;
    public Sprite arrowTowerSprite;
    public Sprite laserTowerSprite;
    
    [Header("投射物")]
    public Sprite cannonballSprite;
    public Sprite arrowSprite;
    
    // 在启动时自动创建预制体
    void Awake()
    {
        Debug.Log("初始化塔预制体...");
        CreateTowerPrefabs();
    }
    
    // 创建所有塔预制体
    public void CreateTowerPrefabs()
    {
        CreateCannonTower();
        CreateArrowTower();
        CreateLaserTower();
        CreateProjectiles();
        Debug.Log("塔预制体初始化完成");
    }
    
    // 创建炮塔预制体
    private GameObject CreateCannonTower()
    {
        GameObject towerObj = new GameObject("CannonTower");
        
        // 添加基础组件
        SpriteRenderer spriteRenderer = towerObj.AddComponent<SpriteRenderer>();
        if (cannonTowerSprite != null)
        {
            spriteRenderer.sprite = cannonTowerSprite;
        }
        else
        {
            // 创建一个简单的圆形作为默认图像
            spriteRenderer.color = Color.gray;
        }
        
        // 添加塔脚本
        CannonTower tower = towerObj.AddComponent<CannonTower>();
        
        // 添加碰撞器用于选择
        CircleCollider2D collider = towerObj.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        
        // 添加音源
        AudioSource audioSource = towerObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        // 创建发射点
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(towerObj.transform);
        firePoint.transform.localPosition = new Vector3(0, 0.5f, 0);
        tower.firePoint = firePoint.transform;
        
        // 设置基本属性
        tower.towerName = "炮塔";
        tower.damage = 30f;
        tower.fireRate = 0.8f;
        tower.range = 6f;
        tower.buildCost = 150;
        tower.upgradeCost = 100;
        
        // 将预制体保存到Resources文件夹
        if (saveToResources)
        {
            GameObject prefabInstance = SaveTowerPrefab("tower/CannonTower", towerObj);
            towerObj = prefabInstance; // 使用保存的实例
        }
        
        // 默认不激活
        towerObj.SetActive(false);
        
        return towerObj;
    }
    
    // 创建箭塔预制体
    private GameObject CreateArrowTower()
    {
        GameObject towerObj = new GameObject("ArrowTower");
        
        // 添加基础组件
        SpriteRenderer spriteRenderer = towerObj.AddComponent<SpriteRenderer>();
        if (arrowTowerSprite != null)
        {
            spriteRenderer.sprite = arrowTowerSprite;
        }
        else
        {
            // 创建一个简单的方形作为默认图像
            spriteRenderer.color = Color.green;
        }
        
        // 添加塔脚本
        ArrowTower tower = towerObj.AddComponent<ArrowTower>();
        
        // 添加碰撞器用于选择
        CircleCollider2D collider = towerObj.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        
        // 添加音源
        AudioSource audioSource = towerObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        // 创建发射点
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(towerObj.transform);
        firePoint.transform.localPosition = new Vector3(0, 0.5f, 0);
        tower.firePoint = firePoint.transform;
        
        // 设置基本属性
        tower.towerName = "箭塔";
        tower.damage = 8f;
        tower.fireRate = 2.5f;
        tower.range = 5f;
        tower.buildCost = 100;
        tower.upgradeCost = 75;
        
        // 将预制体保存到Resources文件夹
        if (saveToResources)
        {
            GameObject prefabInstance = SaveTowerPrefab("tower/ArrowTower", towerObj);
            towerObj = prefabInstance; // 使用保存的实例
        }
        
        // 默认不激活
        towerObj.SetActive(false);
        
        return towerObj;
    }
    
    // 创建激光塔预制体
    private GameObject CreateLaserTower()
    {
        GameObject towerObj = new GameObject("LaserTower");
        
        // 添加基础组件
        SpriteRenderer spriteRenderer = towerObj.AddComponent<SpriteRenderer>();
        if (laserTowerSprite != null)
        {
            spriteRenderer.sprite = laserTowerSprite;
        }
        else
        {
            // 创建一个简单的方形作为默认图像
            spriteRenderer.color = Color.red;
        }
        
        // 添加塔脚本
        LaserTower tower = towerObj.AddComponent<LaserTower>();
        
        // 添加碰撞器用于选择
        CircleCollider2D collider = towerObj.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        
        // 添加音源
        AudioSource audioSource = towerObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        // 添加线渲染器作为激光
        LineRenderer lineRenderer = towerObj.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.yellow;
        tower.laserLineRenderer = lineRenderer;
        lineRenderer.enabled = false;
        
        // 创建发射点
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(towerObj.transform);
        firePoint.transform.localPosition = new Vector3(0, 0.5f, 0);
        tower.firePoint = firePoint.transform;
        
        // 设置基本属性
        tower.towerName = "激光塔";
        tower.damage = 20f;
        tower.fireRate = 0f; // 持续伤害
        tower.range = 4f;
        tower.buildCost = 175;
        tower.upgradeCost = 125;
        tower.damageRate = 0.1f;
        
        // 将预制体保存到Resources文件夹
        if (saveToResources)
        {
            GameObject prefabInstance = SaveTowerPrefab("tower/LaserTower", towerObj);
            towerObj = prefabInstance; // 使用保存的实例
        }
        
        // 默认不激活
        towerObj.SetActive(false);
        
        return towerObj;
    }
    
    // 创建投射物预制体
    private void CreateProjectiles()
    {
        // 创建炮弹预制体
        GameObject cannonball = new GameObject("Cannonball");
        SpriteRenderer cannonballRenderer = cannonball.AddComponent<SpriteRenderer>();
        if (cannonballSprite != null)
        {
            cannonballRenderer.sprite = cannonballSprite;
        }
        else
        {
            cannonballRenderer.color = Color.black;
        }
        
        Projectile cannonballScript = cannonball.AddComponent<Projectile>();
        cannonballScript.damage = 30f;
        cannonballScript.speed = 15f;
        cannonballScript.explosionRadius = 1.5f;
        
        CircleCollider2D cannonballCollider = cannonball.AddComponent<CircleCollider2D>();
        cannonballCollider.radius = 0.2f;
        cannonballCollider.isTrigger = true;
        
        // 保存预制体
        if (saveToResources)
        {
            SaveTowerPrefab("tower/Cannonball", cannonball);
        }
        
        // 创建箭矢预制体
        GameObject arrow = new GameObject("Arrow");
        SpriteRenderer arrowRenderer = arrow.AddComponent<SpriteRenderer>();
        if (arrowSprite != null)
        {
            arrowRenderer.sprite = arrowSprite;
        }
        else
        {
            arrowRenderer.color = Color.yellow;
        }
        
        Projectile arrowScript = arrow.AddComponent<Projectile>();
        arrowScript.damage = 8f;
        arrowScript.speed = 30f;
        arrowScript.explosionRadius = 0f;
        
        BoxCollider2D arrowCollider = arrow.AddComponent<BoxCollider2D>();
        arrowCollider.size = new Vector2(0.5f, 0.1f);
        arrowCollider.isTrigger = true;
        
        // 保存预制体
        if (saveToResources)
        {
            SaveTowerPrefab("tower/Arrow", arrow);
        }
    }
    
    // 将游戏对象保存为预制体
    private GameObject SaveTowerPrefab(string path, GameObject obj)
    {
        // 在运行时，我们不能真正地创建预制体资源
        // 但我们可以创建不会被销毁的游戏对象，然后作为预制体使用
        GameObject instance = Instantiate(obj);
        DontDestroyOnLoad(instance);
        instance.SetActive(false);
        
        Debug.Log($"创建了预制体: {path}");
        return instance;
    }
} 