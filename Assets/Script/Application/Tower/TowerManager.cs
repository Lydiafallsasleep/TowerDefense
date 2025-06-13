using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public enum TowerType
{
    Cannon,
    Arrow,
    Laser
}

public class TowerManager : Singleton<TowerManager>
{
    [Header("Tower Prefabs")]
    public GameObject cannonTowerPrefab;
    public GameObject arrowTowerPrefab;
    public GameObject laserTowerPrefab;
    
    [Header("Placement Settings")]
    public Tilemap placementTilemap; // Used to determine placeable areas
    public Color validPlacementColor = Color.green;
    public Color invalidPlacementColor = Color.red;
    public Vector3 towerPositionOffset = new Vector3(0, 0.5f, 0); // Offset for tower placement position
    
    [Header("Resources")]
    public int currentGold = 200; // Starting gold
    
    [Header("UI References")]
    public Text goldText;
    public GameObject buildPanel;
    public GameObject upgradePanel;
    public Text towerInfoText;
    public Button upgradeButton;
    public Button sellButton;
    public Text notificationText; // Notification text
    private float notificationDuration = 3f; // Notification display duration

    [Header("Range Visualization")]
    public bool showRangeOnSelect = true;
    public GameObject rangeIndicatorPrefab;
    
    // Record of built towers
    private Dictionary<Vector3Int, BaseTower> builtTowers = new Dictionary<Vector3Int, BaseTower>();
    
    // Currently selected tower type
    private TowerType selectedTowerType;
    
    // Currently selected tower (for upgrading or selling)
    private BaseTower selectedTower;
    
    // Whether to use preset placement points
    public bool usePresetPlacementPoints = true;
    
    // Currently selected placement point
    private TowerPlacementPoint selectedPlacementPoint;
    
    // Tower preview
    private GameObject towerPreview;
    private SpriteRenderer previewRenderer;
    
    // Range indicator
    private GameObject currentRangeIndicator;
    
    // In the TowerManager class field area
    public TowerOperationPanel towerOperationPanel;
    
    void Start()
    {
        // Force use of preset placement points
        usePresetPlacementPoints = true;
        
        // Initialize tower prefabs
        InitializeTowerPrefabs();
        
        // Initialize preview
        CreateTowerPreview();
        
        // Initialize UI
        UpdateGoldDisplay();
        ShowBuildPanel();
        
        // If using preset placement points, highlight available points
        if (usePresetPlacementPoints)
        {
            TowerPlacementManager placementManager = TowerPlacementManager.Instance;
            if (placementManager != null)
            {
                placementManager.HighlightAvailablePoints(true);
            }
        }
    }
    
    void Update()
    {
        // Update preview position
        UpdateTowerPreview();
        
        // Detect mouse click
        CheckMouseClick();
        
        // Cancel selection
        if (Input.GetMouseButtonDown(1) && (selectedTower != null || selectedPlacementPoint != null))
        {
            DeselectTower();
            DeselectPlacementPoint();
        }
    }
    
    // Initialize tower prefabs, ensure they exist
    void InitializeTowerPrefabs()
    {
        Debug.Log("TowerManager: Initializing tower prefabs");
        
        // Check Resources directory structure
        Debug.Log("Checking Resources directory structure...");
        Object[] allResources = Resources.LoadAll("");
        foreach (Object res in allResources)
        {
            Debug.Log($"Resource: {res.name} (Type: {res.GetType().Name})");
        }
        
        // Try to load prefabs from Resources - note that path doesn't need "Resources/" prefix or extension
        if (cannonTowerPrefab == null)
        {
            // Try multiple possible paths
            cannonTowerPrefab = Resources.Load<GameObject>("tower/CannonTower");
            if (cannonTowerPrefab == null) cannonTowerPrefab = Resources.Load<GameObject>("Tower/CannonTower");
            if (cannonTowerPrefab == null) cannonTowerPrefab = Resources.Load<GameObject>("Towers/CannonTower");
            if (cannonTowerPrefab == null) cannonTowerPrefab = Resources.Load<GameObject>("Prefabs/tower/CannonTower");
            if (cannonTowerPrefab == null) cannonTowerPrefab = Resources.Load<GameObject>("Prefabs/Tower/CannonTower");
            if (cannonTowerPrefab == null) cannonTowerPrefab = Resources.Load<GameObject>("Prefabs/Towers/CannonTower");
            if (cannonTowerPrefab == null) cannonTowerPrefab = Resources.Load<GameObject>("CannonTower");
        }
            
        if (arrowTowerPrefab == null)
        {
            arrowTowerPrefab = Resources.Load<GameObject>("tower/ArrowTower");
            if (arrowTowerPrefab == null) arrowTowerPrefab = Resources.Load<GameObject>("Tower/ArrowTower");
            if (arrowTowerPrefab == null) arrowTowerPrefab = Resources.Load<GameObject>("Towers/ArrowTower");
            if (arrowTowerPrefab == null) arrowTowerPrefab = Resources.Load<GameObject>("Prefabs/tower/ArrowTower");
            if (arrowTowerPrefab == null) arrowTowerPrefab = Resources.Load<GameObject>("Prefabs/Tower/ArrowTower");
            if (arrowTowerPrefab == null) arrowTowerPrefab = Resources.Load<GameObject>("Prefabs/Towers/ArrowTower");
            if (arrowTowerPrefab == null) arrowTowerPrefab = Resources.Load<GameObject>("ArrowTower");
        }
            
        if (laserTowerPrefab == null)
        {
            laserTowerPrefab = Resources.Load<GameObject>("tower/LaserTower");
            if (laserTowerPrefab == null) laserTowerPrefab = Resources.Load<GameObject>("Tower/LaserTower");
            if (laserTowerPrefab == null) laserTowerPrefab = Resources.Load<GameObject>("Towers/LaserTower");
            if (laserTowerPrefab == null) laserTowerPrefab = Resources.Load<GameObject>("Prefabs/tower/LaserTower");
            if (laserTowerPrefab == null) laserTowerPrefab = Resources.Load<GameObject>("Prefabs/Tower/LaserTower");
            if (laserTowerPrefab == null) laserTowerPrefab = Resources.Load<GameObject>("Prefabs/Towers/LaserTower");
            if (laserTowerPrefab == null) laserTowerPrefab = Resources.Load<GameObject>("LaserTower");
        }
            
        // Output prefab loading results
        Debug.Log($"CannonTower prefab loading status: {(cannonTowerPrefab != null ? "Success" : "Failed")}");
        Debug.Log($"ArrowTower prefab loading status: {(arrowTowerPrefab != null ? "Success" : "Failed")}");
        Debug.Log($"LaserTower prefab loading status: {(laserTowerPrefab != null ? "Success" : "Failed")}");
        
        // Check if successfully loaded
        bool allPrefabsLoaded = cannonTowerPrefab != null && arrowTowerPrefab != null && laserTowerPrefab != null;
        
        if (!allPrefabsLoaded)
        {
            Debug.LogWarning("Prefab loading failed, will attempt to create Resources/tower directory and generate prefabs");
            
            // Check if there's a SimpleTowerBuilder in the scene
            SimpleTowerBuilder builder = FindObjectOfType<SimpleTowerBuilder>();
            if (builder == null)
            {
                Debug.Log("Creating SimpleTowerBuilder");
                GameObject builderObj = new GameObject("SimpleTowerBuilder");
                builder = builderObj.AddComponent<SimpleTowerBuilder>();
                builder.saveToResources = true;
            }
            
            // Ensure SimpleTowerBuilder knows the correct save path
            if (builder != null)
            {
                builder.resourcePath = "tower";
                Debug.Log("Set SimpleTowerBuilder's resourcePath to 'tower'");
            }
            
            // Wait a frame to let SimpleTowerBuilder initialize
            Invoke("LoadBuiltPrefabs", 0.2f);
        }
        
        // Force use of preset placement points
        usePresetPlacementPoints = true;
    }
    
    // Load from prefabs created by Builder
    void LoadBuiltPrefabs()
    {
        Debug.Log("Attempting to load tower prefabs from Resources/tower folder...");
        
        // Try to find Resources directory structure again
        Debug.Log("Checking Resources directory structure again...");
        Object[] allResources = Resources.LoadAll("");
        foreach (Object res in allResources)
        {
            Debug.Log($"Resource: {res.name} (Type: {res.GetType().Name})");
        }
        
        // Check if tower directory exists
        Object[] towerResources = Resources.LoadAll("tower");
        Debug.Log($"Number of resources in tower directory: {towerResources.Length}");
        foreach (Object res in towerResources)
        {
            Debug.Log($"Tower Resource: {res.name} (Type: {res.GetType().Name})");
        }
        
        // Try to load from different paths
        if (cannonTowerPrefab == null)
        {
            cannonTowerPrefab = Resources.Load<GameObject>("tower/CannonTower");
            if (cannonTowerPrefab == null) cannonTowerPrefab = Resources.Load<GameObject>("Tower/CannonTower");
            if (cannonTowerPrefab == null) cannonTowerPrefab = Resources.Load<GameObject>("Towers/CannonTower");
            if (cannonTowerPrefab == null) cannonTowerPrefab = Resources.Load<GameObject>("CannonTower");
        }
        
        if (arrowTowerPrefab == null)
        {
            arrowTowerPrefab = Resources.Load<GameObject>("tower/ArrowTower");
            if (arrowTowerPrefab == null) arrowTowerPrefab = Resources.Load<GameObject>("Tower/ArrowTower");
            if (arrowTowerPrefab == null) arrowTowerPrefab = Resources.Load<GameObject>("Towers/ArrowTower");
            if (arrowTowerPrefab == null) arrowTowerPrefab = Resources.Load<GameObject>("ArrowTower");
        }
        
        if (laserTowerPrefab == null)
        {
            laserTowerPrefab = Resources.Load<GameObject>("tower/LaserTower");
            if (laserTowerPrefab == null) laserTowerPrefab = Resources.Load<GameObject>("Tower/LaserTower");
            if (laserTowerPrefab == null) laserTowerPrefab = Resources.Load<GameObject>("Towers/LaserTower");
            if (laserTowerPrefab == null) laserTowerPrefab = Resources.Load<GameObject>("LaserTower");
        }
        
        // Output loading status of each prefab
        Debug.Log($"Retry loading - CannonTower prefab: {(cannonTowerPrefab != null ? "Success" : "Failed")}");
        Debug.Log($"Retry loading - ArrowTower prefab: {(arrowTowerPrefab != null ? "Success" : "Failed")}");
        Debug.Log($"Retry loading - LaserTower prefab: {(laserTowerPrefab != null ? "Success" : "Failed")}");
        
        if (cannonTowerPrefab == null || arrowTowerPrefab == null || laserTowerPrefab == null)
        {
            Debug.LogError("Unable to load tower prefabs, will attempt to create temporary prefabs");
            
            // Create temporary prefabs
            CreateTemporaryTowerPrefabs();
        }
        else
        {
            Debug.Log("Tower prefabs loaded successfully!");
        }
    }
    
    // Create temporary prefabs, for emergency situations
    void CreateTemporaryTowerPrefabs()
    {
        Debug.Log("Creating temporary tower prefabs...");
        
        // If prefabs are not loaded, create temporary objects
        if (cannonTowerPrefab == null)
        {
            GameObject tempCannonTower = new GameObject("CannonTower");
            tempCannonTower.AddComponent<CannonTower>();
            tempCannonTower.AddComponent<SpriteRenderer>();
            cannonTowerPrefab = tempCannonTower;
            
            // Set not to destroy
            DontDestroyOnLoad(tempCannonTower);
            tempCannonTower.SetActive(false);
        }
        
        if (arrowTowerPrefab == null)
        {
            GameObject tempArrowTower = new GameObject("ArrowTower");
            tempArrowTower.AddComponent<ArrowTower>();
            tempArrowTower.AddComponent<SpriteRenderer>();
            arrowTowerPrefab = tempArrowTower;
            
            // Set not to destroy
            DontDestroyOnLoad(tempArrowTower);
            tempArrowTower.SetActive(false);
        }
        
        if (laserTowerPrefab == null)
        {
            GameObject tempLaserTower = new GameObject("LaserTower");
            tempLaserTower.AddComponent<LaserTower>();
            tempLaserTower.AddComponent<SpriteRenderer>();
            laserTowerPrefab = tempLaserTower;
            
            // Set not to destroy
            DontDestroyOnLoad(tempLaserTower);
            tempLaserTower.SetActive(false);
        }
        
        Debug.Log("Temporary tower prefabs created");
    }
    
    void CreateTowerPreview()
    {
        towerPreview = new GameObject("TowerPreview");
        previewRenderer = towerPreview.AddComponent<SpriteRenderer>();
        
        // Set preview sprite based on currently selected tower type
        UpdatePreviewSprite();
        
        // Set semi-transparent
        Color color = previewRenderer.color;
        color.a = 0.5f;
        previewRenderer.color = color;
    }
    
    void UpdatePreviewSprite()
    {
        if (previewRenderer != null)
        {
            GameObject prefab = GetTowerPrefab(selectedTowerType);
            if (prefab != null)
            {
                SpriteRenderer towerRenderer = prefab.GetComponent<SpriteRenderer>();
                if (towerRenderer != null)
                {
                    previewRenderer.sprite = towerRenderer.sprite;
                }
            }
        }
    }
    
    void UpdateTowerPreview()
    {
        // If there's a selected placement point, preview fixed at that point
        if (usePresetPlacementPoints && selectedPlacementPoint != null)
        {
            Debug.Log("UpdateTowerPreview: Selected placement point" + selectedPlacementPoint.transform.position);
            if (towerPreview != null)
            {
                towerPreview.transform.position = selectedPlacementPoint.transform.position;
                
                // Check if tower can be placed at point
                bool canPlace = CanPlaceTowerAtPoint(selectedPlacementPoint);
                Debug.Log("UpdateTowerPreview: Whether tower can be placed" + canPlace);
                Color color = canPlace ? validPlacementColor : invalidPlacementColor;
                color.a = 0.5f; // Semi-transparent
                if (previewRenderer != null)
                {
                    previewRenderer.color = color;
                }
            }
            return;
        }

        // Get mouse position
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        Vector3Int cellPos = Vector3Int.zero;
        Vector3 cellWorldPos = Vector3.zero;
        
        // Use preset placement point mode
        if (usePresetPlacementPoints)
        {
            // Use plane for ray detection, get world coordinates
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            if (plane.Raycast(ray, out float distance))
            {
                mouseWorldPos = ray.GetPoint(distance);
            }
            
            // Try to get nearest placement point from TowerPlacementManager
            TowerPlacementManager placementManager = TowerPlacementManager.Instance;
            if (placementManager != null)
            {
                TowerPlacementPoint nearestPoint = placementManager.GetNearestAvailablePoint(mouseWorldPos);
                if (nearestPoint != null)
                {
                    cellPos = nearestPoint.gridPosition;
                    cellWorldPos = nearestPoint.transform.position;
                    
                    // Update preview position
                    if (towerPreview != null)
                    {
                        towerPreview.transform.position = cellWorldPos;
                        
                        // Check if tower can be placed
                        bool canPlace = CanPlaceTower(cellPos);
                        
                        // Update preview color
                        Color previewColor = canPlace ? validPlacementColor : invalidPlacementColor;
                        previewColor.a = 0.5f; // Semi-transparent
                        if (previewRenderer != null)
                        {
                            previewRenderer.color = previewColor;
                        }
                    }
                    return;
                }
                else
                {
                    // If no usable placement point found, hide preview
                    if (towerPreview != null)
                    {
                        towerPreview.transform.position = new Vector3(-1000, -1000, -1000); // Move out of sight
                    }
                    return;
                }
            }
            else
            {
                Debug.LogWarning("TowerPlacementManager instance not found");
                return;
            }
        }
        // Use Tilemap mode - keep but don't use
        else if (placementTilemap != null)
        {
            // Convert world coordinates to cell coordinates
            cellPos = placementTilemap.WorldToCell(mouseWorldPos);
        
            // Convert cell coordinates back to world coordinates (centered)
            cellWorldPos = placementTilemap.GetCellCenterWorld(cellPos);
        }
        else
        {
            // If no Tilemap, use integer part of mouse position as grid position
            cellPos = new Vector3Int(
                Mathf.RoundToInt(mouseWorldPos.x),
                Mathf.RoundToInt(mouseWorldPos.y),
                Mathf.RoundToInt(mouseWorldPos.z)
            );
            cellWorldPos = new Vector3(cellPos.x, cellPos.y, cellPos.z);
        }
        
        // Update preview position
        towerPreview.transform.position = cellWorldPos;
        
        // Check if tower can be placed
        bool canBuildTower = CanPlaceTower(cellPos);
        
        // Update preview color
        Color towerPreviewColor = canBuildTower ? validPlacementColor : invalidPlacementColor;
        towerPreviewColor.a = 0.5f; // Semi-transparent
        previewRenderer.color = towerPreviewColor;
    }
    
    private void CheckMouseClick()
    {
        // Click on UI is ignored
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Original mouse left click detection logic
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }
    
    void HandleMouseClick()
    {
        // If click occurs on UI, it's directly ignored
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
        
        // Get mouse position
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        Vector3Int cellPos;
        
        // Use preset placement point mode
        if (usePresetPlacementPoints)
        {
            // Use plane for ray detection, get world coordinates
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            if (plane.Raycast(ray, out float distance))
            {
                mouseWorldPos = ray.GetPoint(distance);
            }
            
            // Calculate grid position - to match keys in builtTowers dictionary
            if (placementTilemap != null)
            {
                cellPos = placementTilemap.WorldToCell(mouseWorldPos);
            }
            else
            {
                // Try to get nearest placement point from TowerPlacementManager
                TowerPlacementManager placementManager = TowerPlacementManager.Instance;
                if (placementManager != null)
                {
                    TowerPlacementPoint nearestPoint = placementManager.GetNearestAvailablePoint(mouseWorldPos);
                    if (nearestPoint != null)
                    {
                        cellPos = nearestPoint.gridPosition;
                    }
                    else if (placementManager.placementPoints.Count > 0)
                    {
                        // Use grid position of first placement point as reference
                        cellPos = placementManager.placementPoints[0].gridPosition;
                    }
                    else
                    {
                        // If no placement points, use integer part of mouse position as grid position
                        cellPos = new Vector3Int(
                            Mathf.RoundToInt(mouseWorldPos.x),
                            Mathf.RoundToInt(mouseWorldPos.y),
                            Mathf.RoundToInt(mouseWorldPos.z)
                        );
                    }
                }
                else
                {
                    // If no TowerPlacementManager, use integer part of mouse position
                    cellPos = new Vector3Int(
                        Mathf.RoundToInt(mouseWorldPos.x),
                        Mathf.RoundToInt(mouseWorldPos.y),
                        Mathf.RoundToInt(mouseWorldPos.z)
                    );
                }
            }
            
            HandlePresetPlacementClick(mouseWorldPos, cellPos);
            return;
        }
        // Always use preset placement point mode, no longer use Tilemap mode
        else
        {
            // Force use of preset placement points
            usePresetPlacementPoints = true;
            
            // Use plane for ray detection, get world coordinates
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            if (plane.Raycast(ray, out float distance))
            {
                mouseWorldPos = ray.GetPoint(distance);
            }
            
            // Try to get nearest placement point from TowerPlacementManager
            TowerPlacementManager placementManager = TowerPlacementManager.Instance;
            if (placementManager != null)
            {
                TowerPlacementPoint nearestPoint = placementManager.GetNearestAvailablePoint(mouseWorldPos);
                if (nearestPoint != null)
                {
                    cellPos = nearestPoint.gridPosition;
                    HandlePresetPlacementClick(mouseWorldPos, cellPos);
                    return;
                }
                else
                {
                    Debug.LogWarning("No usable placement point found");
                }
            }
            else
            {
                Debug.LogWarning("TowerPlacementManager instance not found");
            }
            
            // If placement point cannot be found, return directly
            return;
        }
    }
    
    // Handle click on preset placement point
    private void HandlePresetPlacementClick(Vector3 mouseWorldPos, Vector3Int cellPos)
    {
        TowerPlacementManager placementManager = TowerPlacementManager.Instance;
        if (placementManager == null) return;
        
        // Find nearest placement point
        TowerPlacementPoint nearestPoint = placementManager.GetNearestAvailablePoint(mouseWorldPos);
        
        // Add empty check, if no usable placement point found
        if (nearestPoint == null)
        {
            Debug.LogWarning("No usable placement point found");
            // First check if clicked on existing tower
            if (builtTowers.ContainsKey(cellPos))
            {
                // Existing tower, select it
                SelectTower(builtTowers[cellPos]);
            }
            return;
        }
        
        float clickDistance = Vector3.Distance(mouseWorldPos, nearestPoint.transform.position);
        
        // First check if clicked on existing tower
        if (builtTowers.ContainsKey(cellPos))
        {
            // Existing tower, select it
            SelectTower(builtTowers[cellPos]);
            return;
        }
        
        // If clicked on sufficiently close usable placement point
        if (clickDistance < 1.0f)
        {
            // Select this placement point
            SelectPlacementPoint(nearestPoint);
            return;
        }
        else if (selectedTower != null)
        {
            // If already selected a tower and clicked on empty land, cancel selection
            DeselectTower();
            return;
        }
        else if (selectedPlacementPoint != null)
        {
            // If already selected a placement point and clicked on other place, cancel selection
            DeselectPlacementPoint();
            return;
        }
    }
    
    // Select placement point
    public void SelectPlacementPoint(TowerPlacementPoint point)
    {
        Debug.Log($"[TowerManager] SelectPlacementPoint -> {point.pointID} at {point.transform.position}");
        if (selectedPlacementPoint != point)
        {
            // Cancel previous selection
            DeselectPlacementPoint();
            
            selectedPlacementPoint = point;
            
            // Highlight this placement point
            // TODO: Implement highlight effect
            
            // Show build panel
            ShowBuildPanel();
            
            // Update preview position to placement point
            if (towerPreview != null)
            {
                towerPreview.transform.position = point.transform.position;
                
                // Check if tower can be placed at point
                bool canPlace = CanPlaceTowerAtPoint(point);
                Color color = canPlace ? validPlacementColor : invalidPlacementColor;
                color.a = 0.5f; // Semi-transparent
                previewRenderer.color = color;
            }
        }
    }
    
    // Cancel selection of placement point
    public void DeselectPlacementPoint()
    {
        Debug.Log("[TowerManager] DeselectPlacementPoint");
        if (selectedPlacementPoint != null)
        {
            // Cancel highlight effect
            // TODO: Remove highlight effect
            
            selectedPlacementPoint = null;
        }
    }
    
    // Check if tower can be placed at placement point
    public bool CanPlaceTowerAtPoint(TowerPlacementPoint point)
    {
        if (point == null || point.isOccupied || !point.isEnabled)
        {
            return false;
        }
        
        // Special handling for ObstaclePlacementPoint
        if (point is ObstaclePlacementPoint)
        {
            ObstaclePlacementPoint obstaclePoint = (ObstaclePlacementPoint)point;
            Vector3Int obstaclePos = obstaclePoint.obstaclePosition;
            
            // Check enhanced obstacle system
            EnhancedObstacleManager enhancedObstacleManager = FindObjectOfType<EnhancedObstacleManager>();
            if (enhancedObstacleManager != null)
            {
                if (enhancedObstacleManager.IsObstacle(obstaclePos) && !enhancedObstacleManager.IsClearedObstacle(obstaclePos))
                {
                    Debug.Log($"[TowerManager] Obstacle at position {obstaclePos} not cleared, cannot build tower");
                    return false;
                }
            }
            
            // Check old obstacle system
            ObstacleManager obstacleManager = ObstacleManager.Instance;
            if (obstacleManager != null && !obstacleManager.IsClearedObstacle(obstaclePoint.obstaclePosition))
            {
                Debug.Log($"[TowerManager] Obstacle at position {obstaclePos} not cleared, cannot build tower");
                return false;
            }
        }
        
        // Check if there's any un-cleared obstacle at the placement point's location
        Vector3Int gridPos = point.gridPosition;
        
        // Check enhanced obstacle manager
        EnhancedObstacleManager enhancedObstacleMgr = FindObjectOfType<EnhancedObstacleManager>();
        if (enhancedObstacleMgr != null && !enhancedObstacleMgr.CanPlaceAtPosition(gridPos))
        {
            Debug.Log($"[TowerManager] Position {gridPos} has un-cleared obstacle, cannot place tower");
            return false;
        }
        
        // Check old obstacle manager
        ObstacleManager obstacleMgr = ObstacleManager.Instance;
        if (obstacleMgr != null && !obstacleMgr.CanPlaceAtPosition(gridPos))
        {
            Debug.Log($"[TowerManager] Position {gridPos} has un-cleared obstacle, cannot place tower");
            return false;
        }
        
        // Check if there's enough gold
        GameObject prefab = GetTowerPrefab(selectedTowerType);
        if (prefab != null)
        {
            BaseTower towerScript = prefab.GetComponent<BaseTower>();
            if (towerScript != null && currentGold < towerScript.cost)
            {
                return false; // Not enough gold
            }
        }
        
        return true;
    }
    
    // Build tower at selected placement point
    public void BuildTowerOnSelectedPoint()
    {
        Debug.Log("BuildTowerOnSelectedPoint called");
        
        if (selectedPlacementPoint == null)
        {
            Debug.LogWarning("No selected placement point, cannot build tower!");
            return;
        }
        
        Debug.Log("BuildTowerOnSelectedPoint: Selected placement point" + selectedPlacementPoint.pointID + " at " + selectedPlacementPoint.transform.position);
        Debug.Log("BuildTowerOnSelectedPoint: Starting to build tower");
        
        if (!CanPlaceTowerAtPoint(selectedPlacementPoint))
        {
            Debug.LogWarning("Cannot build tower at current selected placement point!");
            return;
        }
        
        // Get current selected tower prefab
        GameObject prefab = GetTowerPrefab(selectedTowerType);
        if (prefab == null)
        {
            Debug.LogError($"Tower prefab is null: {selectedTowerType}");
            return;
        }
        
        // Check if there's enough gold
        BaseTower towerScript = prefab.GetComponent<BaseTower>();
        if (towerScript == null)
        {
            Debug.LogError("Tower prefab missing BaseTower component!");
            return;
        }
        
        // Check gold
        bool hasEnoughGold = false;
        int cost = towerScript.cost;
        
        // Use CoinManager to deduct gold first
        if (CoinManager.Instance != null)
        {
            hasEnoughGold = CoinManager.Instance.TrySpendCoins(cost);
        }
        else
        {
            hasEnoughGold = currentGold >= cost;
        }
        
        if (!hasEnoughGold)
        {
            ShowNotification("Not enough gold! Need " + cost + " gold");
            Debug.LogWarning("Not enough gold, cannot build tower!");
            return;
        }
        
        // Get tower's offset (global offset + tower-specific offset)
        Vector3 towerOffset = towerPositionOffset;
        if (towerScript != null) {
            towerOffset += towerScript.positionOffset;
        }
        Vector3 towerPosition = selectedPlacementPoint.transform.position + towerOffset;
        GameObject tower = Instantiate(prefab, towerPosition, Quaternion.identity);
        tower.SetActive(true); // Ensure tower is active
        
        // Get tower script
        BaseTower towerComponent = tower.GetComponent<BaseTower>();
        
        if (towerComponent != null)
        {
            // Deduct gold
            if (CoinManager.Instance != null)
            {
                // CoinManager has already deducted gold in TrySpendCoins
            }
            else
            {
                currentGold -= cost;
                UpdateGoldDisplay();
            }
            
            // Record built tower (using grid coordinates)
            Vector3Int gridPosition = selectedPlacementPoint.gridPosition;
            builtTowers[gridPosition] = towerComponent;
            
            // Set placement point as occupied
            selectedPlacementPoint.OccupyPoint(towerComponent);
            
            Debug.Log($"Built {towerComponent.towerName}, cost {cost} gold");
            
            // Select newly built tower
            SelectTower(towerComponent);
            DeselectPlacementPoint();
        }
    }
    
    void DeselectTower()
    {
        // Hide range indicator
        if (currentRangeIndicator != null)
        {
            Destroy(currentRangeIndicator);
            currentRangeIndicator = null;
        }
        
        // Clear selected state
        selectedTower = null;
        
        // Show build panel
        ShowBuildPanel();
    }
    
    public bool CanPlaceTower(Vector3Int cellPos)
    {
        // Check if there's already a tower at this position
        if (builtTowers.ContainsKey(cellPos))
        {
            return false;
        }
        
        // If using preset placement points, check through TowerPlacementManager
        if (usePresetPlacementPoints)
        {
            TowerPlacementManager placementManager = TowerPlacementManager.Instance;
            if (placementManager != null)
            {
                return placementManager.CanPlaceTowerAt(cellPos);
            }
            return false;
        }
        // If not using preset placement points, check through Tilemap
        else if (placementTilemap != null)
        {
            // Check if it's a valid placement position (has corresponding tile)
            if (!placementTilemap.HasTile(cellPos))
            {
                return false;
            }
        }
        else
        {
            // Force use of preset placement point mode
            usePresetPlacementPoints = true;
            
            // Retry using preset placement points
            TowerPlacementManager placementManager = TowerPlacementManager.Instance;
            if (placementManager != null)
            {
                return placementManager.CanPlaceTowerAt(cellPos);
            }
            return false;
        }
        
        // Check if there's any obstacle at the position and not cleared
        // First check enhanced obstacle manager
        EnhancedObstacleManager enhancedObstacleMgr = FindObjectOfType<EnhancedObstacleManager>();
        if (enhancedObstacleMgr != null && !enhancedObstacleMgr.CanPlaceAtPosition(cellPos))
        {
            return false;  // There's obstacle, cannot place
        }
        
        // Then check old obstacle manager
        ObstacleManager obstacleMgr = ObstacleManager.Instance;
        if (obstacleMgr != null && !obstacleMgr.CanPlaceAtPosition(cellPos))
        {
            return false;  // There's obstacle, cannot place
        }
        
        // Check if there's enough gold
        GameObject prefab = GetTowerPrefab(selectedTowerType);
        if (prefab != null)
        {
            BaseTower towerComponent = prefab.GetComponent<BaseTower>();
            if (towerComponent != null && towerComponent.cost > currentGold)
            {
                return false;  // Not enough gold
            }
        }
        
        return true;  // All checks passed
    }
    
    void BuildTower(Vector3Int cellPos)
    {
        GameObject prefab = GetTowerPrefab(selectedTowerType);
        if (prefab == null)
        {
            Debug.LogError("Tower prefab not found!");
            return;
        }
        
        // Get tower's basic components, check if there's enough gold
        BaseTower towerPrefabComponent = prefab.GetComponent<BaseTower>();
        if (towerPrefabComponent == null)
        {
            Debug.LogError("Tower prefab missing BaseTower component!");
            return;
        }
        
        // First check if there's enough gold
        bool hasEnoughGold = false;
        int cost = towerPrefabComponent.cost;
        
        // Use CoinManager to deduct gold first
        if (CoinManager.Instance != null)
        {
            hasEnoughGold = CoinManager.Instance.TrySpendCoins(cost);
        }
        else
        {
            hasEnoughGold = currentGold >= cost;
        }
        
        if (!hasEnoughGold)
        {
            ShowNotification("Not enough gold! Need " + cost + " gold");
            Debug.LogWarning("Not enough gold, cannot build tower!");
            return;
        }
        
        // Get placement point
        TowerPlacementManager placementManager = TowerPlacementManager.Instance;
        if (placementManager == null)
        {
            Debug.LogError("TowerPlacementManager not found, cannot build tower!");
            return;
        }
        
        TowerPlacementPoint point = placementManager.GetPlacementPoint(cellPos);
        if (point == null)
        {
            Debug.LogWarning($"Placement point not found, cannot build tower! Position: {cellPos}");
            return;
        }
        
        // Check if placement point is already occupied
        if (point.isOccupied)
        {
            ShowNotification("This position is occupied!");
            return;
        }
        
        // Get tower's offset (global offset + tower-specific offset)
        Vector3 towerOffset = towerPositionOffset;
        if (towerPrefabComponent != null) {
            towerOffset += towerPrefabComponent.positionOffset;
        }
        Vector3 towerPosition = point.transform.position + towerOffset;
        GameObject tower = Instantiate(prefab, towerPosition, Quaternion.identity);
        tower.SetActive(true); // Ensure tower is active
        
        // Get tower script
        BaseTower towerComponent = tower.GetComponent<BaseTower>();
        
        if (towerComponent != null)
        {
            // Deduct gold
            if (CoinManager.Instance != null)
            {
                // CoinManager has already deducted gold in TrySpendCoins
            }
            else
            {
                currentGold -= cost;
                UpdateGoldDisplay();
            }
            
            // Record built tower
            builtTowers[cellPos] = towerComponent;
            
            // Set placement point as occupied
            point.OccupyPoint(towerComponent);
            
            Debug.Log($"Built {towerComponent.towerName}, cost {cost} gold");
            
            // Select newly built tower
            SelectTower(towerComponent);
        }
    }
    
    void SelectTower(BaseTower tower)
    {
        // Cancel previous selection
        if (selectedTower != null && selectedTower != tower)
        {
            DeselectTower();
        }
        
        selectedTower = tower;
        Debug.Log($"Selected {tower.towerName}, level: {tower.level}/{tower.maxLevel}");
        
        // Show tower range
        if (showRangeOnSelect)
        {
            ShowTowerRange(tower);
        }
        
        // Show upgrade panel
        ShowUpgradePanel();
        
        // Update tower info display
        UpdateTowerInfo();
    }
    
    // Show tower attack range
    void ShowTowerRange(BaseTower tower)
    {
        // Clear existing range indicator
        if (currentRangeIndicator != null)
        {
            Destroy(currentRangeIndicator);
        }
        
        // Create range indicator
        if (rangeIndicatorPrefab != null)
        {
            currentRangeIndicator = Instantiate(rangeIndicatorPrefab, tower.transform.position, Quaternion.identity);
        }
        else
        {
            currentRangeIndicator = new GameObject("RangeIndicator");
            currentRangeIndicator.transform.position = tower.transform.position;
            
            SpriteRenderer rangeRenderer = currentRangeIndicator.AddComponent<SpriteRenderer>();
            rangeRenderer.sprite = Resources.Load<Sprite>("UI/Circle");
            if (rangeRenderer.sprite == null) // If cannot load sprite, create a simple circle
            {
                rangeRenderer.color = new Color(1f, 1f, 0f, 0.2f); // Semi-transparent yellow
            }
            
            rangeRenderer.sortingOrder = -1; // Ensure range is displayed below tower
        }
        
        // Set range size
        currentRangeIndicator.transform.localScale = new Vector3(tower.range * 2, tower.range * 2, 1);
    }
    
    // Upgrade selected tower
    public void UpgradeSelectedTower()
    {
        if (selectedTower == null)
            return;
        
        int upgradeCost = selectedTower.GetUpgradeCost();
        
        bool hasEnoughGold = false;
            
        // Use CoinManager to deduct gold first
        if (CoinManager.Instance != null)
        {
            hasEnoughGold = CoinManager.Instance.TrySpendCoins(upgradeCost);
        }
        else
        {
            hasEnoughGold = currentGold >= upgradeCost;
            if (hasEnoughGold)
        {
            currentGold -= upgradeCost;
                UpdateGoldDisplay();
            }
        }
            
        if (hasEnoughGold && selectedTower.level < selectedTower.maxLevel)
        {
            
            selectedTower.Upgrade();
            
            // Update range indicator size
            if (currentRangeIndicator != null)
            {
                currentRangeIndicator.transform.localScale = new Vector3(selectedTower.range * 2, selectedTower.range * 2, 1);
            }
            
            // Update UI information
            UpdateTowerInfo();
            
            Debug.Log($"Upgraded {selectedTower.towerName} to {selectedTower.level} level, cost {upgradeCost} gold, remaining {currentGold} gold");
        }
        else if (selectedTower.level >= selectedTower.maxLevel)
        {
            Debug.LogWarning($"{selectedTower.towerName} is already at max level!");
        }
        else
        {
            Debug.LogWarning($"Not enough gold, need {upgradeCost} gold!");
        }
    }
    
    
    public void SelectTowerType(TowerType type)
    {
        selectedTowerType = type;
        Debug.Log($"Selected {type} tower");
        
        // Update preview sprite
        UpdatePreviewSprite();
    }
    
    // Get tower prefab
    public GameObject GetTowerPrefab(TowerType type)
    {
        GameObject prefab = null;
        string typeName = "";
        
        switch (type)
        {
            case TowerType.Cannon:
                prefab = cannonTowerPrefab;
                typeName = "CannonTower";
                break;
            case TowerType.Arrow:
                prefab = arrowTowerPrefab;
                typeName = "ArrowTower";
                break;
            case TowerType.Laser:
                prefab = laserTowerPrefab;
                typeName = "LaserTower";
                break;
            default:
                Debug.LogError($"Unknown tower type: {type}");
                return null;
        }
        
        if (prefab == null)
        {
            Debug.LogWarning($"Failed to get {typeName} prefab, trying to reload...");
            prefab = Resources.Load<GameObject>($"tower/{typeName}");
            
            if (prefab == null)
            {
                Debug.LogError($"Cannot load {typeName} prefab, please ensure prefab is located in Resources/tower/{typeName}");
            }
            else
            {
                Debug.Log($"Successfully reloaded {typeName} prefab");
                // Update corresponding prefab reference
                switch (type)
                {
                    case TowerType.Cannon:
                        cannonTowerPrefab = prefab;
                        break;
                    case TowerType.Arrow:
                        arrowTowerPrefab = prefab;
                        break;
                    case TowerType.Laser:
                        laserTowerPrefab = prefab;
                        break;
                }
            }
        }
        
        return prefab;
    }
    
    // Add gold (called by GameManager)
    public void AddGold(int amount)
    {
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(amount);
        }
    }
    
    // UI related methods
    public void UpdateGoldDisplay()
    {
        // Use CoinManager, UI automatically updates, no need to manually refresh
        return;
    }
    
    private void ShowBuildPanel()
    {
        if (buildPanel != null)
            buildPanel.SetActive(true);
            
        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }
    
    private void ShowUpgradePanel()
    {
        if (buildPanel != null)
            buildPanel.SetActive(false);
            
        if (upgradePanel != null)
            upgradePanel.SetActive(true);
            
        // Update upgrade button status
        if (upgradeButton != null && selectedTower != null)
        {
            int upgradeCost = selectedTower.GetUpgradeCost();
            bool hasEnoughGold = false;
            
            // Check if there's enough gold
            if (CoinManager.Instance != null)
            {
                hasEnoughGold = CoinManager.Instance.HasEnoughCoins(upgradeCost);
            }
            else
            {
                hasEnoughGold = currentGold >= upgradeCost;
            }
            
            upgradeButton.interactable = selectedTower.level < selectedTower.maxLevel && hasEnoughGold;
        }
    }
    
    private void UpdateTowerInfo()
    {
        if (towerInfoText != null && selectedTower != null)
        {
            towerInfoText.text = $"{selectedTower.towerName} (level {selectedTower.level}/{selectedTower.maxLevel})\n" +
                                 $"Damage: {selectedTower.damage}\n" +
                                 $"Fire rate: {selectedTower.fireRate}/second\n" +
                                 $"Range: {selectedTower.range}";
                                 
            if (selectedTower.level < selectedTower.maxLevel)
            {
                towerInfoText.text += $"\nUpgrade cost: {selectedTower.GetUpgradeCost()}";
            }
            else
            {
                towerInfoText.text += "\nAlready at max level";
            }
            
            towerInfoText.text += $"\nSell value: {selectedTower.GetSellValue()}";
        }
    }
    
    // UI button callbacks
    public void OnCannonTowerButton()
    {
        SelectTowerType(TowerType.Cannon);
    }
    
    public void OnArrowTowerButton()
    {
        SelectTowerType(TowerType.Arrow);
    }
    
    public void OnLaserTowerButton()
    {
        SelectTowerType(TowerType.Laser);
    }
    
    public void OnUpgradeButtonClick()
    {
        UpgradeSelectedTower();
    }
    
    
    // Handle placement point selected/deselected events
    public void OnPlacementPointSelected(TowerPlacementPoint point)
    {
        // Display tower information that can be built at this point in the UI
        // Special logic can be added based on game design
    }

    public void OnPlacementPointDeselected()
    {
        // Hide build information in the UI
    }

    // Add method for building tower from UI
    public void BuildTowerButtonClicked()
    {
        // If using preset placement points and there's a selected point
        if (usePresetPlacementPoints && selectedPlacementPoint != null)
        {
            Debug.Log("BuildTowerButtonClicked: Building tower" + selectedPlacementPoint.transform.position);
            
            BuildTowerOnSelectedPoint();
        }
        else
        {
            // No additional operation needed in normal placement mode
            // Because it will be handled by HandleMouseClick in clicking map
        }
    }

    // Show notification
    public void ShowNotification(string message)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.gameObject.SetActive(true);
            
            // Hide notification after a few seconds
            CancelInvoke("HideNotification");
            Invoke("HideNotification", notificationDuration);
        }
    }
    
    // Hide notification
    private void HideNotification()
    {
        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }
    }
    
    // Reset TowerManager state
    public void ResetState()
    {
        Debug.Log("[TowerManager] Reset TowerManager state");
        
        // Reset gold
        currentGold = 200; // Reset to initial gold
        UpdateGoldDisplay();
        
        // Cancel current selected tower
        DeselectTower();
        DeselectPlacementPoint();
        
        // Hide upgrade panel, show build panel
        ShowBuildPanel();
        
        // Hide notification
        HideNotification();
        
        // Reset tower preview
        if (towerPreview != null)
        {
            towerPreview.SetActive(false);
        }
        
        // Reset range indicator
        if (currentRangeIndicator != null)
        {
            Destroy(currentRangeIndicator);
            currentRangeIndicator = null;
        }
        
        Debug.Log("[TowerManager] TowerManager state reset");
    }

    public void OnTowerSelected(BaseTower tower)
    {
        if (tower != null)
        {
            SelectTower(tower);
        }
    }

    public void SellSelectedTower(BaseTower selectedTower)
    {
        if (selectedTower == null)
        {
            Debug.LogWarning("No selected tower to sell");
            return;
        }

        // Get tower's sell value
        int sellValue = selectedTower.GetSellValue();

        // Use CoinManager to add gold (if exists)
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(sellValue);
        }
        else
        {
            currentGold += sellValue;
            UpdateGoldDisplay();
        }

        // 1. Remove tower from built towers list
        Vector3Int towerKey = Vector3Int.zero;
        bool found = false;

        // Iterate through dictionary to find this tower's position
        foreach (var pair in builtTowers)
        {
            if (pair.Value == selectedTower)
            {
                towerKey = pair.Key;
                found = true;
                break;
            }
        }

        if (found)
        {
            builtTowers.Remove(towerKey);
            Debug.Log($"Removed tower from built list: {selectedTower.towerName}");
        }
        else
        {
            Debug.LogWarning($"Tower not found in built list: {selectedTower.towerName}");
        }

        // 2. Get and reset associated placement point
        TowerPlacementPoint placementPoint = null;

        if (usePresetPlacementPoints)
        {
            TowerPlacementManager placementManager = TowerPlacementManager.Instance;
            if (placementManager != null)
            {
                // Get associated placement point
                placementPoint = placementManager.GetPlacementPoint(towerKey);
                Debug.Log("Get associated point" + placementPoint);
                if (placementPoint != null)
                {
                    // Reset placement point state
                    placementPoint.ReleasePoint();  // This will set isEnabled = true
                    Debug.Log($"Reset placement point: {placementPoint.pointID}, {placementPoint.isEnabled}");

                    // If needed, can directly set:
                    // placementPoint.isEnabled = true;
                    // placementPoint.isOccupied = false;
                }
                else
                {
                    Debug.LogWarning($"Tower associated placement point not found: {towerKey}");
                }
            }
        }

        // 3. Destroy tower object
        Debug.Log($"Sold tower: {selectedTower.towerName}, gained {sellValue} gold");
        Destroy(selectedTower.gameObject);

        // 4. Clear selected state and range indicator
        if (currentRangeIndicator != null)
        {
            Destroy(currentRangeIndicator);
            currentRangeIndicator = null;
        }

        selectedTower = null;

        // 5. Show build panel
        ShowBuildPanel();

        // 6. Show notification
        ShowNotification($"Sold tower, gained {sellValue} gold");

        // 7. If there's operation panel, hide it
        if (towerOperationPanel != null)
        {
            towerOperationPanel.HidePanel();
        }
    }
    public void OnTowerSold(BaseTower selectedTower)
    {
        SellSelectedTower(selectedTower);
    }
    public void OnTowerUpgraded(BaseTower upgradedTower)
    {
        if (upgradedTower == null)
        {
            Debug.LogWarning("Received empty tower upgrade notification");
            return;
        }

        Debug.Log($"{upgradedTower.towerName} upgraded to {upgradedTower.level} level");
    }

}