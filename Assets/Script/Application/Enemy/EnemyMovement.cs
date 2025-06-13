using UnityEngine;
using System.Collections;

public class EnemyMovement : MonoBehaviour
{
    public enum MonsterType { Slime, Fish }

    [Header("Settings")]
    public MonsterType monsterType;
    public float moveSpeed = 2f;
    public float originalMoveSpeed = 2f; // Original movement speed, used for recovering from slow effects
    public float waypointThreshold = 0.1f;
    [Tooltip("Number of path initialization retries")]
    public int maxInitRetries = 3;
    [Tooltip("Retry interval time (seconds)")]
    public float retryInterval = 0.5f;
    [Tooltip("Enable smooth movement")]
    public bool useSmoothMovement = true;
    [Tooltip("Rotation speed")]
    public float rotationSpeed = 5f;
    [Tooltip("Damage to player when reaching the end")]
    public int damageToPlayer = 1;

    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool pathInitialized = false;
    private Vector2 currentDirection;
    private Vector2 targetDirection;
    private PlayerHealth playerHealth;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentDirection = Vector2.right; // Initial direction
        
        // Save original speed for recovering from slow effects
        originalMoveSpeed = moveSpeed;
        
        // Find PlayerHealth component
        playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogWarning("[EnemyMovement] PlayerHealth component not found, will use GameManager for damage handling");
        }
    }

    void OnEnable()
    {
        // Reset key states
        pathInitialized = false;
        currentWaypointIndex = 0;
        
        // Start coroutine to try initializing path over multiple frames
        StartCoroutine(InitializePathWithRetry());
    }

    IEnumerator InitializePathWithRetry()
    {
        int retryCount = 0;
        
        // Wait a short time first to ensure the path has a chance to initialize
        yield return new WaitForSeconds(0.2f);
        
        // Try to initialize the path, retry up to maxInitRetries times
        while (!pathInitialized && retryCount < maxInitRetries)
        {
            if (InitializePath())
            {
                pathInitialized = true;
                yield break; // Successfully initialized, exit coroutine
            }
            
            retryCount++;
            Debug.Log($"Path initialization failed, trying again in {retryInterval} seconds (attempt {retryCount})...");
            yield return new WaitForSeconds(retryInterval); // Wait before trying again
        }
        
        if (!pathInitialized)
        {
            Debug.LogError($"Path initialization failed after {retryCount} retries. Creating temporary path.");
            // Create temporary path and try to use it
            string parentName = monsterType == MonsterType.Slime ? "LandPathParent" : "WaterPathParent";
            CreateAndUseTemporaryPath(parentName);
        }
    }

    bool InitializePath()
    {
        string parentName = monsterType == MonsterType.Slime ? "LandPathParent" : "WaterPathParent";
        Debug.Log($"[{gameObject.name}] Trying to find path parent: {parentName}");
        
        // Direct lookup
        GameObject pathParentObj = GameObject.Find(parentName);
        Transform pathParent = pathParentObj?.transform;
        
        // If path parent object not found, create temporary path
        if (pathParent == null)
        {
            Debug.LogError($"[{gameObject.name}] Path parent not found: {parentName}, creating temporary path");
                return CreateAndUseTemporaryPath(parentName);
        }
        
        // Path parent exists, check child objects
        if (pathParent.childCount == 0)
        {
            // Has parent but no children, directly create temporary path
            Debug.LogError($"[{gameObject.name}] Path object {parentName} exists but has no children! Creating temporary path");
                return CreateAndUseTemporaryPath(parentName);
        }
        
        Debug.Log($"Successfully found path parent: {parentName}, child count: {pathParent.childCount}");
        
        // Create waypoint array and fill immediately
        try
        {
            int childCount = pathParent.childCount;
            waypoints = new Transform[childCount];
            for (int i = 0; i < childCount; i++)
            {
                waypoints[i] = pathParent.GetChild(i);
                if (waypoints[i] == null)
                {
                    Debug.LogError($"Waypoint {i} is null! Path initialization failed");
                    return false;
                }
            }
            
            currentWaypointIndex = 0;
            Debug.Log($"Initialized {waypoints.Length} waypoints for {monsterType}");
            
            // Additional validation: ensure first waypoint is available
            if (waypoints.Length > 0 && waypoints[0] != null)
            {
                Debug.Log($"First waypoint position: {waypoints[0].position}");
                // Set initial position to first waypoint
                transform.position = waypoints[0].position;
                
                // If there's a second waypoint, calculate initial direction
                if (waypoints.Length > 1)
                {
                    Vector2 direction = (waypoints[1].position - waypoints[0].position).normalized;
                    currentDirection = direction;
                    targetDirection = direction;
                }
                
                return true; // Initialization successful
            }
            else
            {
                Debug.LogError("Waypoint array is empty or first waypoint is null!");
                return false; // Initialization failed
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception during waypoint initialization: {e.Message}");
            return false;
        }
    }

    // Create and use temporary path, returns whether successful
    bool CreateAndUseTemporaryPath(string parentName)
    {
        Debug.Log($"Creating and using temporary path: {parentName}");
        Transform pathParent = CreateTemporaryPath(parentName);
        if (pathParent == null || pathParent.childCount == 0)
        {
            Debug.LogError("Failed to create temporary path");
            return false;
        }

        int childCount = pathParent.childCount;
        waypoints = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
        {
            waypoints[i] = pathParent.GetChild(i);
        }
        
        currentWaypointIndex = 0;
        Debug.Log($"Using temporary path, waypoint count: {waypoints.Length}");
        
        // Set initial position to first waypoint
        if (waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
            
            // If there's a second waypoint, calculate initial direction
            if (waypoints.Length > 1)
            {
                Vector2 direction = (waypoints[1].position - waypoints[0].position).normalized;
                currentDirection = direction;
                targetDirection = direction;
            }
            
            return true;
        }
        
        return false;
    }

    // Create a temporary path for testing
    private Transform CreateTemporaryPath(string pathName)
    {
        Debug.Log($"Creating temporary path: {pathName}");
        
        // Clean up existing path parent object with the same name
        GameObject existingPath = GameObject.Find(pathName);
        if (existingPath != null)
        {
            Debug.Log($"Found existing path object: {pathName}, will clear its children");
            foreach (Transform child in existingPath.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Create new waypoints
            Vector3[] points = GetDefaultPathPoints(monsterType);
            for (int i = 0; i < points.Length; i++)
            {
                GameObject waypoint = new GameObject($"Waypoint_{i}");
                waypoint.transform.position = points[i];
                waypoint.transform.SetParent(existingPath.transform);
        }
        
            return existingPath.transform;
        }
        else
        {
        // Create new path parent object
        GameObject pathParent = new GameObject(pathName);
        
            // Create waypoints
        Vector3[] points = GetDefaultPathPoints(monsterType);
        for (int i = 0; i < points.Length; i++)
        {
            GameObject waypoint = new GameObject($"Waypoint_{i}");
            waypoint.transform.position = points[i];
            waypoint.transform.SetParent(pathParent.transform);
        }
        
        return pathParent.transform;
        }
    }

    // Get default waypoints
    private Vector3[] GetDefaultPathPoints(MonsterType type)
    {
        // Default path points based on monster type
        Vector3[] points;
        
        if (type == MonsterType.Slime)
        {
            // Land path (horizontal line from left to right)
            points = new Vector3[]
            {
                new Vector3(-8f, 0f, 0f),
                new Vector3(-4f, 0f, 0f),
                new Vector3(0f, 0f, 0f),
                new Vector3(4f, 0f, 0f),
                new Vector3(8f, 0f, 0f)
            };
        }
        else // Fish
        {
            // Water path (curved path)
            points = new Vector3[]
            {
                new Vector3(-8f, -2f, 0f),
                new Vector3(-4f, -1f, 0f),
                new Vector3(0f, -2f, 0f),
                new Vector3(4f, -1f, 0f),
                new Vector3(8f, -2f, 0f)
            };
        }
        
        return points;
    }

    void FixedUpdate()
    {
        if (!pathInitialized || waypoints == null || waypoints.Length == 0)
        {
            return; // Don't move if path is not ready
        }
        
        if (currentWaypointIndex >= waypoints.Length)
        {
            // Reached the end of the path
            ReachedEnd();
            return;
        }
        
        // Make sure the current waypoint is valid
        if (waypoints[currentWaypointIndex] == null)
        {
            Debug.LogError($"Waypoint at index {currentWaypointIndex} is null!");
            return;
        }
        
        // Get current waypoint position
        Vector2 targetPosition = waypoints[currentWaypointIndex].position;

        // Calculate distance to waypoint
        float distanceToWaypoint = Vector2.Distance(rb.position, targetPosition);
        
        // Check if we reached the waypoint
        if (distanceToWaypoint < waypointThreshold)
        {
            // Move to next waypoint
            currentWaypointIndex++;
            
            // Check if we've reached the end
            if (currentWaypointIndex >= waypoints.Length)
            {
                ReachedEnd();
                return;
            }
            
            // Update target direction to next waypoint
            if (waypoints[currentWaypointIndex] != null)
            {
                targetDirection = ((Vector2)waypoints[currentWaypointIndex].position - rb.position).normalized;
            }
        }
        else
        {
            // Update target direction to current waypoint
            targetDirection = (targetPosition - rb.position).normalized;
        }
        
        // Smooth rotation of direction vector
        if (useSmoothMovement)
        {
            currentDirection = Vector2.Lerp(currentDirection, targetDirection, Time.deltaTime * rotationSpeed);
        }
        else
        {
            currentDirection = targetDirection;
        }
        
        // Move towards the current waypoint
        Vector2 movement = currentDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
        
        // Update sprite flip based on movement direction
        if (currentDirection.x < 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (currentDirection.x > 0)
        {
            spriteRenderer.flipX = false;
        }
    }

    void ReachedEnd()
    {
        // Deal damage to player when enemy reaches the end
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageToPlayer);
            Debug.Log($"Enemy reached end, dealing {damageToPlayer} damage to player");
        }
        else
        {
            // Try to find GameManager if PlayerHealth wasn't found
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.PlayerTakeDamage(damageToPlayer);
                Debug.Log($"Enemy reached end, using GameManager to deal {damageToPlayer} damage");
            }
            else
            {
                Debug.LogError("Cannot deal damage to player: Neither PlayerHealth nor GameManager found");
            }
        }
        
        // Despawn this enemy
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.OnDespawn(gameObject);
            Debug.Log("Enemy despawned through ObjectPool");
        }
        else
        {
            Debug.LogWarning("ObjectPool.Instance not found, destroying enemy directly");
            Destroy(gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize waypoints and path in editor
        if (waypoints != null && waypoints.Length > 0)
        {
            // Draw waypoints
            Gizmos.color = Color.red;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawSphere(waypoints[i].position, 0.2f);
                    
                    // Draw lines between waypoints
                    if (i > 0 && waypoints[i-1] != null)
                    {
                        Gizmos.DrawLine(waypoints[i-1].position, waypoints[i].position);
                    }
                }
            }
        }
    }

    public void ResetState()
    {
        // Reset movement variables
        moveSpeed = originalMoveSpeed;
        
        // Reset waypoint index
        currentWaypointIndex = 0;
        
        // Reset path initialization
        pathInitialized = false;
        
        // Return to first waypoint if possible
        if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
        {
            transform.position = waypoints[0].position;
        }
        
        // Re-initialize path
        StartCoroutine(InitializePathWithRetry());
    }
    
    public void ResetPath()
    {
        // Reset path-related variables
        pathInitialized = false;
        currentWaypointIndex = 0;
        
        // Start initialization coroutine
        StartCoroutine(InitializePathWithRetry());
    }
    
    public float GetPathProgress()
    {
        if (waypoints == null || waypoints.Length <= 1)
            return 0f;
            
        // Return progress as a percentage
        return (float)currentWaypointIndex / (waypoints.Length - 1);
    }
    
    public void ApplySlow(float slowFactor, float duration)
    {
        // Apply slow effect
        moveSpeed = originalMoveSpeed * (1f - slowFactor);
        
        // Cancel any existing slow coroutines
        CancelInvoke("ResetMoveSpeed");
        
        // Schedule reset after duration
        Invoke("ResetMoveSpeed", duration);
        
        Debug.Log($"Applied slow effect: {slowFactor*100}% for {duration} seconds. Speed reduced from {originalMoveSpeed} to {moveSpeed}");
    }
    
    private void ResetMoveSpeed()
    {
        // Reset to original move speed
        moveSpeed = originalMoveSpeed;
        Debug.Log($"Slow effect ended. Speed reset to {moveSpeed}");
    }
}