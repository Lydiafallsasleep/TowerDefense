using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Tower placement point that interacts with obstacles, only available after the obstacle is cleared
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ObstaclePlacementPoint : TowerPlacementPoint
{
    [Header("Obstacle Settings")]
    public Vector3Int obstaclePosition; // Related obstacle position
    public bool autoDetectPosition = true; // Whether to automatically detect position
    
    private ObstacleManager obstacleManager;
    private SpriteRenderer spriteRenderer;
    
    private void Awake()
    {
        // Get component references
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // If no placement indicator is specified, use this SpriteRenderer
        if (placementIndicator == null)
            placementIndicator = spriteRenderer;
            
        // Default state is unavailable until the obstacle is cleared
        isEnabled = false;
    }
    
    private void Start()
    {
        // Get manager instance
        obstacleManager = ObstacleManager.Instance;
        
        // If auto-detect position is enabled and obstacle manager exists
        if (autoDetectPosition && obstacleManager != null)
        {
            // Try to get position from any obstacle layer
            if (obstacleManager.obstacleTilemaps != null && obstacleManager.obstacleTilemaps.Length > 0)
            {
                foreach (Tilemap tilemap in obstacleManager.obstacleTilemaps)
                {
                    if (tilemap != null)
                    {
                        obstaclePosition = tilemap.WorldToCell(transform.position);
                        gridPosition = obstaclePosition; // Sync to base class grid position
                        break;
                    }
                }
            }
            // Backward compatibility - use main obstacle layer
            else if (obstacleManager.obstacleTilemap != null)
            {
                obstaclePosition = obstacleManager.obstacleTilemap.WorldToCell(transform.position);
                gridPosition = obstaclePosition; // Sync to base class grid position
            }
        }
        
        // Initial state check - check if obstacle is already cleared
        UpdateInitialState();
    }
    
    // Initial state check
    private void UpdateInitialState()
    {
        if (obstacleManager != null && obstacleManager.IsClearedObstacle(obstaclePosition))
        {
            EnablePoint();
        }
        else
        {
            DisablePoint();
        }
    }
    
    private void OnEnable()
    {
        // Register event listener (ensure called after Start)
        if (obstacleManager == null)
            obstacleManager = ObstacleManager.Instance;
            
        if (obstacleManager != null)
        {
            obstacleManager.OnObstacleCleared += OnObstacleCleared;
        }
    }
    
    private void OnDisable()
    {
        // Unregister event
        if (obstacleManager != null)
        {
            obstacleManager.OnObstacleCleared -= OnObstacleCleared;
        }
    }
    
    // Obstacle cleared event handler
    private void OnObstacleCleared(Vector3Int position)
    {
        if (position == obstaclePosition)
        {
            EnablePoint();
        }
    }
    
    /// <summary>
    /// Update placement point availability based on obstacle state
    /// </summary>
    public void UpdateAvailability()
    {
        if (obstacleManager == null) return;
        
        // Check if obstacle is cleared
        bool obstacleCleared = obstacleManager.IsClearedObstacle(obstaclePosition);
        
        // Update availability state
        if (obstacleCleared && !isEnabled)
        {
            EnablePoint(); // Obstacle cleared, enable placement point
        }
        else if (!obstacleCleared && isEnabled)
        {
            DisablePoint(); // Obstacle not cleared, disable placement point
        }
    }
    
    // Draw gizmos in scene view to show obstacle association
    private void OnDrawGizmos()
    {
        // Draw basic info (copy base class logic rather than calling it)
        Gizmos.color = isOccupied ? Color.red : (isEnabled ? Color.green : Color.gray);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // Draw connection line to obstacle
        if (obstacleManager == null) return;
        
        // Try to find any obstacle layer containing this position
        Vector3 obstacleWorldPos = Vector3.zero;
        bool found = false;
        
        if (obstacleManager.obstacleTilemaps != null && obstacleManager.obstacleTilemaps.Length > 0)
        {
            foreach (Tilemap tilemap in obstacleManager.obstacleTilemaps)
            {
                if (tilemap != null && tilemap.GetTile(obstaclePosition) != null)
                {
                    obstacleWorldPos = tilemap.GetCellCenterWorld(obstaclePosition);
                    found = true;
                    break;
                }
            }
            
            // If no layer with Tile is found, use position from first layer
            if (!found && obstacleManager.obstacleTilemaps[0] != null)
            {
                obstacleWorldPos = obstacleManager.obstacleTilemaps[0].GetCellCenterWorld(obstaclePosition);
                found = true;
            }
        }
        else if (obstacleManager.obstacleTilemap != null) // Backward compatibility
        {
            obstacleWorldPos = obstacleManager.obstacleTilemap.GetCellCenterWorld(obstaclePosition);
            found = true;
        }
        
        // If valid position found, draw connection line
        if (found)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, obstacleWorldPos);
        }
    }
}
