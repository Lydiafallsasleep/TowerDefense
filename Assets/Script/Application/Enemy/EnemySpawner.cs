using UnityEngine;
using System.Collections;

public class EnemySpawner : Singleton<EnemySpawner>
{
    [Header("Spawn Settings")]
    public float spawnInterval = 2f;
    public Transform spawnPoint;
    public bool autoSpawn = true;
    [Tooltip("Initial delay to ensure paths are generated first")]
    public float initialDelay = 1f;
    [Tooltip("Maximum number of enemies allowed on screen")]
    public int maxEnemiesOnScreen = 10;

    // Simplified path settings
    [Header("Path Settings")]
    [Tooltip("Default land path name")]
    public string landPathName = "LandPathParent";
    [Tooltip("Default water path name")]
    public string waterPathName = "WaterPathParent";

    private float timer = 0f;
    private bool initialized = false;
    private bool pathsAvailable = false;
    private bool isGameOver = false;
    private PlayerHealth playerHealth;
    private GameManager gameManager;

    void Start()
    {
        // Find PlayerHealth component
        playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            // Subscribe to game over event
            playerHealth.OnGameOver += OnGameOver;
            Debug.Log("[EnemySpawner] Subscribed to PlayerHealth's OnGameOver event");
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] PlayerHealth component not found");
        }
        
        // Find GameManager component
        gameManager = GameManager.Instance;
        
        // Delay spawning enemies for a short time
        StartCoroutine(DelayedStart());
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (playerHealth != null)
        {
            playerHealth.OnGameOver -= OnGameOver;
        }
    }
    
    // Game over callback
    private void OnGameOver()
    {
        Debug.Log("[EnemySpawner] Received game over event, stopping enemy spawning");
        isGameOver = true;
        autoSpawn = false;
        StopAllCoroutines();
    }

    IEnumerator DelayedStart()
    {
        Debug.Log("EnemySpawner waiting for initialization...");
        yield return new WaitForSeconds(initialDelay);
        
        // Check if paths exist
        pathsAvailable = CheckPaths();
        initialized = true;
        
        if (pathsAvailable)
        {
            Debug.Log("EnemySpawner initialization complete, paths available, starting to spawn enemies");
        }
        else
        {
            Debug.LogError("EnemySpawner initialization complete, but paths unavailable, enemies may not move correctly!");
            // Simplified handling, force allow spawning
            pathsAvailable = true;
            CreateDefaultPaths();
        }
    }

    // Simplified path checking method
    bool CheckPaths()
    {
        bool landPathOk = false;
        bool waterPathOk = false;
        
        // Check land path
        GameObject landPathObj = GameObject.Find(landPathName);
        Transform landPath = landPathObj?.transform;
        if (landPath != null && landPath.childCount > 0)
        {
            Debug.Log($"Found land path, waypoint count: {landPath.childCount}");
            landPathOk = true;
        }
        else
        {
            Debug.LogError($"Cannot find valid land path ({landPathName}) or path is empty");
            CreateDefaultPath(landPathName, true);
            landPathOk = true;
        }

        // Check water path
        GameObject waterPathObj = GameObject.Find(waterPathName);
        Transform waterPath = waterPathObj?.transform;
        if (waterPath != null && waterPath.childCount > 0)
        {
            Debug.Log($"Found water path, waypoint count: {waterPath.childCount}");
            waterPathOk = true;
        }
        else
        {
            Debug.LogError($"Cannot find valid water path ({waterPathName}) or path is empty");
            CreateDefaultPath(waterPathName, false);
            waterPathOk = true;
        }
        
        return landPathOk && waterPathOk;
    }

    // Create default paths
    void CreateDefaultPaths()
    {
        CreateDefaultPath(landPathName, true);
        CreateDefaultPath(waterPathName, false);
    }
    
    // Create a default path
    void CreateDefaultPath(string pathName, bool isLandPath)
    {
        GameObject pathParent = GameObject.Find(pathName);
        if (pathParent == null)
        {
            pathParent = new GameObject(pathName);
        }
        
        // Clear existing children
        foreach (Transform child in pathParent.transform)
        {
            Destroy(child.gameObject);
        }
        
        // Create waypoints
        Vector3[] points;
        float centerX = 0f;
        float centerY = 0f;
        
        if (isLandPath)
        {
            // Create Z-shaped land path
            points = new Vector3[] {
                new Vector3(centerX - 5f, centerY - 5f, 0),
                new Vector3(centerX + 5f, centerY - 5f, 0),
                new Vector3(centerX + 5f, centerY, 0),
                new Vector3(centerX - 5f, centerY, 0),
                new Vector3(centerX - 5f, centerY + 5f, 0),
                new Vector3(centerX + 5f, centerY + 5f, 0)
            };
        }
        else
        {
            // Create circular water path
            points = new Vector3[] {
                new Vector3(centerX - 5f, centerY - 5f, 0),
                new Vector3(centerX + 5f, centerY - 5f, 0),
                new Vector3(centerX + 5f, centerY + 5f, 0),
                new Vector3(centerX - 5f, centerY + 5f, 0),
                new Vector3(centerX - 5f, centerY - 5f, 0)
            };
        }
        
        // Create waypoints
        for (int i = 0; i < points.Length; i++)
        {
            GameObject waypoint = new GameObject($"Waypoint_{i}");
            waypoint.transform.position = points[i];
            waypoint.transform.SetParent(pathParent.transform);
            Debug.Log($"Created default waypoint {i} at position: {points[i]}");
        }
        
        Debug.Log($"Default {(isLandPath ? "land" : "water")} path created, waypoint count: {pathParent.transform.childCount}");
    }

    void Update()
    {
        // If game is over, stop spawning enemies
        if (isGameOver)
        {
            return;
        }
        
        // Check if GameManager has marked game as over
        if (gameManager != null && gameManager.isGameOver)
        {
            Debug.Log("[EnemySpawner] Detected GameManager.isGameOver is true, stopping enemy spawning");
            isGameOver = true;
            autoSpawn = false;
            return;
        }
        
        if (!autoSpawn || !initialized || !pathsAvailable)
        {
            return;
        }

        // Check current number of enemies in the scene
        int currentEnemyCount = GameObject.FindObjectsOfType<EnemyMovement>().Length;
        
        // If maximum enemy count reached, don't spawn more
        if (currentEnemyCount >= maxEnemiesOnScreen)
        {
            timer = 0f; // Reset timer
            return;
        }

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnEnemy();
            Debug.Log($"Automatically spawned enemy, current enemy count: {currentEnemyCount + 1}");
        }
    }

    public void SpawnEnemy(EnemyMovement.MonsterType? type = null)
    {
        // If game is over, don't spawn more enemies
        if (isGameOver)
        {
            Debug.Log("[EnemySpawner] Game is over, not spawning more enemies");
            return;
        }
        
        // Ensure paths are available
        if (!pathsAvailable)
        {
            pathsAvailable = CheckPaths();
            if (!pathsAvailable)
            {
                Debug.LogError("Cannot spawn enemy due to unavailable paths, but will not prevent next attempt");
                pathsAvailable = true; // Force reset to true to ensure next attempt still happens
                return;
            }
        }
        
        // If no specific type provided, randomly choose one
        EnemyMovement.MonsterType enemyType = type ?? 
            (Random.value > 0.5f ? EnemyMovement.MonsterType.Slime : EnemyMovement.MonsterType.Fish);
        
        // Determine spawn position
        Vector3 position = spawnPoint != null ? 
            spawnPoint.position : new Vector3(-8f, Random.Range(-2f, 2f), 0f);
        
        // Spawn the enemy
        GameObject enemy = SpawnEnemyAtPosition(enemyType, position);
        
        if (enemy != null)
        {
            Debug.Log($"Spawned {enemyType} enemy at {position}");
        }
        else
        {
            Debug.LogError($"Failed to spawn {enemyType} enemy");
        }
    }

    public void SpawnMultipleEnemies(int count)
    {
        if (isGameOver)
        {
            Debug.Log("[EnemySpawner] Game is over, not spawning multiple enemies");
            return;
        }
        
        // Use coroutine to spread out spawns
        StartCoroutine(SpawnMultipleEnemiesCoroutine(count));
    }
    
    private IEnumerator SpawnMultipleEnemiesCoroutine(int count)
    {
        Debug.Log($"Starting to spawn {count} enemies");
        
        for (int i = 0; i < count; i++)
        {
            if (isGameOver)
                break;
                
            SpawnEnemy();
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.Log($"Finished spawning multiple enemies");
    }
    
    public void SetGameOver(bool gameOver)
    {
        // Set game over status
        isGameOver = gameOver;
        
        if (gameOver)
        {
            // Stop auto-spawning when game over
            autoSpawn = false;
            
            // Stop all running coroutines
            StopAllCoroutines();
            
            // Log status
            Debug.Log("[EnemySpawner] Game over status set, enemy spawning stopped");
            
            // Optionally handle existing enemies
            if (gameOver)
            {
                // Could add code here to clear all enemies if needed
            }
        }
    }
    
    public GameObject SpawnEnemyWithType(EnemyMovement.MonsterType type)
    {
        // If game is over, don't spawn
        if (isGameOver)
        {
            Debug.Log("[EnemySpawner] Game is over, not spawning requested enemy");
            return null;
        }
        
        // Ensure paths are available
        if (!pathsAvailable)
        {
            pathsAvailable = CheckPaths();
            if (!pathsAvailable)
            {
                Debug.LogError("Cannot spawn enemy due to unavailable paths");
                return null;
            }
        }
        
        // Determine spawn position
        Vector3 position;
        if (spawnPoint != null)
        {
            position = spawnPoint.position;
        }
        else
        {
            // Default spawn position based on enemy type
            if (type == EnemyMovement.MonsterType.Slime)
            {
                // Land enemy starts on the left side
                position = new Vector3(-8f, 0f, 0f);
            }
            else // Fish
            {
                // Water enemy starts on the left side slightly lower
                position = new Vector3(-8f, -2f, 0f);
            }
        }
        
        // Try using the object pool first
        GameObject enemy = null;
        if (ObjectPool.Instance != null)
        {
            string prefabName = type == EnemyMovement.MonsterType.Slime ? "Slime" : "Fish";
            enemy = ObjectPool.Instance.OnSpawn(prefabName);
            
            if (enemy != null)
            {
                // Set the position
                enemy.transform.position = position;
                
                // Ensure enemy movement component exists and is set up
                EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
                if (movement != null)
                {
                    movement.monsterType = type;
                    // The OnEnable method will handle path initialization
                }
                else
                {
                    Debug.LogError($"Spawned enemy {prefabName} lacks EnemyMovement component");
                }
                
                Debug.Log($"Spawned {prefabName} from object pool at {position}");
            }
            else
            {
                Debug.LogError($"Failed to spawn {prefabName} from object pool");
            }
        }
        else
        {
            Debug.LogError("ObjectPool.Instance not found, cannot spawn enemy");
        }
        
        return enemy;
    }
    
    public void ResetState()
    {
        // Reset spawner state
        timer = 0f;
        isGameOver = false;
        
        // Clear existing enemies if needed
        EnemyMovement[] existingEnemies = FindObjectsOfType<EnemyMovement>();
        foreach (EnemyMovement enemy in existingEnemies)
        {
            if (ObjectPool.Instance != null)
            {
                ObjectPool.Instance.OnDespawn(enemy.gameObject);
            }
            else
            {
                Destroy(enemy.gameObject);
            }
        }
        
        Debug.Log("[EnemySpawner] State reset, existing enemies cleared");
        
        // Re-check paths
        pathsAvailable = CheckPaths();
        
        // Restart auto-spawning if enabled
        if (autoSpawn)
        {
            Debug.Log("[EnemySpawner] Auto-spawning restarted");
        }
    }
    
    private GameObject SpawnEnemyAtPosition(EnemyMovement.MonsterType enemyType, Vector3 position)
    {
        // Try using object pool first
        GameObject enemy = null;
        
        string prefabName = enemyType == EnemyMovement.MonsterType.Slime ? "Slime" : "Fish";
        
        if (ObjectPool.Instance != null)
        {
            // Get from object pool
            enemy = ObjectPool.Instance.OnSpawn(prefabName);
            
            if (enemy == null)
            {
                Debug.LogError($"Failed to get {prefabName} from object pool");
                return null;
            }
            
            // Set position
            enemy.transform.position = position;
            
            // Initialize enemy if needed
            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.monsterType = enemyType;
                // The OnEnable method will handle path initialization
            }
            else
            {
                Debug.LogError($"Spawned enemy lacks EnemyMovement component");
            }
        }
        else
        {
            Debug.LogError("ObjectPool.Instance not found, cannot spawn enemy");
        }
        
        return enemy;
    }
} 