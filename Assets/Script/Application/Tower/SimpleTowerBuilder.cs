using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Runtime tower prefab builder utility class to handle missing tower prefabs
/// </summary>
public class SimpleTowerBuilder : MonoBehaviour
{
    [Header("Prefab Saving Settings")]
    public bool saveToResources = true;
    public string resourcePath = "tower"; // Resource path for saving prefabs

    [Header("Base Components")]
    public Sprite cannonTowerSprite;
    public Sprite arrowTowerSprite;
    public Sprite laserTowerSprite;
    
    [Header("Projectiles")]
    public Sprite cannonballSprite;
    public Sprite arrowSprite;
    
    // Automatically create prefabs on startup
    void Awake()
    {
        Debug.Log("Initializing tower prefabs...");
        CreateTowerPrefabs();
    }
    
    // Create all tower prefabs
    public void CreateTowerPrefabs()
    {
        CreateCannonTower();
        CreateArrowTower();
        CreateLaserTower();
        CreateProjectiles();
        Debug.Log("Tower prefab initialization complete");
    }
    
    // Create cannon tower prefab
    private GameObject CreateCannonTower()
    {
        GameObject towerObj = new GameObject("CannonTower");
        
        // Add base components
        SpriteRenderer spriteRenderer = towerObj.AddComponent<SpriteRenderer>();
        if (cannonTowerSprite != null)
        {
            spriteRenderer.sprite = cannonTowerSprite;
        }
        else
        {
            // Create simple circle as default sprite
            spriteRenderer.color = Color.gray;
        }
        
        // Add tower script
        CannonTower tower = towerObj.AddComponent<CannonTower>();
        
        // Add collider for selection
        CircleCollider2D collider = towerObj.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        
        // Add audio source
        AudioSource audioSource = towerObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        // Create fire point
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(towerObj.transform);
        firePoint.transform.localPosition = new Vector3(0, 0.5f, 0);
        tower.firePoint = firePoint.transform;
        
        // Set base properties
        tower.towerName = "Cannon Tower";
        tower.damage = 30f;
        tower.fireRate = 0.8f;
        tower.range = 6f;
        tower.cost = 150;
        tower.upgradePrice = 100;
        
        // Save prefab to Resources folder
        if (saveToResources)
        {
            string path = resourcePath + "/CannonTower";
            GameObject prefabInstance = SaveTowerPrefab(path, towerObj);
            towerObj = prefabInstance; // Use saved instance
        }
        
        // Deactivate by default
        towerObj.SetActive(false);
        
        return towerObj;
    }
    
    // Create arrow tower prefab
    private GameObject CreateArrowTower()
    {
        GameObject towerObj = new GameObject("ArrowTower");
        
        // Add base components
        SpriteRenderer spriteRenderer = towerObj.AddComponent<SpriteRenderer>();
        if (arrowTowerSprite != null)
        {
            spriteRenderer.sprite = arrowTowerSprite;
        }
        else
        {
            // Create simple square as default sprite
            spriteRenderer.color = Color.green;
        }
        
        // Add tower script
        ArrowTower tower = towerObj.AddComponent<ArrowTower>();
        
        // Add collider for selection
        CircleCollider2D collider = towerObj.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        
        // Add audio source
        AudioSource audioSource = towerObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        // Create fire point
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(towerObj.transform);
        firePoint.transform.localPosition = new Vector3(0, 0.5f, 0);
        tower.firePoint = firePoint.transform;
        
        // Set base properties
        tower.towerName = "Arrow Tower";
        tower.damage = 8f;
        tower.fireRate = 2.5f;
        tower.range = 5f;
        tower.cost = 100;
        tower.upgradePrice = 75;
        
        // Save prefab to Resources folder
        if (saveToResources)
        {
            string path = resourcePath + "/ArrowTower";
            GameObject prefabInstance = SaveTowerPrefab(path, towerObj);
            towerObj = prefabInstance; // Use saved instance
        }
        
        // Deactivate by default
        towerObj.SetActive(false);
        
        return towerObj;
    }
    
    // Create laser tower prefab
    private GameObject CreateLaserTower()
    {
        GameObject towerObj = new GameObject("LaserTower");
        
        // Add base components
        SpriteRenderer spriteRenderer = towerObj.AddComponent<SpriteRenderer>();
        if (laserTowerSprite != null)
        {
            spriteRenderer.sprite = laserTowerSprite;
        }
        else
        {
            // Create simple square as default sprite
            spriteRenderer.color = Color.red;
        }
        
        // Add tower script
        LaserTower tower = towerObj.AddComponent<LaserTower>();
        
        // Add collider for selection
        CircleCollider2D collider = towerObj.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        
        // Add audio source
        AudioSource audioSource = towerObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        // Add line renderer for laser
        LineRenderer lineRenderer = towerObj.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.yellow;
        tower.laserBeam = lineRenderer;
        lineRenderer.enabled = false;
        
        // Create fire point
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(towerObj.transform);
        firePoint.transform.localPosition = new Vector3(0, 0.5f, 0);
        tower.firePoint = firePoint.transform;
        
        // Set base properties
        tower.towerName = "Laser Tower";
        tower.damage = 20f;
        tower.fireRate = 0f; // Continuous damage
        tower.range = 4f;
        tower.cost = 175;
        tower.upgradePrice = 125;
        
        // Save prefab to Resources folder
        if (saveToResources)
        {
            string path = resourcePath + "/LaserTower";
            GameObject prefabInstance = SaveTowerPrefab(path, towerObj);
            towerObj = prefabInstance; // Use saved instance
        }
        
        // Deactivate by default
        towerObj.SetActive(false);
        
        return towerObj;
    }
    
    // Create projectile prefabs
    private void CreateProjectiles()
    {
        // Create cannonball prefab
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
        
        // Save prefab
        if (saveToResources)
        {
            string path = resourcePath + "/Cannonball";
            SaveTowerPrefab(path, cannonball);
        }
        
        // Create arrow prefab
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
        arrowScript.speed = 20f;
        arrowScript.explosionRadius = 0f; // Arrows don't explode
        
        BoxCollider2D arrowCollider = arrow.AddComponent<BoxCollider2D>();
        arrowCollider.size = new Vector2(0.5f, 0.1f);
        arrowCollider.isTrigger = true;
        
        // Save prefab
        if (saveToResources)
        {
            string path = resourcePath + "/Arrow";
            SaveTowerPrefab(path, arrow);
        }
    }
    
    // Save prefab to Resources folder
    private GameObject SaveTowerPrefab(string path, GameObject obj)
    {
        // Use resourcePath property to build full path
        string fullPath = path;
        
        // If path doesn't start with resourcePath, add resourcePath prefix
        if (!string.IsNullOrEmpty(resourcePath) && !path.StartsWith(resourcePath))
        {
            // Remove any existing "tower/" prefix from path
            string cleanPath = path;
            if (path.StartsWith("tower/"))
            {
                cleanPath = path.Substring(6); // Remove "tower/"
            }
            
            // Build new path
            fullPath = resourcePath + "/" + cleanPath;
        }
        
        // At runtime, we can't actually save prefabs, but can store objects in global manager
        // This just simulates the prefab saving process
        
        Debug.Log($"Simulating prefab save: {fullPath}");
        
        // In actual project, could use Resources.Load to load prefabs
        // Or use a prefab pool manager to handle these objects
        
        return obj;
    }
}